using System.Security.Claims;
using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Autorizacion;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Tests.Unitarias;

// Pruebas del manejador que Program.cs engancha en OnValidatePrincipal de la
// cookie de sesión (T11).
//
// ¿Qué es OnValidatePrincipal? Cada vez que llega un pedido con la cookie, ASP.NET
// Core la abre, arma la identidad del usuario y, ANTES de dejarlo pasar, nos
// pregunta: "¿esta identidad sigue valiendo?". Ahí el manejador consulta si el
// usuario sigue habilitado en EOP. Si no, llama a RejectPrincipal (para este
// pedido el usuario pasa a ser anónimo) y borra la cookie (SignOutAsync).
//
// Acá lo probamos solo, sin levantar la app: armamos a mano el mismo objeto que
// ASP.NET Core le pasa (CookieValidatePrincipalContext) con servicios falsos:
// - un repositorio en memoria que anota lo que le consultan;
// - un servicio de autenticación que anota los cierres de sesión, en vez de
//   escribir encabezados Set-Cookie (eso se prueba en las de integración);
// - un registro de logs en memoria.
public class ManejadorDeValidacionDeAccesoConSesionIniciadaTests
{
    private const string ObjectIdJuan = "11111111-1111-1111-1111-111111111111";
    private const string ObjectIdSinUsuarioEnEop = "99999999-9999-9999-9999-999999999999";
    private const string CorreoJuan = "juan.perez@eop.com.uy";
    private const string IdSolicitud = "solicitud-t11-0001";
    private const string CategoriaDeLogs = "ValidacionSesiones";

    // Microsoft.Identity.Web reconoce el Object ID con cualquiera de estos dos
    // nombres de claim: el corto ("oid") y el largo, que es el que queda cuando
    // ASP.NET Core "traduce" los nombres de los claims del token.
    private const string ClaimOidCorto = "oid";
    private const string ClaimOidLargo = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    // La excepción simulada trae datos personales en su mensaje a propósito:
    // así comprobamos que no terminan en el log.
    private const string MensajeErrorConDatosSensibles =
        "Fallo simulado para " + CorreoJuan + " con " + ObjectIdJuan;

    // ---------- Usuario habilitado: conserva la sesión ----------

