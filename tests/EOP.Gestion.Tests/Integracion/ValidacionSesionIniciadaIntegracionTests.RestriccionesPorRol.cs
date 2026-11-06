using System.Net;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Integracion;

// S1-T30 — Restricciones por rol sobre rutas reales que hasta ahora solo se comprobaban leyendo el atributo
// (Materiales) o con una sesión de prueba sin OnValidatePrincipal (Clientes), y operaciones reales enviadas
// con un token válido por alguien que dejó de estar habilitado después de obtenerlo.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    private const string RutaRegistrarMaterial = "/Materiales/Registrar";
    private const string RutaRegistrarCliente = "/Clientes/Registrar";
    private const string TextoDelPostDeMaterialesEjecutado = "El registro todavía no está disponible";

    private static Dictionary<string, string> DatosDeMaterial() => new()
    {
        ["Nombre"] = "Material de prueba",
        ["Unidad"] = "1",
        ["ProveedorId"] = "1",
        ["Precio"] = "10",
        ["Moneda"] = "1"
    };

    private static Dictionary<string, string> DatosDeCliente() => new()
    {
        ["Nombre"] = "Cliente de prueba",
        ["Rut"] = "215521750017",
        ["CorreoContacto"] = "cliente@ejemplo.com",
        ["PaisWhatsApp"] = "UY",
        ["NumeroNacionalWhatsApp"] = "099123456"
    };

    private static async Task<HttpResponseMessage> EnviarPostConProteccionAsync(
        FabricaConSesionReal fabrica, string ruta, string cookieDeSesion, ProteccionAntifalsificacion proteccion, Dictionary<string, string> datos)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Post, ruta)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>(datos)
            {
                ["__RequestVerificationToken"] = proteccion.Token
            })
        };
        pedido.Headers.Add("Cookie", $"{cookieDeSesion}; {proteccion.Cookie}");

        return await CrearCliente(fabrica).SendAsync(pedido);
    }

    private static async Task<int> ContarClientesAsync(FabricaConSesionReal fabrica)
    {
        using var alcance = fabrica.Services.CreateScope();
        return await alcance.ServiceProvider.GetRequiredService<EopDbContext>().Set<Cliente>().CountAsync();
    }

    // ---------- Materiales (RF-73: registran Supervisor y Administrador) ----------

    [Theory]
    [InlineData(RolUsuario.Administrador, true)]
    [InlineData(RolUsuario.Supervisor, true)]
    [InlineData(RolUsuario.Operador, false)]
    public async Task AltaDeMateriales_SoloLaAbrenAdministradorYSupervisor(RolUsuario rol, bool permitido)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaRegistrarMaterial, cookieDeSesion);

        if (permitido)
        {
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        }
    }

    [Fact]
    public async Task Operador_ConSesionYTokenValidos_NoPuedeEnviarElAltaDeMateriales()
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        ProteccionAntifalsificacion proteccion = await ObtenerProteccionAntifalsificacionAsync(fabrica, "/", cookieDeSesion);

        var respuesta = await EnviarPostConProteccionAsync(fabrica, RutaRegistrarMaterial, cookieDeSesion, proteccion, DatosDeMaterial());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        Assert.DoesNotContain(TextoDelPostDeMaterialesEjecutado, await respuesta.Content.ReadAsStringAsync());
    }

    // ---------- Clientes (RF-73: todos registran) ----------

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task CadaRol_PuedeAbrirYEnviarElAltaDeClientes(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        ProteccionAntifalsificacion proteccion = await ObtenerProteccionAntifalsificacionAsync(fabrica, RutaRegistrarCliente, cookieDeSesion);

        var respuesta = await EnviarPostConProteccionAsync(fabrica, RutaRegistrarCliente, cookieDeSesion, proteccion, DatosDeCliente());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RutaRegistrarCliente, respuesta.Headers.Location?.OriginalString);
        Assert.Equal(1, await ContarClientesAsync(fabrica));
    }

    [Theory]
    [InlineData(RutaRegistrarCliente)]
    [InlineData(RutaRegistrarMaterial)]
    public async Task PostSinSesion_ARutasRealesProtegidas_SeRechazaSinEjecutarNada(string ruta)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        Dictionary<string, string> datos = ruta == RutaRegistrarCliente ? DatosDeCliente() : DatosDeMaterial();

        var respuesta = await CrearCliente(fabrica).PostAsync(ruta, new FormUrlEncodedContent(datos));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(0, await ContarClientesAsync(fabrica));
    }

    // ---------- Sesión que deja de valer con un token ya obtenido ----------

    [Theory]
    [InlineData("inactivado")]
    [InlineData("desvinculado")]
    public async Task AdministradorQueDejaDeEstarHabilitado_ConElFormularioYElTokenDeAntes_NoPuedeCambiarRoles(string situacion)
    {
        // Las pruebas de roles anteriores piden el token justo antes de enviar, y un usuario rechazado ya no lo
        // puede conseguir. Acá lo obtuvo cuando todavía estaba habilitado: es la pantalla que quedó abierta.
        using var fabrica = new FabricaConSesionReal();
        int idPrimerAdministrador = await fabrica.GuardarUsuarioAsync(
            activo: true, entraObjectId: ObjectIdAdministrador, rol: RolUsuario.Administrador, correo: CorreoAdministrador);
        int idSegundoAdministrador = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Administrador);
        string cookieDeSesion = await IniciarSesionComoAsync(fabrica, ObjectIdAdministrador, CorreoAdministrador);
        ProteccionAntifalsificacion proteccion = await ObtenerProteccionAntifalsificacionAsync(fabrica, "/", cookieDeSesion);

        if (situacion == "inactivado")
        {
            await fabrica.ModificarUsuarioAsync(idPrimerAdministrador, usuario => usuario.Activo = false);
        }
        else
        {
            await fabrica.ModificarUsuarioAsync(idPrimerAdministrador, usuario => usuario.EntraObjectId = null);
        }

        var respuesta = await EnviarPostConProteccionAsync(fabrica, RutaModificarRol, cookieDeSesion, proteccion, new Dictionary<string, string>
        {
            ["UsuarioId"] = idSegundoAdministrador.ToString(),
            ["NuevoRol"] = "Operador"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.True(BorraLaCookieDeSesion(respuesta));
        Assert.Equal(RolUsuario.Administrador, (await fabrica.LeerUsuarioAsync(idSegundoAdministrador))?.Rol);
    }

    [Fact]
    public async Task UltimoAdministradorActivo_NoPuedeSerDegradadoPorHttpAunqueExistaOtroAdministradorInactivo()
    {
        // Un Administrador inactivo no sostiene al sistema: la cuenta real tiene que ignorarlo.
        using var fabrica = new FabricaConSesionReal();
        int idAdministrador = await fabrica.GuardarUsuarioAsync(
            activo: true, entraObjectId: ObjectIdAdministrador, rol: RolUsuario.Administrador, correo: CorreoAdministrador);
        await fabrica.GuardarUsuarioAsync(activo: false, entraObjectId: ObjectIdJuan, rol: RolUsuario.Administrador);
        string cookieAdministrador = await IniciarSesionComoAsync(fabrica, ObjectIdAdministrador, CorreoAdministrador);

        var respuesta = await EnviarModificarRolAsync(fabrica, cookieAdministrador, idAdministrador.ToString(), "Supervisor");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RolUsuario.Administrador, (await fabrica.LeerUsuarioAsync(idAdministrador))?.Rol);
    }
}
