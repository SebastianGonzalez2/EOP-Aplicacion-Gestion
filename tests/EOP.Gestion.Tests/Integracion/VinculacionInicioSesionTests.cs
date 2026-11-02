using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Autorizacion;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EOP.Gestion.Tests.Integracion;

// Probamos el código que Program.cs engancha en OnTicketReceived: el momento
// en que Microsoft ya autenticó a la persona y la app decide si es un usuario
// de EOP habilitado (US-042 / RF-69).
//
// No hay Microsoft real ni base de datos real:
// - levantamos la app en memoria con WebApplicationFactory y le pedimos el
//   evento OnTicketReceived tal como lo dejó configurado Program.cs;
// - el repositorio de usuarios se reemplaza por una lista en memoria.
//
// Hay dos grupos:
// 1. Llamamos al evento directamente con una identidad armada a mano y
//    miramos si dejó seguir el login o si lo cortó (respuesta gestionada
//    con redirección a la pantalla de error).
// 2. Simulamos el ida y vuelta completo del login con un esquema remoto
//    "de mentira" que usa el MISMO evento, para ver si el usuario termina
//    con la cookie de sesión o no.
// 3. Hacemos que el repositorio lance excepciones y comprobamos que el
//    evento las controla, corta el login y lo registra en el log.
// 4. Revisamos los logs de los accesos rechazados.
// 5. Revisamos las redirecciones y la pantalla de error.
public class VinculacionInicioSesionTests
{
    private const string ObjectIdJuan = "11111111-1111-1111-1111-111111111111";
    private const string ObjectIdDesconocido = "99999999-9999-9999-9999-999999999999";

    // ---------- 1. El evento por sí solo ----------

