using System.Net;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Tests.Integracion;

// S2-T09 (US-046, RF-75) — Un usuario inactivado pierde el acceso aunque ya tuviera la sesión abierta,
// también cuando intenta escribir datos, y vuelve a entrar al reactivarlo con un inicio de sesión nuevo.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task UsuarioInactivadoConLaSesionAbierta_NoPuedeEnviarUnaAltaAunqueTengaElTokenDeAntes(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        ProteccionAntifalsificacion proteccion = await ObtenerProteccionAntifalsificacionAsync(fabrica, RutaRegistrarCliente, cookieDeSesion);

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = false);

        var respuesta = await EnviarPostConProteccionAsync(fabrica, RutaRegistrarCliente, cookieDeSesion, proteccion, DatosDeCliente());

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.True(BorraLaCookieDeSesion(respuesta));
        Assert.Equal(0, await ContarClientesAsync(fabrica));
    }

    [Fact]
    public async Task UsuarioInactivadoYLuegoReactivado_ConUnInicioDeSesionNuevo_VuelveATrabajarNormalmente()
    {
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieVieja = await IniciarSesionYObtenerCookieAsync(fabrica);

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = false);
        await AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(
            await PedirConCookieAsync(fabrica, RutaSoloOperador, cookieVieja));

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = true);

        string cookieNueva = await IniciarSesionYObtenerCookieAsync(fabrica);
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloOperador, cookieNueva));

        ProteccionAntifalsificacion proteccion = await ObtenerProteccionAntifalsificacionAsync(fabrica, RutaRegistrarCliente, cookieNueva);
        var respuesta = await EnviarPostConProteccionAsync(fabrica, RutaRegistrarCliente, cookieNueva, proteccion, DatosDeCliente());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(1, await ContarClientesAsync(fabrica));
    }
}
