using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Clientes;
using EOP.Gestion.Infrastructure.Telefonia;
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Core.Interfaces.Materiales;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Materiales;
using EOP.Gestion.Core.Interfaces.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Materiales;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
namespace EOP.Gestion.Infrastructure;

/// <summary>
/// Punto único de registro de los servicios de Infrastructure en el contenedor.
/// </summary>
public static class InyeccionDeDependencias
{
    public const string NombreCadenaConexion = "EopDb";

    public static IServiceCollection AgregarInfraestructura(this IServiceCollection services,IConfiguration configuration)
    {
        // La cadena nunca se versiona (RNF-2 / RNF-11): en desarrollo viene de
        // User Secrets y en Azure de la configuración del App Service.
        var cadenaConexion = configuration.GetConnectionString(NombreCadenaConexion);

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{NombreCadenaConexion}'. En desarrollo, configurala con: " +
                $"dotnet user-secrets set \"ConnectionStrings:{NombreCadenaConexion}\" \"<cadena>\" " +
                "--project src/EOP.Gestion.WebApp");
        }

        services.AddDbContext<EopDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));
        //AddDbContext<EopDbContext>: registra nuestro contexto para que la aplicación pueda utilizarlo.
        //UseSqlServer(cadenaConexion): indica que trabajaremos con SQL Server y proporciona los datos de conexión.

        // Registramos el repositorio de clientes para consultar sus datos.
        services.AddScoped<IRepositorioClientes, RepositorioClientes>();

        // Registramos el servicio encargado de consultar
        // y filtrar los clientes para el listado.
        services.AddScoped<ConsultarClientes>();

        // Registramos el servicio que consulta y filtra los materiales
        // por nombre, unidad y proveedor para el listado del catálogo.
        services.AddScoped<ConsultarMateriales>();

        // Registramos el servicio que consulta los proveedores existentes
        // para mostrarlos en el selector del formulario de materiales.
        services.AddScoped<ConsultarProveedoresParaSeleccion>();

        // Registramos el repositorio de proveedores para comprobar duplicados de nombre y RUT
        // y para guardar los proveedores nuevos.
        // También lo utilizaremos para comprobar que el proveedor
        // seleccionado al registrar un material exista.
        services.AddScoped<IRepositorioProveedores, RepositorioProveedores>();

        // Singleton: arma la lista de países una sola vez y no guarda nada de cada pedido,
        // así que todos los pedidos pueden compartir la misma instancia sin pisarse.
        services.AddSingleton<IServicioTelefonos, ServicioTelefonos>();

        services.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        //Cuando alguna clase necesite un IRepositorioUsuarios, creá y proporcionale una instancia de RepositorioUsuarios.


        // Registramos el repositorio de materiales.
        // Cuando una clase necesite IRepositorioMateriales,
        // ASP.NET Core le proporcionará RepositorioMateriales.
        services.AddScoped<IRepositorioMateriales, RepositorioMateriales>();

        // Registramos el repositorio de presupuestos, que guarda el presupuesto
        // y le asigna su código interno.
        services.AddScoped<IRepositorioPresupuestos, RepositorioPresupuestos>();

        // Registramos el servicio que comprueba que el cliente
        // esté activo y que el tipo de proyecto exista.
        services.AddScoped<IConsultaReferenciasPresupuesto, ConsultaReferenciasPresupuesto>();


        // Consulta los tipos de proyecto disponibles
        // para el formulario de creación de presupuestos.
        services.AddScoped<ConsultarTiposProyectoParaSeleccion>();

        return services;
    }
}