    [Fact]
    public async Task OnTicketReceived_UsuarioVinculadoYActivo_NoMarcaFallo()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        using var fabrica = new FabricaDePrueba(repositorio);

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdJuan));

        // Result null significa "seguí normalmente": se crea la sesión.
        Assert.Null(contexto.Result);
        AssertNoRedirige(contexto);
    }

    [Fact]
    public async Task OnTicketReceived_PrimerAcceso_VinculaUsandoElCorreoDePreferredUsername()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        using var fabrica = new FabricaDePrueba(repositorio);

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdJuan),
            new Claim("preferred_username", "juan.perez@eop.com.uy"));

        Assert.Null(contexto.Result);
        AssertNoRedirige(contexto);
        Assert.Equal(ObjectIdJuan, repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task OnTicketReceived_SinPreferredUsername_UsaElClaimDeCorreo()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        using var fabrica = new FabricaDePrueba(repositorio);

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdJuan),
            new Claim(ClaimTypes.Email, "juan.perez@eop.com.uy"));

        Assert.Null(contexto.Result);
        AssertNoRedirige(contexto);
        Assert.Equal(ObjectIdJuan, repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task OnTicketReceived_IdentidadNoRegistradaEnEop_RechazaYRedirigeAPantallaDeError()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        using var fabrica = new FabricaDePrueba(repositorio);

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdDesconocido),
            new Claim("preferred_username", "desconocido@eop.com.uy"));

        AssertRechazadoConRedireccionAPantallaDeError(contexto);
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task OnTicketReceived_UsuarioInactivo_RechazaYRedirigeAPantallaDeError()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario(activo: false, entraObjectId: ObjectIdJuan));
        using var fabrica = new FabricaDePrueba(repositorio);

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdJuan));

        AssertRechazadoConRedireccionAPantallaDeError(contexto);
    }

    [Fact]
    public async Task OnTicketReceived_SinClaimOid_RechazaYRedirigeSinVincular()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        using var fabrica = new FabricaDePrueba(repositorio);

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("preferred_username", "juan.perez@eop.com.uy"));

        AssertRechazadoConRedireccionAPantallaDeError(contexto);
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task OnTicketReceived_SinIdentidad_RechazaYRedirigeAPantallaDeError()
    {
        using var fabrica = new FabricaDePrueba(new RepositorioUsuariosEnMemoria());

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica, sinIdentidad: true);

        AssertRechazadoConRedireccionAPantallaDeError(contexto);
    }

    // ---------- 2. El login completo, simulado ----------

    [Fact]
    public async Task LoginSimulado_UsuarioHabilitado_ObtieneCookieDeSesion()
    {
        // Esta prueba confirma que la simulación funciona: si el usuario
        // está habilitado, la app le da la cookie y lo manda al inicio.
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        using var fabrica = new FabricaDePrueba(repositorio);

        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/", respuesta.Headers.Location?.OriginalString);
        Assert.True(TieneCookieDeSesion(respuesta));
    }

    [Fact]
    public async Task LoginSimulado_PrimerAcceso_VinculaAlUsuarioYObtieneCookieDeSesion()
    {
        // Además de probar el primer acceso, esta prueba confirma que la
        // simulación ejecuta de verdad el evento de Program.cs: si no lo
        // ejecutara, nadie vincularía al usuario.
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        using var fabrica = new FabricaDePrueba(repositorio);

        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.Equal(ObjectIdJuan, repositorio.Usuarios.Single().EntraObjectId);
        Assert.True(TieneCookieDeSesion(respuesta));
    }

    [Fact]
    public async Task LoginSimulado_IdentidadNoRegistradaEnEop_NoObtieneCookieDeSesion()
    {
        // Escenario "Identidad no registrada en EOP" de la US-042: Microsoft
        // autentica a alguien que no existe en EOP. No debe quedar con sesión.
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        using var fabrica = new FabricaDePrueba(repositorio);

        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdDesconocido, "desconocido@eop.com.uy");

        Assert.False(TieneCookieDeSesion(respuesta),
            "La app emitió la cookie de sesión a una identidad que no es usuario habilitado de EOP.");
        AssertRedirigeAPantallaDeError(respuesta);
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task LoginSimulado_UsuarioInactivo_NoObtieneCookieDeSesion()
    {
        // Escenario "Usuario inactivo" de la US-042.
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario(activo: false, entraObjectId: ObjectIdJuan));
        using var fabrica = new FabricaDePrueba(repositorio);

        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.False(TieneCookieDeSesion(respuesta),
            "La app emitió la cookie de sesión a un usuario Inactivo.");
        AssertRedirigeAPantallaDeError(respuesta);
    }

    // ---------- 3. Errores durante la identificación ----------

    // La excepción simulada trae datos personales en su mensaje a propósito:
    // así comprobamos que no terminan en el log.
    private const string MensajeErrorConDatosSensibles =
        "Fallo simulado para juan.perez@eop.com.uy con " + ObjectIdJuan;

    [Fact]
    public async Task OnTicketReceived_ErrorAlBuscarUsuario_ControlaLaExcepcionYRedirigeAPantallaDeError()
    {
        // La base "se cae" en la primera búsqueda (por EntraObjectId).
        var repositorio = new RepositorioUsuariosEnMemoria
        {
            ErrorAlBuscar = new InvalidOperationException(MensajeErrorConDatosSensibles)
        };
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        TicketReceivedContext? contexto = null;

        // Record.ExceptionAsync devuelve la excepción si el evento la deja
        // escapar, o null si la atrapó.
        var excepcion = await Record.ExceptionAsync(async () =>
            contexto = await EjecutarOnTicketReceivedAsync(fabrica,
                new Claim("oid", ObjectIdJuan),
                new Claim("preferred_username", "juan.perez@eop.com.uy")));

        Assert.Null(excepcion);
        AssertRechazadoConRedireccionAPantallaDeError(contexto!);
        AssertErrorRegistradoSinDatosSensibles(logs, nameof(InvalidOperationException));
    }

    [Fact]
    public async Task OnTicketReceived_ErrorAlVincularEnPrimerAcceso_ControlaLaExcepcionYNoVincula()
    {
        // La búsqueda funciona, pero la base falla justo al guardar la vinculación.
        var repositorio = new RepositorioUsuariosEnMemoria
        {
            ErrorAlVincular = new TimeoutException(MensajeErrorConDatosSensibles)
        };
        repositorio.Usuarios.Add(CrearUsuario());
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        TicketReceivedContext? contexto = null;

        var excepcion = await Record.ExceptionAsync(async () =>
            contexto = await EjecutarOnTicketReceivedAsync(fabrica,
                new Claim("oid", ObjectIdJuan),
                new Claim("preferred_username", "juan.perez@eop.com.uy")));

        Assert.Null(excepcion);
        AssertRechazadoConRedireccionAPantallaDeError(contexto!);
        AssertErrorRegistradoSinDatosSensibles(logs, nameof(TimeoutException));
        Assert.Null(repositorio.Usuarios.Single().EntraObjectId);
    }

    [Fact]
    public async Task LoginSimulado_ErrorDelRepositorio_NoObtieneCookieYRedirigeAPantallaDeError()
    {
        // El mismo escenario, pero recorriendo el login completo: el usuario
        // está habilitado, pero como la base falla no se lo puede confirmar.
        var repositorio = new RepositorioUsuariosEnMemoria
        {
            ErrorAlBuscar = new InvalidOperationException(MensajeErrorConDatosSensibles)
        };
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdJuan, "juan.perez@eop.com.uy");

        Assert.False(TieneCookieDeSesion(respuesta),
            "La app emitió la cookie de sesión aunque no pudo identificar al usuario.");

        // Pide un 302 a la pantalla de error: si la excepción se hubiera
        // escapado, la respuesta sería un 500 y esta comprobación fallaría.
        AssertRedirigeAPantallaDeError(respuesta);
        AssertErrorRegistradoSinDatosSensibles(logs, nameof(InvalidOperationException));
    }

    [Fact]
    public async Task OnTicketReceived_SolicitudCanceladaPorElUsuario_NoSeRegistraComoErrorYNoCreaSesion()
    {
        // El usuario cerró la pestaña a mitad del login: ASP.NET Core marca
        // la solicitud como cancelada (RequestAborted) y la consulta se corta.
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica, false,
            solicitud => solicitud.RequestAborted = cancelacion.Token,
            new Claim("oid", ObjectIdJuan));

        // No se crea la sesión.
        AssertRechazadoConRedireccionAPantallaDeError(contexto);

        // Queda como información, no como error de base de datos.
        Assert.DoesNotContain(logs.Entradas, e => e.Nivel >= LogLevel.Error);
        var entrada = Assert.Single(logs.Entradas,
            e => e.Categoria == "VinculacionUsuarios" && e.Nivel == LogLevel.Information);
        Assert.Contains("cancelada", entrada.Mensaje);
        AssertSinDatosSensibles(entrada);
    }

    [Fact]
    public async Task OnTicketReceived_CancelacionQueNoVieneDelUsuario_SeRegistraComoError()
    {
        // Una operación cancelada por otro motivo (por ejemplo, un tiempo de
        // espera interno) SÍ es un problema y tiene que quedar como error.
        var repositorio = new RepositorioUsuariosEnMemoria
        {
            ErrorAlBuscar = new TaskCanceledException(MensajeErrorConDatosSensibles)
        };
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdJuan));

        AssertRechazadoConRedireccionAPantallaDeError(contexto);
        AssertErrorRegistradoSinDatosSensibles(logs, nameof(TaskCanceledException));
    }

    // ---------- 4. Logs de accesos rechazados ----------

    [Fact]
    public async Task OnTicketReceived_SinIdentidad_RegistraAdvertenciaSinDatosSensibles()
    {
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(new RepositorioUsuariosEnMemoria(), logs);

        await EjecutarOnTicketReceivedAsync(fabrica, sinIdentidad: true);

        AssertAdvertenciaRegistradaSinDatosSensibles(logs, "no envió una identidad válida");
    }

    [Fact]
    public async Task OnTicketReceived_SinClaimOid_RegistraAdvertenciaDeIdentidadInvalidaSinElCorreo()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("preferred_username", "juan.perez@eop.com.uy"));

        AssertAdvertenciaRegistradaSinDatosSensibles(logs, "no envió una identidad válida");
    }

    [Fact]
    public async Task OnTicketReceived_IdentidadNoRegistrada_RegistraAdvertenciaSinDatosSensibles()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdDesconocido),
            new Claim("preferred_username", "desconocido@eop.com.uy"));

        AssertAdvertenciaRegistradaSinDatosSensibles(logs, "no se encontró un usuario habilitado de EOP");
    }

    [Fact]
    public async Task OnTicketReceived_UsuarioInactivo_RegistraLaMismaAdvertenciaQueUnoInexistente()
    {
        // Mismo texto a propósito: el log no distingue "inactivo" de "no existe".
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario(activo: false, entraObjectId: ObjectIdJuan));
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdJuan),
            new Claim("preferred_username", "juan.perez@eop.com.uy"));

        AssertAdvertenciaRegistradaSinDatosSensibles(logs, "no se encontró un usuario habilitado de EOP");
    }

    [Fact]
    public async Task OnTicketReceived_AccesoAceptado_NoRegistraAdvertenciasNiErrores()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario(entraObjectId: ObjectIdJuan));
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        await EjecutarOnTicketReceivedAsync(fabrica,
            new Claim("oid", ObjectIdJuan));

        Assert.DoesNotContain(logs.Entradas,
            e => e.Categoria == "VinculacionUsuarios" && e.Nivel >= LogLevel.Warning);
    }

    [Fact]
    public async Task LoginSimulado_AccesosRechazados_RegistranAdvertenciaSinDatosSensibles()
    {
        // Lo mismo, pero recorriendo el login completo.
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());
        var logs = new RegistroDeLogsDePrueba();
        using var fabrica = new FabricaDePrueba(repositorio, logs);

        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdDesconocido, "desconocido@eop.com.uy");

        Assert.False(TieneCookieDeSesion(respuesta));
        AssertAdvertenciaRegistradaSinDatosSensibles(logs, "no se encontró un usuario habilitado de EOP");
    }

    // ---------- 5. Redirecciones y pantalla de error ----------

    [Theory]
    [InlineData("sin identidad")]
    [InlineData("usuario inexistente")]
    [InlineData("error de base de datos")]
    public async Task OnTicketReceived_AppPublicadaEnUnaSubcarpeta_RedirigeRespetandoElPathBase(string motivo)
    {
        // Si la app se publica en, por ejemplo, midominio.com/eop, la
        // redirección tiene que incluir ese "/eop" adelante.
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario());

        if (motivo == "error de base de datos")
        {
            repositorio.ErrorAlBuscar = new InvalidOperationException(MensajeErrorConDatosSensibles);
        }

        using var fabrica = new FabricaDePrueba(repositorio);

        var contexto = await EjecutarOnTicketReceivedAsync(fabrica,
            sinIdentidad: motivo == "sin identidad",
            prepararSolicitud: solicitud => solicitud.Request.PathBase = "/eop",
            claims: [new Claim("oid", ObjectIdDesconocido), new Claim("preferred_username", "desconocido@eop.com.uy")]);

        Assert.True(contexto.Result?.Handled);
        Assert.Equal(StatusCodes.Status302Found, contexto.Response.StatusCode);
        Assert.Equal("/eop" + ManejadorFallosInicioSesion.RutaPantallaError, contexto.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task LoginSimulado_AccesoRechazado_LlegaAUnaPantallaDeErrorGenericaQuePermiteReintentar()
    {
        var repositorio = new RepositorioUsuariosEnMemoria();
        repositorio.Usuarios.Add(CrearUsuario(activo: false, entraObjectId: ObjectIdJuan));
        using var fabrica = new FabricaDePrueba(repositorio);

        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdJuan, "juan.perez@eop.com.uy");
        AssertRedirigeAPantallaDeError(respuesta);

        // Seguimos la redirección, como haría el navegador.
        var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var pantalla = await cliente.GetAsync(respuesta.Headers.Location);
        string html = WebUtility.HtmlDecode(await pantalla.Content.ReadAsStringAsync());

        // En la vista las frases están cortadas en varias líneas: juntamos
        // saltos de línea y espacios repetidos en un solo espacio.
        html = Regex.Replace(html, @"\s+", " ");

        Assert.Equal(HttpStatusCode.OK, pantalla.StatusCode);

        // Mensaje válido para cualquier motivo, con el botón para reintentar.
        Assert.Contains("No pudimos completar tu inicio de sesión", html);
        Assert.Contains("comunicate con el administrador de EOP", html);
        Assert.Contains("href=\"/Cuenta/IniciarSesion\"", html);

        // No le cuenta a quien intenta entrar en qué estado está la cuenta.
        Assert.DoesNotContain("inactiv", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no está registrad", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no existe", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("juan.perez@eop.com.uy", html);
    }

    // ---------- Ayudas ----------

    // Debe haber exactamente un error registrado por la vinculación, que diga
    // qué tipo de falla fue, pero sin correo, identificador ni mensaje original.
    private static void AssertErrorRegistradoSinDatosSensibles(RegistroDeLogsDePrueba logs, string tipoDeError)
    {
        var entrada = Assert.Single(logs.Entradas,
            e => e.Categoria == "VinculacionUsuarios" && e.Nivel == LogLevel.Error);

        Assert.Contains(tipoDeError, entrada.Mensaje);
        AssertSinDatosSensibles(entrada);
    }

    // Debe haber exactamente una advertencia con el motivo esperado, sin datos
    // sensibles, y ningún error (un acceso rechazado no es una falla del sistema).
    private static void AssertAdvertenciaRegistradaSinDatosSensibles(RegistroDeLogsDePrueba logs, string motivoEsperado)
    {
        var entrada = Assert.Single(logs.Entradas,
            e => e.Categoria == "VinculacionUsuarios" && e.Nivel == LogLevel.Warning);

        Assert.Contains(motivoEsperado, entrada.Mensaje);
        AssertSinDatosSensibles(entrada);
        Assert.DoesNotContain(logs.Entradas, e => e.Nivel >= LogLevel.Error);
    }

    private static void AssertSinDatosSensibles(EntradaDeLog entrada)
    {
        // "@" cubre cualquier correo, no solo los de estas pruebas.
        foreach (string datoSensible in new[] { "@", ObjectIdJuan, ObjectIdDesconocido, "Fallo simulado" })
        {
            Assert.DoesNotContain(datoSensible, entrada.Mensaje);
            Assert.DoesNotContain(datoSensible, entrada.Valores);
        }

        // Tampoco se adjunta la excepción: su mensaje puede traer datos
        // personales (SQL Server, por ejemplo, incluye el valor repetido
        // cuando se viola un índice único).
        Assert.Null(entrada.Excepcion);
    }

    // El evento rechazó el login: dio la respuesta por gestionada (así ASP.NET
    // Core no crea la sesión) y mandó al usuario a la pantalla de error.
    private static void AssertRechazadoConRedireccionAPantallaDeError(TicketReceivedContext contexto)
    {
        Assert.NotNull(contexto.Result);
        Assert.True(contexto.Result.Handled);
        Assert.Equal(StatusCodes.Status302Found, contexto.Response.StatusCode);
        Assert.Equal(ManejadorFallosInicioSesion.RutaPantallaError, contexto.Response.Headers.Location.ToString());
    }

    // El evento dejó seguir el login: no tocó la respuesta.
    private static void AssertNoRedirige(TicketReceivedContext contexto)
    {
        Assert.Equal(StatusCodes.Status200OK, contexto.Response.StatusCode);
        Assert.Equal(string.Empty, contexto.Response.Headers.Location.ToString());
    }

    // Lo mismo, pero mirando la respuesta HTTP real del login simulado.
    private static void AssertRedirigeAPantallaDeError(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(ManejadorFallosInicioSesion.RutaPantallaError, respuesta.Headers.Location?.OriginalString);
    }

    private static Usuario CrearUsuario(bool activo = true, string? entraObjectId = null)
    {
        return new Usuario
        {
            Id = 1,
            NombreCompleto = "Juan Pérez",
            CorreoCorporativo = "juan.perez@eop.com.uy",
            Rol = RolUsuario.Operador,
            Activo = activo,
            EntraObjectId = entraObjectId
        };
    }

    // Arma el mismo objeto que ASP.NET Core le pasa al evento después del
    // login y ejecuta el evento configurado en Program.cs.
    private static async Task<TicketReceivedContext> EjecutarOnTicketReceivedAsync(
        FabricaDePrueba fabrica, params Claim[] claims)
    {
        return await EjecutarOnTicketReceivedAsync(fabrica, false, null, claims);
    }

    // prepararSolicitud permite cambiar la solicitud antes de ejecutar el
    // evento, por ejemplo para simular un PathBase o una solicitud cancelada.
    private static async Task<TicketReceivedContext> EjecutarOnTicketReceivedAsync(
        FabricaDePrueba fabrica, bool sinIdentidad, Action<HttpContext>? prepararSolicitud = null, params Claim[] claims)
    {
        var opciones = fabrica.Services
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        using var alcance = fabrica.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = alcance.ServiceProvider };
        prepararSolicitud?.Invoke(httpContext);
        var esquema = new AuthenticationScheme(
            OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));

        var identidad = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
        var ticket = new AuthenticationTicket(identidad, OpenIdConnectDefaults.AuthenticationScheme);

        var contexto = new TicketReceivedContext(httpContext, esquema, opciones, ticket);

        if (sinIdentidad)
        {
            contexto.Principal = null;
        }

        await opciones.Events.OnTicketReceived(contexto);

        return contexto;
    }

    private static async Task<HttpResponseMessage> HacerLoginSimuladoAsync(
        FabricaDePrueba fabrica, string entraObjectId, string correo)
    {
        // AllowAutoRedirect = false y HandleCookies = false: queremos ver la
        // respuesta tal cual, con sus encabezados Set-Cookie.
        var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

        var pedido = new HttpRequestMessage(HttpMethod.Get, ManejadorEntraSimulado.RutaRetorno);
        pedido.Headers.Add(ManejadorEntraSimulado.EncabezadoOid, entraObjectId);
        pedido.Headers.Add(ManejadorEntraSimulado.EncabezadoCorreo, correo);

        // Si el pedido termina en excepción no hay respuesta ni cookie:
        // devolvemos un 500 para que las pruebas lo traten como "sin sesión".
        try
        {
            return await cliente.SendAsync(pedido);
        }
        catch (Exception)
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }

    private static bool TieneCookieDeSesion(HttpResponseMessage respuesta)
    {
        // La cookie de sesión se llama ".AspNetCore.Cookies" (o ".AspNetCore.CookiesC1",
        // ".AspNetCore.CookiesC2"... cuando es grande y se parte en pedazos).
        if (!respuesta.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return false;
        }

        string nombre = CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme;
        return cookies.Any(c => c.StartsWith(nombre, StringComparison.Ordinal)
            && !c.StartsWith(nombre + "=;", StringComparison.Ordinal));
    }

    // Arma la app como la real, salvo:
    // - una cadena de conexión ficticia (no se usa: el repositorio es en memoria);
    // - el repositorio de usuarios, que es la lista en memoria de la prueba;
    // - un esquema remoto simulado que reutiliza el OnTicketReceived de Program.cs.
    // - si se le pasa un registro de logs, la app también escribe sus logs ahí.
    private sealed class FabricaDePrueba(
        RepositorioUsuariosEnMemoria repositorio,
        RegistroDeLogsDePrueba? registroDeLogs = null)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:EopDb",
                "Server=(localdb)\\pruebas;Database=NoSeUsa;Trusted_Connection=True");

            builder.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton<IRepositorioUsuarios>(repositorio);

                if (registroDeLogs != null)
                {
                    servicios.AddSingleton<ILoggerProvider>(registroDeLogs);
                }

                servicios.AddAuthentication()
                    .AddRemoteScheme<RemoteAuthenticationOptions, ManejadorEntraSimulado>(
                        ManejadorEntraSimulado.Nombre,
                        ManejadorEntraSimulado.Nombre,
                        opciones =>
                        {
                            opciones.CallbackPath = ManejadorEntraSimulado.RutaRetorno;
                            opciones.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        });

                // Copiamos el evento tal como quedó configurado para OpenID Connect.
                servicios.AddOptions<RemoteAuthenticationOptions>(ManejadorEntraSimulado.Nombre)
                    .Configure<IOptionsMonitor<OpenIdConnectOptions>>((opciones, opcionesOidc) =>
                    {
                        var eventosOidc = opcionesOidc.Get(OpenIdConnectDefaults.AuthenticationScheme).Events;
                        opciones.Events = new RemoteAuthenticationEvents
                        {
                            OnTicketReceived = eventosOidc.OnTicketReceived
                        };
                    });
            });
        }
    }

    // Hace el papel de Microsoft volviendo a la app: cuando llega un pedido a
    // la ruta de retorno, "autentica" a quien indiquen los encabezados.
    // A partir de ahí, ASP.NET Core sigue su camino normal: ejecuta
    // OnTicketReceived y, según el resultado, crea o no la cookie de sesión.
    private sealed class ManejadorEntraSimulado(
        IOptionsMonitor<RemoteAuthenticationOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador)
        : RemoteAuthenticationHandler<RemoteAuthenticationOptions>(opciones, logger, codificador)
    {
        public const string Nombre = "EntraSimulado";
        public const string RutaRetorno = "/signin-entra-simulado";
        public const string EncabezadoOid = "X-Oid-Simulado";
        public const string EncabezadoCorreo = "X-Correo-Simulado";

        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new("oid", Request.Headers[EncabezadoOid].ToString()),
                new("preferred_username", Request.Headers[EncabezadoCorreo].ToString())
            };

            var identidad = new ClaimsPrincipal(new ClaimsIdentity(claims, Nombre));
            var ticket = new AuthenticationTicket(identidad, new AuthenticationProperties(), Nombre);

            return Task.FromResult(HandleRequestResult.Success(ticket));
        }
    }

    // Repositorio en memoria con las mismas condiciones que el real.
    private sealed class RepositorioUsuariosEnMemoria : IRepositorioUsuarios
    {
        public List<Usuario> Usuarios { get; } = [];

        // Si no es null, las búsquedas lanzan esta excepción (por ejemplo, la base no responde).
        public Exception? ErrorAlBuscar { get; set; }

        // Si no es null, la vinculación lanza esta excepción.
        public Exception? ErrorAlVincular { get; set; }

        public Task<Usuario?> ObtenerPorIdentidadExternaAsync(string entraObjectId, CancellationToken cancellationToken = default)
        {
            // Igual que EF Core: si la solicitud ya se canceló, no seguimos.
            cancellationToken.ThrowIfCancellationRequested();

            if (ErrorAlBuscar != null)
            {
                throw ErrorAlBuscar;
            }

            return Task.FromResult(Usuarios.SingleOrDefault(u => u.EntraObjectId == entraObjectId));
        }

        public Task<Usuario?> ObtenerPorCorreoCorporativoAsync(string correoCorporativo, CancellationToken cancellationToken = default)
        {
            // Igual que EF Core: si la solicitud ya se canceló, no seguimos.
            cancellationToken.ThrowIfCancellationRequested();

            if (ErrorAlBuscar != null)
            {
                throw ErrorAlBuscar;
            }

            return Task.FromResult(Usuarios.SingleOrDefault(u => u.CorreoCorporativo == correoCorporativo));
        }

        public Task<bool> VincularIdentidadSiNoVinculadaAsync(int usuarioId, string entraObjectId, CancellationToken cancellationToken = default)
        {
            // Igual que EF Core: si la solicitud ya se canceló, no seguimos.
            cancellationToken.ThrowIfCancellationRequested();

            if (ErrorAlVincular != null)
            {
                throw ErrorAlVincular;
            }

            var usuario = Usuarios.SingleOrDefault(u => u.Id == usuarioId && u.EntraObjectId == null && u.Activo);

            if (usuario == null)
            {
                return Task.FromResult(false);
            }

            usuario.EntraObjectId = entraObjectId;
            return Task.FromResult(true);
        }

        // El cambio de rol (T14) no se usa en las pruebas de este repositorio.
        public Task<IBloqueoAdministradores> BloquearAdministradoresAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task ActualizarRolAsync(int usuarioId, RolUsuario nuevoRol, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    // El registro de logs en memoria (RegistroDeLogsDePrueba y EntradaDeLog)
    // estaba acá. Se movió, sin cambios, a Compartido/RegistroDeLogsDePrueba.cs
    // para que también lo usen las pruebas de la T11.
}
