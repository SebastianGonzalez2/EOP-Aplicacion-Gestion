using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Tests.Integracion;

// S2-T07 / US-046: el POST /Usuarios/CambiarEstado pasando por toda la app (cookie real,
// OnValidatePrincipal, [Authorize], token antifalsificación, Controller, caso de uso,
// repositorio real y base con las tablas reales) y el listado con su diálogo de confirmación.
// La base es SQLite: los bloqueos de SQL Server se prueban en CambiarEstadoConcurrenciaSqlServerTests.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    private const string RutaCambiarEstado = "/Usuarios/CambiarEstado";

    // ---------- Inactivar y reactivar ----------

    [Fact]
    public async Task Administrador_InactivaAUnUsuario_ElRegistroSeConservaYElListadoLoMuestraInactivo()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Supervisor);

        var respuesta = await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Inactivo");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Usuarios", respuesta.Headers.Location?.OriginalString);

        // El mismo registro, con todos sus datos: inactivar no elimina ni modifica nada más.
        Usuario? juan = await fabrica.LeerUsuarioAsync(idJuan);
        Assert.NotNull(juan);
        Assert.False(juan.Activo);
        Assert.Equal(RolUsuario.Supervisor, juan.Rol);
        Assert.Equal(ObjectIdJuan, juan.EntraObjectId);
        Assert.Equal(CorreoJuan, juan.CorreoCorporativo);

        string listado = await LeerListadoTrasElCambioAsync(fabrica, cookieAdministrador, respuesta);
        Assert.Contains("El usuario se inactivó correctamente.", listado);
        Assert.Contains("estado-cerrado\">Inactivo", listado);
    }

    [Fact]
    public async Task Administrador_ReactivaAUnUsuarioInactivo_VuelveAActivo()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: false, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Activo");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await EstaActivoAsync(fabrica, idJuan));

        string listado = await LeerListadoTrasElCambioAsync(fabrica, cookieAdministrador, respuesta);
        Assert.Contains("El usuario se reactivó correctamente.", listado);
    }

    [Fact]
    public async Task UsuarioInactivadoDesdeLaPantalla_PierdeElAccesoConLaSesionQueYaTenia()
    {
        // Es la consecuencia de inactivar según RF-75. La integración completa con el control
        // de acceso es de S2-T09: acá solo comprobamos que el cambio lo alcanza.
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieJuan = await IniciarSesionYObtenerCookieAsync(fabrica);

        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloOperador, cookieJuan));

        await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Inactivo");

        await AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(
            await PedirConCookieAsync(fabrica, RutaSoloOperador, cookieJuan));
    }

    [Fact]
    public async Task UsuarioQueYaTeniaEseEstado_SeAdvierteYNoCambiaNada()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: false, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Inactivo");

        Assert.False(await EstaActivoAsync(fabrica, idJuan));

        string listado = await LeerListadoTrasElCambioAsync(fabrica, cookieAdministrador, respuesta);
        Assert.Contains("El usuario ya se encontraba Inactivo. No se realizaron cambios.", listado);
    }

    [Fact]
    public async Task CambiarElEstadoDeUnUsuario_NoAfectaAlRestoDeLosUsuarios()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        int idOtro = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: null, rol: RolUsuario.Operador, correo: "otro@eop.com.uy");

        await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Inactivo");

        Assert.True(await EstaActivoAsync(fabrica, idOtro));
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieAdministrador));
    }

    // ---------- Quién puede ejecutar la acción ----------

    [Theory]
    [InlineData(RolUsuario.Operador)]
    [InlineData(RolUsuario.Supervisor)]
    public async Task CambiarEstado_UsuarioSinRolAdministrador_ConTokenValido_RecibeAccesoDenegadoYNoCambiaNada(RolUsuario rolDelSolicitante)
    {
        // RNF-4: tiene sesión y token válidos, y aun así el servidor lo frena porque su rol no le alcanza.
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rolDelSolicitante);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        int idOtro = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: null, rol: RolUsuario.Operador, correo: "otro@eop.com.uy");

        var respuesta = await EnviarCambiarEstadoAsync(fabrica, cookieDeSesion, idOtro.ToString(), "Inactivo");

        await AssertAccesoDenegadoPorRolAsync(respuesta);
        Assert.True(await EstaActivoAsync(fabrica, idOtro));
    }

    [Fact]
    public async Task CambiarEstado_PedidoSinSesion_SeRechazaYNoCambiaNada()
    {
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var formulario = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UsuarioId"] = idJuan.ToString(),
            ["NuevoEstado"] = "Inactivo"
        });

        var respuesta = await CrearCliente(fabrica).PostAsync(RutaCambiarEstado, formulario);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.True(await EstaActivoAsync(fabrica, idJuan));
    }

    [Fact]
    public async Task CambiarEstado_AdministradorSinTokenAntifalsificacion_SeRechazaYNoCambiaNada()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaCambiarEstado)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UsuarioId"] = idJuan.ToString(),
                ["NuevoEstado"] = "Inactivo"
            })
        };
        pedido.Headers.Add("Cookie", cookieAdministrador);

        var respuesta = await CrearCliente(fabrica).SendAsync(pedido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(await EstaActivoAsync(fabrica, idJuan));
    }

    [Fact]
    public async Task CambiarEstado_PedidoGet_NoEjecutaElCambio()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await PedirConCookieAsync(
            fabrica,
            $"{RutaCambiarEstado}?UsuarioId={idJuan}&NuevoEstado=Inactivo",
            cookieAdministrador);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.True(await EstaActivoAsync(fabrica, idJuan));
    }

    // ---------- Validación de los datos recibidos ----------

    [Theory]
    [InlineData("")]
    [InlineData("inactivo")]
    [InlineData("INACTIVO")]
    [InlineData("Inactivo ")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("Activo,Inactivo")]
    [InlineData("Eliminado")]
    public async Task EstadoQueNoEsActivoNiInactivo_SeRechazaYNadieCambia(string estadoEnviado)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idJuan.ToString(), estadoEnviado);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await EstaActivoAsync(fabrica, idJuan));

        string listado = await LeerListadoTrasElCambioAsync(fabrica, cookieAdministrador, respuesta);
        Assert.Contains("No se pudo cambiar el estado: los datos enviados no son válidos.", listado);
    }

    [Fact]
    public async Task PedidoSinElCampoEstado_NoInactivaAlUsuario()
    {
        // Si el estado fuera un bool, un campo ausente quedaría en false (Inactivo).
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idJuan.ToString(), nuevoEstado: null);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await EstaActivoAsync(fabrica, idJuan));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("99999")]
    public async Task CambiarEstado_UsuarioInvalidoOInexistente_SeRechazaSinTocarANadie(string usuarioIdEnviado)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, usuarioIdEnviado, "Inactivo");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await EstaActivoAsync(fabrica, idJuan));
    }

    // ---------- Último Administrador activo ----------

    [Fact]
    public async Task UltimoAdministradorActivo_NoPuedeInactivarseASiMismoYConservaSusPermisos()
    {
        using var fabrica = new FabricaConSesionReal();
        int idAdministrador = await fabrica.GuardarUsuarioAsync(
            activo: true, entraObjectId: ObjectIdAdministrador, rol: RolUsuario.Administrador, correo: CorreoAdministrador);
        string cookieAdministrador = await IniciarSesionComoAsync(fabrica, ObjectIdAdministrador, CorreoAdministrador);

        var respuesta = await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idAdministrador.ToString(), "Inactivo");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await EstaActivoAsync(fabrica, idAdministrador));
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieAdministrador));

        string listado = await LeerListadoTrasElCambioAsync(fabrica, cookieAdministrador, respuesta);
        Assert.Contains("No se puede inactivar al usuario: es el último Administrador activo del sistema.", listado);
    }

    [Fact]
    public async Task AdministradorConOtroAdministradorActivo_SePuedeInactivar_YPierdeElAccesoEnElPedidoSiguiente()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Administrador);
        string cookieJuan = await IniciarSesionYObtenerCookieAsync(fabrica);

        await EnviarCambiarEstadoAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Inactivo");

        Assert.False(await EstaActivoAsync(fabrica, idJuan));
        await AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(
            await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieJuan));
    }

    // ---------- Confirmación en el listado (RNF-1, RF-75) ----------

    [Fact]
    public async Task Listado_InactivarSoloAbreElDialogoDeConfirmacion_NoEnviaNadaPorSiMismo()
    {
        // Cancelar el diálogo es cerrarlo en el navegador: nunca llega un POST al servidor.
        // Lo que comprobamos acá es que el botón de la fila no envía el cambio por sí solo
        // y que mostrar el listado no modifica a nadie.
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        string html = await LeerHtmlDecodificadoAsync(await PedirConCookieAsync(fabrica, RutaListadoUsuarios, cookieAdministrador));

        // El botón de la fila abre el diálogo.
        Assert.Matches(
            $"<button type=\"button\"[^>]*data-bs-toggle=\"modal\"[^>]*data-bs-target=\"#confirmarInactivar-{idJuan}\"", html);

        // El diálogo pregunta, tiene «Cancelar» y recién su botón principal envía el formulario.
        string dialogo = ExtraerDialogoDeConfirmacion(html, idJuan);
        Assert.Contains("¿Inactivar al usuario Juan Pérez?", dialogo);
        Assert.Contains("data-bs-dismiss=\"modal\"", dialogo);
        Assert.Matches(@">\s*Cancelar\s*<", dialogo);
        Assert.Matches("<button type=\"submit\" class=\"btn btn-danger\">Inactivar usuario</button>", dialogo);
        Assert.Contains("name=\"NuevoEstado\" value=\"Inactivo\"", dialogo);

        // Ningún formulario fuera de los diálogos envía "Inactivo".
        string sinDialogos = Regex.Replace(html, "<div class=\"modal fade\".*?</form>", string.Empty, RegexOptions.Singleline);
        Assert.DoesNotContain("value=\"Inactivo\"", sinDialogos);

        Assert.True(await EstaActivoAsync(fabrica, idJuan));
    }

    [Fact]
    public async Task Listado_UsuarioInactivo_OfreceReactivarSinConfirmacionYNoTieneDialogo()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: false, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        string html = await LeerHtmlDecodificadoAsync(await PedirConCookieAsync(fabrica, RutaListadoUsuarios, cookieAdministrador));

        Assert.DoesNotContain($"confirmarInactivar-{idJuan}", html);
        Assert.Contains("name=\"NuevoEstado\" value=\"Activo\"", html);
        Assert.Matches(@">\s*Reactivar\s*<", html);
    }

    // ---------- Ayudas ----------

    private static async Task<bool> EstaActivoAsync(FabricaConSesionReal fabrica, int usuarioId)
    {
        Usuario? usuario = await fabrica.LeerUsuarioAsync(usuarioId);
        Assert.NotNull(usuario);
        return usuario.Activo;
    }

    // Después del POST, el navegador sigue la redirección al listado y le manda la cookie de
    // TempData, que es por donde viaja el mensaje de éxito o de error.
    private static async Task<string> LeerListadoTrasElCambioAsync(
        FabricaConSesionReal fabrica, string cookieDeSesion, HttpResponseMessage respuestaDelPost)
    {
        string cookieTempData = respuestaDelPost.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith(".AspNetCore.Mvc.CookieTempDataProvider", StringComparison.Ordinal))
            .Split(';')[0];

        return await LeerHtmlDecodificadoAsync(
            await PedirConCookieAsync(fabrica, RutaListadoUsuarios, $"{cookieDeSesion}; {cookieTempData}"));
    }

    private static string ExtraerDialogoDeConfirmacion(string html, int usuarioId)
    {
        Match dialogo = Regex.Match(
            html,
            $"<div class=\"modal fade\"\\s+id=\"confirmarInactivar-{usuarioId}\".*?</form>",
            RegexOptions.Singleline);

        Assert.True(dialogo.Success, "El listado no incluyó el diálogo de confirmación.");
        return dialogo.Value;
    }

    // Igual que el formulario real: primero abre el listado para conseguir el token antifalsificación
    // (cada formulario de la pantalla lo incluye) y después manda el POST con ese token.
    private static async Task<HttpResponseMessage> EnviarCambiarEstadoAsync(
        FabricaConSesionReal fabrica, string cookieDeSesion, string usuarioId, string? nuevoEstado)
    {
        var paginaConFormulario = await PedirConCookieAsync(fabrica, "/", cookieDeSesion);
        string html = await paginaConFormulario.Content.ReadAsStringAsync();

        Match campoDelToken = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(campoDelToken.Success, "La página no incluyó el token antifalsificación.");

        string cookieAntifalsificacion = paginaConFormulario.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith(NombreDeCookieAntifalsificacion, StringComparison.Ordinal))
            .Split(';')[0];

        var campos = new Dictionary<string, string>
        {
            ["UsuarioId"] = usuarioId,
            ["__RequestVerificationToken"] = campoDelToken.Groups[1].Value
        };

        if (nuevoEstado != null)
        {
            campos["NuevoEstado"] = nuevoEstado;
        }

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaCambiarEstado)
        {
            Content = new FormUrlEncodedContent(campos)
        };
        pedido.Headers.Add("Cookie", $"{cookieDeSesion}; {cookieAntifalsificacion}");

        return await CrearCliente(fabrica).SendAsync(pedido);
    }
}