    [Theory]
    [InlineData(ClaimOidCorto)]
    [InlineData(ClaimOidLargo)]
    public async Task UsuarioVinculadoYActivo_ConservaLaSesionSinBorrarLaCookieNiRegistrarAdvertencias(string nombreDelClaimOid)
    {
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: true));
        var contexto = escenario.CrearContexto(new Claim(nombreDelClaimOid, ObjectIdJuan));
        var principalOriginal = contexto.Principal;

        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto);

        // La identidad sigue siendo la misma: el pedido continúa autenticado.
        Assert.Same(principalOriginal, contexto.Principal);
        Assert.Empty(escenario.Autenticacion.EsquemasConSesionCerrada);
        Assert.DoesNotContain(escenario.Logs.Entradas, e => e.Nivel >= LogLevel.Warning);

        // Y lo decidió consultando la base con el Object ID de la cookie.
        Assert.Equal([ObjectIdJuan], escenario.Repositorio.IdentificadoresConsultados);
    }

    [Fact]
    public async Task UsuarioHabilitado_LaConsultaUsaElTokenDeCancelacionDeLaSolicitud()
    {
        // Si el navegador abandona el pedido, la consulta a la base también se
        // tiene que cortar: para eso el token de RequestAborted debe llegar al repositorio.
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: true));
        using var cancelacionDeLaSolicitud = new CancellationTokenSource();
        var contexto = escenario.CrearContexto(new Claim(ClaimOidCorto, ObjectIdJuan));
        contexto.HttpContext.RequestAborted = cancelacionDeLaSolicitud.Token;

        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto);

        Assert.Equal(cancelacionDeLaSolicitud.Token, Assert.Single(escenario.Repositorio.TokensRecibidos));
    }

    // ---------- Rol de la sesión (T13) ----------

    [Fact]
    public async Task SesionConRolesAnteriores_SoloConservaElRolActualGuardadoEnEop()
    {
        // La cookie puede traer roles viejos: por ejemplo, cuando ASP.NET Core la
        // renueva, guarda también el rol que tenía en ese momento. Ninguno puede
        // sobrevivir, en ninguna de las identidades de la sesión. La segunda usa
        // otro nombre de claim para el rol ("roles", el que usa Entra ID) porque
        // cada identidad define cuál es el suyo.
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: true));
        var contexto = escenario.CrearContexto(
            new Claim(ClaimOidCorto, ObjectIdJuan),
            new Claim(ClaimTypes.Role, nameof(RolUsuario.Administrador)));
        contexto.Principal!.AddIdentity(new ClaimsIdentity(
            [new Claim("roles", nameof(RolUsuario.Supervisor))], "OtraIdentidad", "name", "roles"));

        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto);

        // CrearUsuario lo guarda como Operador: ese es el único rol que tiene que quedar.
        Assert.True(contexto.Principal!.IsInRole(nameof(RolUsuario.Operador)));
        Assert.False(contexto.Principal.IsInRole(nameof(RolUsuario.Administrador)));
        Assert.False(contexto.Principal.IsInRole(nameof(RolUsuario.Supervisor)));
    }

    [Fact]
    public async Task IdentidadConOtroTipoDeClaimDeRol_ElRolActualSeAgregaConElTipoQueEsaIdentidadReconoce()
    {
        // [Authorize(Roles = ...)] pregunta con el RoleClaimType de cada identidad. Hoy el flujo real usa el tipo
        // estándar, pero depende de cómo Microsoft.Identity.Web mapea los claims: con un tipo fijo, un cambio ahí
        // dejaría a todos los usuarios sin rol.
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: true));
        var contexto = escenario.CrearContexto(new Claim(ClaimOidCorto, ObjectIdJuan));
        contexto.Principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimOidCorto, ObjectIdJuan)], "Prueba", "name", "roles"));

        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto);

        Assert.True(contexto.Principal!.IsInRole(nameof(RolUsuario.Operador)));
    }

    // ---------- Usuario no habilitado: se rechaza la sesión ----------

    [Fact]
    public async Task UsuarioInactivo_RechazaLaIdentidadYBorraLaCookieDeSesion()
    {
        // Escenario "Usuario inactivo" de la US-042, con la sesión ya iniciada:
        // Juan entró cuando estaba Activo y después un Administrador lo inactivó.
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: false));
        var contexto = escenario.CrearContexto(new Claim(ClaimOidCorto, ObjectIdJuan));

        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto);

        AssertSesionRechazadaYCookieBorrada(escenario, contexto);
    }

    [Fact]
    public async Task IdentidadSinUsuarioDeEopVinculado_RechazaLaIdentidadYBorraLaCookieDeSesion()
    {
        // Escenario "Identidad no registrada en EOP": la cookie es válida (la
        // firmó la app), pero el Object ID ya no corresponde a ningún usuario.
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: true));
        var contexto = escenario.CrearContexto(new Claim(ClaimOidCorto, ObjectIdSinUsuarioEnEop));

        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto);

        AssertSesionRechazadaYCookieBorrada(escenario, contexto);
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    public async Task SesionSinObjectIdUtilizable_SeRechazaSinConsultarLaBase(bool incluirClaimOid, string valorDelClaimOid)
    {
        // Una cookie sin Object ID (o con uno vacío) no permite saber quién es
        // el usuario de EOP. Se rechaza, aunque la identidad traiga un correo
        // que coincida con un usuario activo: en una sesión ya iniciada no se
        // identifica a nadie por correo.
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: true));

        var claims = new List<Claim> { new("preferred_username", CorreoJuan), new("name", "Juan Pérez") };
        if (incluirClaimOid)
        {
            claims.Add(new Claim(ClaimOidCorto, valorDelClaimOid));
        }

        var contexto = escenario.CrearContexto([.. claims]);

        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto);

        AssertSesionRechazadaYCookieBorrada(escenario, contexto);
        Assert.Empty(escenario.Repositorio.IdentificadoresConsultados);
        Assert.Equal(0, escenario.Repositorio.CantidadDeBusquedasPorCorreo);
    }

    [Fact]
    public async Task UsuarioNoHabilitado_RegistraUnaAdvertenciaConElIdDeSolicitudYSinDatosPersonales()
    {
        // RNF-12: el rechazo queda registrado. La identidad de la prueba trae
        // nombre y correo a propósito, para comprobar que no van al log.
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: false));
        var contexto = escenario.CrearContexto(
            new Claim(ClaimOidCorto, ObjectIdJuan),
            new Claim("preferred_username", CorreoJuan),
            new Claim("name", "Juan Pérez"));

        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto);

        var advertencia = Assert.Single(escenario.Logs.Entradas,
            e => e.Categoria == CategoriaDeLogs && e.Nivel == LogLevel.Warning);
        Assert.Contains("usuario no habilitado", advertencia.Mensaje);
        Assert.Contains(IdSolicitud, advertencia.Mensaje);
        AssertSinDatosSensibles(advertencia);
        Assert.DoesNotContain("Juan", advertencia.Mensaje);

        // Un acceso rechazado no es una falla del sistema: no se registra como error.
        Assert.DoesNotContain(escenario.Logs.Entradas, e => e.Nivel >= LogLevel.Error);
    }

    [Fact]
    public async Task UsuarioInactivoYUsuarioInexistente_DejanExactamenteElMismoMensajeEnElLog()
    {
        // Igual que en la T10: el log no distingue "inactivo" de "no existe".
        var escenarioInactivo = new EscenarioDeValidacion();
        escenarioInactivo.Repositorio.Usuarios.Add(CrearUsuario(activo: false));
        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(
            escenarioInactivo.CrearContexto(new Claim(ClaimOidCorto, ObjectIdJuan)));

        var escenarioInexistente = new EscenarioDeValidacion();
        await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(
            escenarioInexistente.CrearContexto(new Claim(ClaimOidCorto, ObjectIdJuan)));

        Assert.Equal(
            Assert.Single(escenarioInactivo.Logs.Entradas, e => e.Categoria == CategoriaDeLogs).Mensaje,
            Assert.Single(escenarioInexistente.Logs.Entradas, e => e.Categoria == CategoriaDeLogs).Mensaje);
    }

    // ---------- Fallas al verificar: nunca se deja pasar ----------

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(TimeoutException))]
    // Una cancelación que NO pidió el navegador (por ejemplo, un tiempo de
    // espera interno) sí es una falla: se registra como error.
    [InlineData(typeof(TaskCanceledException))]
    public async Task FallaAlConsultarLaBase_NoDejaEscaparLaExcepcionRechazaLaSesionYRegistraElTipoDeError(Type tipoDeFalla)
    {
        // "Ante la duda, no": si no se puede confirmar que el usuario sigue
        // habilitado, la sesión no sigue. Y la excepción no se escapa (si se
        // escapara, el usuario vería un error 500 en vez de volver al login).
        //
        // Pasamos el tipo y no la excepción armada porque xUnit no sabe
        // mostrar excepciones como datos de un [Theory].
        var falla = (Exception)Activator.CreateInstance(tipoDeFalla, MensajeErrorConDatosSensibles)!;
        string tipoDeErrorEsperadoEnElLog = tipoDeFalla.Name;

        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: true));
        escenario.Repositorio.ErrorAlBuscar = falla;
        var contexto = escenario.CrearContexto(new Claim(ClaimOidCorto, ObjectIdJuan));

        var excepcionEscapada = await Record.ExceptionAsync(() =>
            ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto));

        Assert.Null(excepcionEscapada);
        Assert.Null(contexto.Principal);
        Assert.Equal([CookieAuthenticationDefaults.AuthenticationScheme], escenario.Autenticacion.EsquemasConSesionCerrada);
        AssertErrorRegistradoSinDatosSensibles(escenario, tipoDeErrorEsperadoEnElLog);
    }

    [Fact]
    public async Task CasoDeUsoNoDisponibleEnElContenedor_RechazaLaSesionYRegistraElError()
    {
        // Si el caso de uso no se pudiera crear (por ejemplo, alguien borra su
        // registro en Program.cs), GetRequiredService lanza una excepción. Como
        // está dentro del try, el resultado tiene que ser el mismo: no se deja pasar.
        var escenario = new EscenarioDeValidacion(registrarCasoDeUso: false);
        var contexto = escenario.CrearContexto(new Claim(ClaimOidCorto, ObjectIdJuan));

        var excepcionEscapada = await Record.ExceptionAsync(() =>
            ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto));

        Assert.Null(excepcionEscapada);
        Assert.Null(contexto.Principal);
        Assert.Equal([CookieAuthenticationDefaults.AuthenticationScheme], escenario.Autenticacion.EsquemasConSesionCerrada);
        AssertErrorRegistradoSinDatosSensibles(escenario, nameof(InvalidOperationException));
    }

    [Fact]
    public async Task SolicitudCanceladaPorElNavegador_RechazaLaIdentidadSinRegistrarloComoError()
    {
        // El usuario cerró la pestaña mientras se consultaba la base. No es una
        // falla de la base, así que no debe aparecer como error en el log; pero
        // tampoco se puede dar por válida una sesión que no se llegó a verificar.
        var escenario = new EscenarioDeValidacion();
        escenario.Repositorio.Usuarios.Add(CrearUsuario(activo: true));
        using var cancelacionDeLaSolicitud = new CancellationTokenSource();
        cancelacionDeLaSolicitud.Cancel();
        var contexto = escenario.CrearContexto(new Claim(ClaimOidCorto, ObjectIdJuan));
        contexto.HttpContext.RequestAborted = cancelacionDeLaSolicitud.Token;

        var excepcionEscapada = await Record.ExceptionAsync(() =>
            ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contexto));

        Assert.Null(excepcionEscapada);
        Assert.Null(contexto.Principal);
        Assert.DoesNotContain(escenario.Logs.Entradas, e => e.Nivel >= LogLevel.Error);

        // No se comprueba si borra o no la cookie: como el navegador ya se fue,
        // la respuesta no le llega de todos modos.
    }

    // ---------- Ayudas ----------

    // El manejador rechazó la identidad (el pedido sigue como anónimo) y pidió
    // borrar la cookie de sesión de EOP, una sola vez y con el esquema correcto.
    private static void AssertSesionRechazadaYCookieBorrada(EscenarioDeValidacion escenario, CookieValidatePrincipalContext contexto)
    {
        Assert.Null(contexto.Principal);
        Assert.Equal([CookieAuthenticationDefaults.AuthenticationScheme], escenario.Autenticacion.EsquemasConSesionCerrada);
    }

    // Exactamente un error de la validación de sesiones, que diga qué tipo de
    // falla fue y el Id de la solicitud, sin correo, Object ID ni mensaje original.
    private static void AssertErrorRegistradoSinDatosSensibles(EscenarioDeValidacion escenario, string tipoDeError)
    {
        var entrada = Assert.Single(escenario.Logs.Entradas,
            e => e.Categoria == CategoriaDeLogs && e.Nivel == LogLevel.Error);

        Assert.Contains(tipoDeError, entrada.Mensaje);
        Assert.Contains(IdSolicitud, entrada.Mensaje);
        AssertSinDatosSensibles(entrada);
    }

    private static void AssertSinDatosSensibles(EntradaDeLog entrada)
    {
        // "@" cubre cualquier correo, no solo los de estas pruebas.
        foreach (string datoSensible in new[] { "@", ObjectIdJuan, ObjectIdSinUsuarioEnEop, "Fallo simulado" })
        {
            Assert.DoesNotContain(datoSensible, entrada.Mensaje);
            Assert.DoesNotContain(datoSensible, entrada.Valores);
        }

        // Tampoco se adjunta la excepción: su mensaje puede traer datos personales.
        Assert.Null(entrada.Excepcion);
    }

    private static Usuario CrearUsuario(bool activo, string? entraObjectId = ObjectIdJuan)
    {
        return new Usuario
        {
            Id = 1,
            NombreCompleto = "Juan Pérez",
            CorreoCorporativo = CorreoJuan,
            Rol = RolUsuario.Operador,
            Activo = activo,
            EntraObjectId = entraObjectId
        };
    }

    // Junta las piezas falsas de una prueba y arma el contexto que recibe el manejador.
    private sealed class EscenarioDeValidacion
    {
        private readonly IServiceProvider _servicios;

        public RepositorioUsuariosQueAnotaLasConsultas Repositorio { get; } = new();
        public ServicioDeAutenticacionQueAnotaLosCierresDeSesion Autenticacion { get; } = new();
        public RegistroDeLogsDePrueba Logs { get; } = new();

        public EscenarioDeValidacion(bool registrarCasoDeUso = true)
        {
            var servicios = new ServiceCollection();
            servicios.AddLogging(registro => registro.AddProvider(Logs));
            servicios.AddSingleton<IRepositorioUsuarios>(Repositorio);
            servicios.AddSingleton<IAuthenticationService>(Autenticacion);

            if (registrarCasoDeUso)
            {
                servicios.AddScoped<VerificarUsuarioHabilitado>();
            }

            _servicios = servicios.BuildServiceProvider();
        }

        // Arma lo mismo que ASP.NET Core le pasa a OnValidatePrincipal después de
        // abrir una cookie válida: el ticket con la identidad guardada en ella.
        public CookieValidatePrincipalContext CrearContexto(params Claim[] claims)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _servicios,
                TraceIdentifier = IdSolicitud
            };

            var esquema = new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));

            var identidad = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
            var ticket = new AuthenticationTicket(identidad, CookieAuthenticationDefaults.AuthenticationScheme);

            return new CookieValidatePrincipalContext(httpContext, esquema, new CookieAuthenticationOptions(), ticket);
        }
    }

    // HttpContext.SignOutAsync(esquema) termina llamando a este servicio. En vez
    // de escribir la cookie de borrado en la respuesta, anotamos qué esquema se cerró.
    private sealed class ServicioDeAutenticacionQueAnotaLosCierresDeSesion : IAuthenticationService
    {
        public List<string?> EsquemasConSesionCerrada { get; } = [];

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            EsquemasConSesionCerrada.Add(scheme);
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            throw new NotSupportedException("El manejador no debería autenticar de nuevo.");

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException("El manejador no debería pedir login.");

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException("El manejador no debería prohibir el acceso.");

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            throw new NotSupportedException("El manejador nunca debería iniciar una sesión.");
    }
}
