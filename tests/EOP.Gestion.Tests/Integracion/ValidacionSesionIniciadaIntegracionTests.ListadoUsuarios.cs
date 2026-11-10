
using System.Net;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Tests.Integracion;

// US-045: comprobamos el listado de usuarios pasando por la aplicación.
// Verificamos los datos mostrados y los permisos de acceso.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    [Fact]
    public async Task Administrador_PuedeConsultarTodosLosUsuarios()
    {
        using var fabrica = new FabricaConSesionReal();

        // Registramos al Administrador que iniciará sesión.
        await fabrica.GuardarUsuarioAsync(
            activo: true,
            entraObjectId: ObjectIdJuan,
            rol: RolUsuario.Administrador);

        // Registramos otro usuario, esta vez inactivo.
        int idInactivo = await fabrica.GuardarUsuarioAsync(
            activo: false,
            entraObjectId: null,
            rol: RolUsuario.Supervisor,
            correo: "maria@eop.com.uy");

        await fabrica.ModificarUsuarioAsync(
            idInactivo,
            usuario => usuario.NombreCompleto = "María Díaz");

        string cookie = await IniciarSesionYObtenerCookieAsync(fabrica);

        // Abrimos el listado como lo haría el Administrador.
        var respuesta = await PedirConCookieAsync(
            fabrica, RutaListadoUsuarios, cookie);

        string html = await LeerHtmlDecodificadoAsync(respuesta);

        // La pantalla debe abrir correctamente.
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        // Deben aparecer los dos usuarios.
        Assert.Contains("Juan Pérez", html);
        Assert.Contains("María Díaz", html);

        // Verificamos los datos del usuario inactivo.
        Assert.Contains("maria@eop.com.uy", html);
        Assert.Contains("Supervisor", html);
        Assert.Contains("Inactivo", html);

        // US-045: no deben existir controles de búsqueda ni filtros.
        Assert.DoesNotContain("name=\"busqueda\"", html);
        Assert.DoesNotContain("name=\"activo\"", html);
    }

    [Theory]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task SupervisorYOperador_NoPuedenConsultarElListado(
        RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();

        await fabrica.GuardarUsuarioAsync(
            activo: true,
            entraObjectId: ObjectIdJuan,
            rol: rol);

        string cookie = await IniciarSesionYObtenerCookieAsync(fabrica);

        // Intentamos acceder directamente por URL.
        var respuesta = await PedirConCookieAsync(
            fabrica, RutaListadoUsuarios, cookie);

        // El servidor debe rechazar el acceso.
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        Assert.Equal(
            "/Cuenta/AccesoDenegado",
            respuesta.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task UsuarioSinSesion_NoPuedeConsultarElListado()
    {
        using var fabrica = new FabricaConSesionReal();

        // Hacemos el pedido sin una cookie de sesión.
        var respuesta = await CrearCliente(fabrica)
            .GetAsync(RutaListadoUsuarios);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
