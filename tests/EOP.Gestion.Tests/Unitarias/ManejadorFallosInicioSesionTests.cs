using EOP.Gestion.WebApp.Autorizacion;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Tests.Unitarias;

// Simulamos que Microsoft rechazó el login y revisamos que la app lo registre
// sin datos sensibles y mande al usuario a la pantalla de error.
public class ManejadorFallosInicioSesionTests
{
    [Fact]
    public async Task ManejarAsync_RedirigeAPantallaDeErrorYEvitaLaExcepcion()
    {
        var contexto = CrearContextoDeFallo(new InvalidOperationException("fallo de prueba"));

        await ManejadorFallosInicioSesion.ManejarAsync(contexto, new LoggerDePrueba());

        Assert.Equal(StatusCodes.Status302Found, contexto.Response.StatusCode);
        Assert.Equal(ManejadorFallosInicioSesion.RutaPantallaError, contexto.Response.Headers.Location);
        Assert.True(contexto.Result?.Handled);
    }

    [Fact]
    public async Task ManejarAsync_RegistraCodigoDeEntraSinExponerDescripcionNiMensaje()
    {
        var fallo = new InvalidOperationException("mensaje con dato sensible");
        fallo.Data["error"] = "access_denied";
        fallo.Data["error_description"] = "descripcion con dato sensible";
        var contexto = CrearContextoDeFallo(fallo);
        var logger = new LoggerDePrueba();

        await ManejadorFallosInicioSesion.ManejarAsync(contexto, logger);

        Assert.Equal(LogLevel.Warning, logger.Nivel);
        Assert.Contains("access_denied", logger.Mensaje);
        Assert.Contains("solicitud-456", logger.Mensaje);
        Assert.DoesNotContain("dato sensible", logger.Mensaje);
        Assert.Null(logger.ExcepcionRegistrada);
    }

    private static RemoteFailureContext CrearContextoDeFallo(Exception fallo)
    {
        var httpContext = new DefaultHttpContext { TraceIdentifier = "solicitud-456" };
        var esquema = new AuthenticationScheme(
            OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));

        return new RemoteFailureContext(httpContext, esquema, new OpenIdConnectOptions(), fallo);
    }

    private sealed class LoggerDePrueba : ILogger
    {
        public LogLevel? Nivel { get; private set; }
        public string Mensaje { get; private set; } = string.Empty;
        public Exception? ExcepcionRegistrada { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Nivel = logLevel;
            Mensaje = formatter(state, exception);
            ExcepcionRegistrada = exception;
        }
    }
}
