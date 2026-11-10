using System.Net;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Tests.Integracion;

// S1-T24 (US-074): acceso a la sección Proveedores con la sesión real. Los tres roles consultan el listado;
// sin sesión no se entra. El alta (solo Administrador y Supervisor) se prueba en
// ValidacionSesionIniciadaIntegracionTests.RegistrarProveedor.cs.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    private const string RutaListadoProveedores = "/Proveedores";

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task LosTresRoles_PuedenConsultarElListadoDeProveedores(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaListadoProveedores, cookieDeSesion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Listado de proveedores", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PedidoSinSesion_NoPuedeConsultarElListadoDeProveedores()
    {
        using var fabrica = new FabricaConSesionReal();

        var respuesta = await CrearCliente(fabrica).GetAsync(RutaListadoProveedores);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task UsuarioInactivadoConLaSesionAbierta_PierdeElAccesoAlListadoDeProveedores()
    {
        // El acceso depende del usuario guardado HOY en la base: si lo inactivan, la cookie que ya tenía no alcanza.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var antes = await PedirConCookieAsync(fabrica, RutaListadoProveedores, cookieDeSesion);
        Assert.Equal(HttpStatusCode.OK, antes.StatusCode);

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = false);

        var despues = await PedirConCookieAsync(fabrica, RutaListadoProveedores, cookieDeSesion);
        Assert.NotEqual(HttpStatusCode.OK, despues.StatusCode);
    }
}
