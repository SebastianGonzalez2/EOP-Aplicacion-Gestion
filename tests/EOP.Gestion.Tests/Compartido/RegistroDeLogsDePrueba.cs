using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Tests.Compartido;

// Lo usaban solo las pruebas de la T10 (VinculacionInicioSesionTests), como clase
// privada. Lo movimos acá, sin cambiarlo, para que las pruebas de la T11 puedan
// revisar los logs de la misma forma en vez de copiar el código.

// Un "destino" de logs que guarda todo en memoria para revisarlo en la prueba.
// Se suma a los que ya tiene la app; no reemplaza ninguno.
internal sealed class RegistroDeLogsDePrueba : ILoggerProvider
{
    private readonly ConcurrentQueue<EntradaDeLog> _entradas = new();

    public IReadOnlyCollection<EntradaDeLog> Entradas => _entradas.ToArray();

    public ILogger CreateLogger(string categoryName) => new LoggerEnMemoria(categoryName, _entradas);

    public void Dispose()
    {
    }

    private sealed class LoggerEnMemoria(string categoria, ConcurrentQueue<EntradaDeLog> entradas) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Además del texto final, guardamos los valores sueltos del mensaje
            // ({TipoError}, etc.), que es lo que verían herramientas como Application Insights.
            string valores = state is IEnumerable<KeyValuePair<string, object?>> pares
                ? string.Join(" | ", pares.Select(p => $"{p.Key}={p.Value}"))
                : string.Empty;

            entradas.Enqueue(new EntradaDeLog(categoria, logLevel, formatter(state, exception), valores, exception));
        }
    }
}

internal sealed record EntradaDeLog(
    string Categoria,
    LogLevel Nivel,
    string Mensaje,
    string Valores,
    Exception? Excepcion);
