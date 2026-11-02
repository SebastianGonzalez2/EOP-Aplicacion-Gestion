using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Tests.Integracion;

// Pruebas de integración de la T14: el POST /Usuarios/ModificarRol pasando por toda la app
// (cookie real, OnValidatePrincipal, [Authorize], token antifalsificación, Controller,
// caso de uso, repositorio real y base con las tablas reales).
//
// Viven en la misma clase que las de la T13 porque comprueban lo mismo que aquellas,
// pero ahora con el cambio de rol hecho de verdad: que los permisos dependen del rol
// guardado en la base. La base es SQLite, así que acá NO se prueban los bloqueos de
// SQL Server: eso está en ModificarRolConcurrenciaSqlServerTests.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    private const string RutaModificarRol = "/Usuarios/ModificarRol";
    private const string ObjectIdAdministrador = "44444444-4444-4444-4444-444444444444";
    private const string CorreoAdministrador = "administrador@eop.com.uy";
    private const string NombreDeCookieAntifalsificacion = ".AspNetCore.Antiforgery.";

    // ---------- Quién puede ejecutar la acción ----------

    [Fact]
    public async Task Administrador_ConDatosValidos_CambiaElRolYVuelveAlListado()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await EnviarModificarRolAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Supervisor");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Usuarios", respuesta.Headers.Location?.OriginalString);
        Assert.Equal(RolUsuario.Supervisor, (await fabrica.LeerUsuarioAsync(idJuan))?.Rol);
    }

    [Theory]
    [InlineData(RolUsuario.Operador)]
    [InlineData(RolUsuario.Supervisor)]
    public async Task UsuarioSinRolAdministrador_ConTokenValido_RecibeAccesoDenegadoYNoCambiaNada(RolUsuario rolDelSolicitante)
    {
        // Es el pedido directo del que habla RNF-4: tiene sesión y token válidos,
        // y aun así el servidor lo frena porque su rol no le alcanza.
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rolDelSolicitante);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        int idOtroOperador = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: null, rol: RolUsuario.Operador, correo: "otro@eop.com.uy");

        var respuesta = await EnviarModificarRolAsync(fabrica, cookieDeSesion, idOtroOperador.ToString(), "Administrador");

        await AssertAccesoDenegadoPorRolAsync(respuesta);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        Assert.Equal(RolUsuario.Operador, (await fabrica.LeerUsuarioAsync(idOtroOperador))?.Rol);
    }

    [Fact]
    public async Task PedidoSinSesion_SeRechazaYNoCambiaNada()
    {
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var formulario = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UsuarioId"] = idJuan.ToString(),
            ["NuevoRol"] = "Administrador"
        });

        var respuesta = await CrearCliente(fabrica).PostAsync(RutaModificarRol, formulario);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(RolUsuario.Operador, (await fabrica.LeerUsuarioAsync(idJuan))?.Rol);
    }

    [Fact]
    public async Task AdministradorSinTokenAntifalsificacion_SeRechazaYNoCambiaNada()
    {
        // Es lo que mandaría otra página que intenta aprovechar la sesión del
        // Administrador (CSRF): su cookie viaja sola, pero el token no lo puede conocer.
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaModificarRol)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UsuarioId"] = idJuan.ToString(),
                ["NuevoRol"] = "Administrador"
            })
        };
        pedido.Headers.Add("Cookie", cookieAdministrador);

        var respuesta = await CrearCliente(fabrica).SendAsync(pedido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(RolUsuario.Operador, (await fabrica.LeerUsuarioAsync(idJuan))?.Rol);
    }

    [Fact]
    public async Task PedidoGet_NoEjecutaLaModificacion()
    {
        // Un enlace o una imagen en otra página generarían un GET: no pueden cambiar datos.
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await PedirConCookieAsync(
            fabrica,
            $"{RutaModificarRol}?UsuarioId={idJuan}&NuevoRol=Administrador",
            cookieAdministrador);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);
        Assert.Equal(RolUsuario.Operador, (await fabrica.LeerUsuarioAsync(idJuan))?.Rol);
    }

    // ---------- Validación de los datos recibidos ----------

    [Theory]
    [InlineData("Gerente")]
    [InlineData("99")]
    [InlineData("0")]
    [InlineData("")]
    [InlineData("Administrador,Supervisor")]
    [InlineData("1,2")]
    public async Task RolQueNoEsDeLosTresFijos_SeRechazaYElRolNoCambia(string rolEnviado)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Supervisor);

        var respuesta = await EnviarModificarRolAsync(fabrica, cookieAdministrador, idJuan.ToString(), rolEnviado);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RolUsuario.Supervisor, (await fabrica.LeerUsuarioAsync(idJuan))?.Rol);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("99999")]
    public async Task UsuarioInvalidoOInexistente_SeRechazaSinTocarANadie(string usuarioIdEnviado)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);

        var respuesta = await EnviarModificarRolAsync(fabrica, cookieAdministrador, usuarioIdEnviado, "Supervisor");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RolUsuario.Operador, (await fabrica.LeerUsuarioAsync(idJuan))?.Rol);
    }

    // ---------- Último Administrador activo ----------

    [Fact]
    public async Task UltimoAdministradorActivo_NoPuedeDegradarseAsiMismoYConservaSusPermisos()
    {
        using var fabrica = new FabricaConSesionReal();
        int idAdministrador = await fabrica.GuardarUsuarioAsync(
            activo: true, entraObjectId: ObjectIdAdministrador, rol: RolUsuario.Administrador, correo: CorreoAdministrador);
        string cookieAdministrador = await IniciarSesionComoAsync(fabrica, ObjectIdAdministrador, CorreoAdministrador);

        var respuesta = await EnviarModificarRolAsync(fabrica, cookieAdministrador, idAdministrador.ToString(), "Operador");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RolUsuario.Administrador, (await fabrica.LeerUsuarioAsync(idAdministrador))?.Rol);
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieAdministrador));
    }

    [Fact]
    public async Task AdministradorConOtroAdministradorActivo_SePuedeDegradar_YPierdeElPermisoEnElPedidoSiguiente()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Administrador);
        string cookieJuan = await IniciarSesionYObtenerCookieAsync(fabrica);
        int idOperador = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: null, rol: RolUsuario.Operador, correo: "otro@eop.com.uy");

        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieJuan));

        await EnviarModificarRolAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Operador");

        Assert.Equal(RolUsuario.Operador, (await fabrica.LeerUsuarioAsync(idJuan))?.Rol);

        // Con la misma cookie, Juan ya no puede usar ni la acción de roles ni otra de Administrador.
        await AssertAccesoDenegadoPorRolAsync(await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieJuan));

        var intentoDeJuan = await EnviarModificarRolAsync(fabrica, cookieJuan, idOperador.ToString(), "Administrador");
        await AssertAccesoDenegadoPorRolAsync(intentoDeJuan);
        Assert.Equal(RolUsuario.Operador, (await fabrica.LeerUsuarioAsync(idOperador))?.Rol);
    }

    // ---------- Los permisos posteriores responden al rol nuevo (T13) ----------

    [Fact]
    public async Task OperadorAscendidoASupervisor_ConLaMismaSesion_PasaATenerLosPermisosDelRolNuevo()
    {
        // El escenario "cambio de rol" de US-044. Juan nunca vuelve a iniciar sesión:
        // lo que cambia sus permisos es lo que OnValidatePrincipal lee de la base.
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieJuan = await IniciarSesionYObtenerCookieAsync(fabrica);

        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloOperador, cookieJuan));
        await AssertAccesoDenegadoPorRolAsync(await PedirConCookieAsync(fabrica, RutaSoloSupervisor, cookieJuan));

        var respuesta = await EnviarModificarRolAsync(fabrica, cookieAdministrador, idJuan.ToString(), "Supervisor");
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloSupervisor, cookieJuan));
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieJuan));

        // Y deja de tener los del rol anterior: el rol es uno solo, no se acumulan.
        await AssertAccesoDenegadoPorRolAsync(await PedirConCookieAsync(fabrica, RutaSoloOperador, cookieJuan));
        await AssertAccesoDenegadoPorRolAsync(await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieJuan));
    }

    [Fact]
    public async Task CambioDeRol_NoAfectaLosPermisosDeOtrosUsuarios()
    {
        // RF-72: "sin alterar los roles o autorizaciones de los demás usuarios".
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieJuan = await IniciarSesionYObtenerCookieAsync(fabrica);
        int idOtroOperador = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: null, rol: RolUsuario.Operador, correo: "otro@eop.com.uy");

        await EnviarModificarRolAsync(fabrica, cookieAdministrador, idOtroOperador.ToString(), "Supervisor");

        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloOperador, cookieJuan));
        await AssertAccesoDenegadoPorRolAsync(await PedirConCookieAsync(fabrica, RutaSoloSupervisor, cookieJuan));
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieAdministrador));
    }

    // ---------- Ayudas ----------

    private static async Task<string> PrepararAdministradorConSesionAsync(FabricaConSesionReal fabrica)
    {
        await fabrica.GuardarUsuarioAsync(
            activo: true, entraObjectId: ObjectIdAdministrador, rol: RolUsuario.Administrador, correo: CorreoAdministrador);

        return await IniciarSesionComoAsync(fabrica, ObjectIdAdministrador, CorreoAdministrador);
    }

    private static async Task<string> IniciarSesionComoAsync(FabricaConSesionReal fabrica, string entraObjectId, string correo)
    {
        var respuesta = await HacerLoginSimuladoAsync(fabrica, entraObjectId, correo);

        string? cookie = ExtraerCookieDeSesion(respuesta);
        Assert.NotNull(cookie);
        return cookie;
    }

    // Hace lo que hará el formulario de US-045: primero abre una página para conseguir
    // el token antifalsificación y después manda el POST con ese token.
    //
    // Como todavía no existe esa pantalla, sacamos el token de la página de inicio:
    // el botón "Cerrar sesión" de la barra de arriba es un formulario que lo incluye.
    private static async Task<HttpResponseMessage> EnviarModificarRolAsync(
        FabricaConSesionReal fabrica, string cookieDeSesion, string usuarioId, string nuevoRol)
    {
        var paginaConFormulario = await PedirConCookieAsync(fabrica, "/", cookieDeSesion);
        string html = await paginaConFormulario.Content.ReadAsStringAsync();

        Match campoDelToken = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(campoDelToken.Success, "La página de inicio no incluyó el token antifalsificación.");

        // El token viaja dos veces: en el formulario y en una cookie. El servidor los compara.
        string cookieAntifalsificacion = paginaConFormulario.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith(NombreDeCookieAntifalsificacion, StringComparison.Ordinal))
            .Split(';')[0];

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaModificarRol)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UsuarioId"] = usuarioId,
                ["NuevoRol"] = nuevoRol,
                ["__RequestVerificationToken"] = campoDelToken.Groups[1].Value
            })
        };
        pedido.Headers.Add("Cookie", $"{cookieDeSesion}; {cookieAntifalsificacion}");

        return await CrearCliente(fabrica).SendAsync(pedido);
    }
}
