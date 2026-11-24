using EOP.Gestion.WebApp.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EOP.Gestion.Tests.Integracion;

// S1-T30 — La matriz de RF-73, ejecutable, para TODAS las acciones que publica la app. Se lee de los endpoints
// reales y no de una lista de atributos: así cuenta lo que ASP.NET Core aplica de verdad (Controller + acción).
//
// Si agregás una acción, esta prueba falla hasta que la declares acá con sus roles según RF-73. Es a propósito:
// obliga a decidir el permiso de cada operación nueva y hace visible cualquier pantalla pública o
// cualquier ruta para administrar roles o permisos individuales, que US-044 prohíbe.
public class MatrizDeAutorizacionTests(SesionIntegracionTests.FabricaDePrueba fabrica)
    : IClassFixture<SesionIntegracionTests.FabricaDePrueba>
{
    private const string Publica = "pública";
    private const string CualquierRolConSesion = "cualquier rol con sesión";
    private const string SoloAdministrador = "Administrador";
    private const string AdministradorOSupervisor = "Administrador,Supervisor";

    private static readonly Dictionary<string, string> MatrizEsperada = new()
    {
        // Sesión (US-042). Sin [HttpGet]/[HttpPost], Home responde a cualquier verbo.
        ["GET Cuenta/IniciarSesion"] = Publica,
        ["GET Cuenta/SesionCerrada"] = Publica,
        ["GET Cuenta/ErrorInicioSesion"] = Publica,
        ["* Home/Error"] = Publica,
        ["POST Cuenta/CerrarSesion"] = CualquierRolConSesion,
        ["GET Cuenta/AccesoDenegado"] = CualquierRolConSesion,
        ["* Home/Index"] = CualquierRolConSesion,
        ["* Home/Privacy"] = CualquierRolConSesion,

        // RF-73, Clientes: todos consultan y registran.
        ["GET Clientes/Index"] = CualquierRolConSesion,
        ["GET Clientes/Registrar"] = CualquierRolConSesion,
        ["POST Clientes/Registrar"] = CualquierRolConSesion,

        // US-002 y RF-73: Operador, Supervisor y Administrador
        // pueden acceder al formulario y crear presupuestos.
        ["GET Presupuestos/Crear"] = "Administrador,Operador,Supervisor",
        ["POST Presupuestos/Crear"] = "Administrador,Operador,Supervisor",

        // US-001 y RF-73: los tres roles pueden consultar
        // el listado de presupuestos.
        ["GET Presupuestos/Index"] = "Administrador,Operador,Supervisor",


        // US-001 y RF-73: los tres roles pueden acceder
        // al detalle de un presupuesto para consultarlo.
        ["GET Presupuestos/Detalle"] = "Administrador,Operador,Supervisor",

        // US-011 y RF-73: los tres roles consultan. El alta comprueba además
        // el estado del presupuesto y la responsabilidad si se trata de un Operador.
        ["GET ConceptosPresupuesto/Index"] = "Administrador,Operador,Supervisor",
        ["POST ConceptosPresupuesto/Agregar"] = "Administrador,Operador,Supervisor",

        // US-007 y RF-73: los tres roles pueden entrar; el servidor además exige estado editable
        // y, para el Operador, ser el responsable del presupuesto.
        ["GET ConceptosPresupuesto/SeleccionarMaterial"] = "Administrador,Operador,Supervisor",
        ["POST ConceptosPresupuesto/AsignarMaterial"] = "Administrador,Operador,Supervisor",
        ["POST ConceptosPresupuesto/CambiarProveedor"] = "Administrador,Operador,Supervisor",
        ["POST ConceptosPresupuesto/ActualizarReferencia"] = "Administrador,Operador,Supervisor",

        // RF-67 y RF-73: los tres roles pueden consultar materiales.
        // Solamente Supervisor y Administrador pueden registrarlos.

        ["GET Materiales/Index"] = CualquierRolConSesion,
        ["GET Materiales/Registrar"] = AdministradorOSupervisor,
        ["POST Materiales/Registrar"] = AdministradorOSupervisor,
        ["GET Materiales/AgregarProveedor"] = AdministradorOSupervisor,
        ["POST Materiales/AgregarProveedor"] = AdministradorOSupervisor,
        ["GET Materiales/Editar"] = AdministradorOSupervisor,
        ["POST Materiales/Editar"] = AdministradorOSupervisor,


        // US-074: los tres roles pueden consultar proveedores.
        ["GET Proveedores/Index"] = "Administrador,Operador,Supervisor",

        // RF-73: solamente Supervisor y Administrador pueden registrar proveedores.
        ["GET Proveedores/Registrar"] = AdministradorOSupervisor,
        ["POST Proveedores/Registrar"] = AdministradorOSupervisor,

        // RF-72 y RF-73, Usuarios: exclusivamente el Administrador.
        ["GET Usuarios/Index"] = SoloAdministrador,
        ["GET Usuarios/Registrar"] = SoloAdministrador,
        ["POST Usuarios/Registrar"] = SoloAdministrador,
        ["POST Usuarios/ModificarRol"] = SoloAdministrador,
        ["POST Usuarios/CambiarEstado"] = SoloAdministrador,
    };

    private List<(string Accion, Endpoint Endpoint, ControllerActionDescriptor Descriptor)> ObtenerAccionesDeLaApp()
    {
        return fabrica.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(endpoint => (Endpoint: endpoint, Descriptor: endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()))
            .Where(par => par.Descriptor != null && par.Descriptor.ControllerTypeInfo.Assembly == typeof(HomeController).Assembly)
            .Select(par =>
            {
                var verbos = par.Endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
                string verbo = verbos == null || verbos.Count == 0 ? "*" : string.Join(",", verbos.Order());

                return ($"{verbo} {par.Descriptor!.ControllerName}/{par.Descriptor.ActionName}", par.Endpoint, par.Descriptor!);
            })
            .ToList();
    }

    // Autorización efectiva: [AllowAnonymous] gana; sin roles rige la política de respaldo; si Controller y acción
    // piden roles, hay que cumplir todos los atributos, así que quedan los roles comunes a todos ellos.
    private static string CalcularAcceso(Endpoint endpoint)
    {
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            return Publica;
        }

        List<string[]> rolesPorAtributo = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Where(atributo => !string.IsNullOrWhiteSpace(atributo.Roles))
            .Select(atributo => atributo.Roles!.Split(',', StringSplitOptions.TrimEntries))
            .ToList();

        if (rolesPorAtributo.Count == 0)
        {
            return CualquierRolConSesion;
        }

        return string.Join(",", rolesPorAtributo.Aggregate((comunes, roles) => [.. comunes.Intersect(roles)]).Order());
    }

    [Fact]
    public void CadaAccionPublicada_TieneExactamenteElAccesoQueDefineLaMatrizDeRf73()
    {
        List<string> real = ObtenerAccionesDeLaApp()
            .Select(accion => $"{accion.Accion} => {CalcularAcceso(accion.Endpoint)}")
            .Order(StringComparer.Ordinal)
            .ToList();

        List<string> esperado = MatrizEsperada
            .Select(fila => $"{fila.Key} => {fila.Value}")
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(esperado, real);
    }

    [Fact]
    public void CadaPostPublicado_ExigeElTokenAntifalsificacion()
    {
        var postsSinProteccion = ObtenerAccionesDeLaApp()
            .Where(accion => accion.Accion.StartsWith("POST ", StringComparison.Ordinal))
            .Where(accion => accion.Descriptor.MethodInfo.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() == null)
            .Select(accion => accion.Accion);

        Assert.Empty(postsSinProteccion);
    }
}
