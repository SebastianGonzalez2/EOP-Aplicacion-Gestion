using EOP.Gestion.WebApp.Controllers;
using EOP.Gestion.WebApp.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Tests.Unitarias;

public class ManejoErroresTests
{
    [Fact]
    public void Error_RegistraTipoYReferenciaSinExponerMensajeDeExcepcion()
    {
        var logger = new LoggerDePrueba();
        var contexto = new DefaultHttpContext { TraceIdentifier = "solicitud-123" };
        contexto.Features.Set<IExceptionHandlerPathFeature>(new ErrorDePrueba
        {
            Error = new InvalidOperationException("dato sensible de prueba"),
            Path = "/Home/Index"
        });
        var controller = new HomeController(logger)
        {
            ControllerContext = new ControllerContext { HttpContext = contexto }
        };

        var resultado = Assert.IsType<ViewResult>(controller.Error());
        var modelo = Assert.IsType<ErrorViewModel>(resultado.Model);

        Assert.Equal("solicitud-123", modelo.RequestId);
        Assert.Equal(LogLevel.Error, logger.Nivel);
        Assert.Contains("InvalidOperationException", logger.Mensaje);
        Assert.Contains(modelo.RequestId!, logger.Mensaje);
        Assert.DoesNotContain("dato sensible de prueba", logger.Mensaje);
        Assert.Null(logger.ExcepcionRegistrada);
    }

    private sealed class LoggerDePrueba : ILogger<HomeController>
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

    private sealed class ErrorDePrueba : IExceptionHandlerPathFeature
    {
        public Exception Error { get; set; } = null!;
        public string Path { get; set; } = string.Empty;
        public Endpoint? Endpoint { get; set; }
        public RouteValueDictionary? RouteValues { get; set; }
    }
}
