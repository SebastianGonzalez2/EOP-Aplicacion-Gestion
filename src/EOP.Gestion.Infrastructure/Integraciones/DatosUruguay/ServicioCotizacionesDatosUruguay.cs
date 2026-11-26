using System.Collections.Concurrent;
using System.Globalization;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Cotizaciones;
using EOP.Gestion.Core.Interfaces.Cotizaciones;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;

/// <summary>
/// Implementa <see cref="IServicioCotizaciones"/> con datosUruguay, guardando en caché las
/// cotizaciones ya obtenidas para no consultar la API cada vez (DT-12).
/// </summary>
/// <remarks>
/// Cómo resuelve un pedido:
///   1. Averigua la fecha de hoy en Uruguay (nunca la del servidor, que en Azure está en UTC).
///   2. Si la cotización de esa fecha ya está en la caché y no venció, la devuelve sin consultar la API.
///   3. Si no, consulta la API (a través de <see cref="BuscadorCotizacionesDatosUruguay"/>) y, si
///      obtuvo una cotización válida, la guarda en la caché: 15 minutos si es la de hoy y 24 horas
///      si es de un día anterior.
///
/// La consulta es "bajo demanda": solo se llama a la API cuando alguien necesita una cotización
/// que no está en la caché. No hay ningún proceso que la actualice solo, y cuando una entrada
/// vence no pasa nada hasta que alguien vuelva a pedirla.
///
/// Se registra como Singleton: hay una sola instancia para toda la aplicación, así que todos los
/// usuarios comparten la misma caché y los mismos bloqueos.
/// </remarks>
public sealed class ServicioCotizacionesDatosUruguay : IServicioCotizaciones
{
    // Cuánto dura en caché la cotización de hoy. Es corta porque durante el día el BCU todavía
    // puede publicar el valor de hoy: si a las 10:00 respondimos "la de ayer", más tarde tiene que
    // volver a consultarse. Coincide con los 15 minutos que el propio proveedor cachea sus respuestas.
    public static readonly TimeSpan DuracionCotizacionVigente = TimeSpan.FromMinutes(15);

    // Cuánto dura en caché la cotización de un día anterior. Un día que ya pasó no debería cambiar,
    // así que puede durar más.
    public static readonly TimeSpan DuracionCotizacionHistorica = TimeSpan.FromHours(24);

    // La caché (IMemoryCache) es una sola para toda la aplicación y la pueden usar otros módulos.
    // El prefijo evita que nuestras claves choquen con las de otro.
    private const string PrefijoClaveCache = "cotizacion-usd:";

    private readonly BuscadorCotizacionesDatosUruguay _buscador;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ServicioCotizacionesDatosUruguay> _logger;

    // Un "turno" por fecha. Si dos usuarios piden a la vez la cotización del mismo día y no está en
    // la caché, sin esto los dos consultarían la API. Con el turno, el primero consulta y el segundo
    // espera; cuando entra, la encuentra en la caché. Pedidos de fechas distintas no se esperan entre sí.
    //
    // ConcurrentDictionary porque varios pedidos pueden agregar fechas al mismo tiempo.
    private readonly ConcurrentDictionary<DateOnly, SemaphoreSlim> _turnosPorFecha = new();

    public ServicioCotizacionesDatosUruguay(
        BuscadorCotizacionesDatosUruguay buscador,
        IMemoryCache cache,
        TimeProvider reloj,
        ILogger<ServicioCotizacionesDatosUruguay> logger)
    {
        _buscador = buscador;
        _cache = cache;
        _reloj = reloj;
        _logger = logger;
    }

    public Task<CotizacionUsd?> ObtenerCotizacionVigenteAsync(CancellationToken cancellationToken = default)
    {
        DateOnly hoy = ObtenerFechaDeHoyEnUruguay();

        return ObtenerCotizacionAsync(hoy, hoy, cancellationToken);
    }

    public Task<CotizacionUsd?> ObtenerCotizacionHistoricaAsync(DateOnly fecha, CancellationToken cancellationToken = default)
    {
        DateOnly hoy = ObtenerFechaDeHoyEnUruguay();

        // Una fecha futura no tiene cotización todavía. Si la aceptáramos, terminaríamos devolviendo
        // la de hoy como si fuera "la de mañana" y la dejaríamos 24 horas en la caché.
        if (fecha > hoy)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fecha),
                fecha,
                "No se puede pedir la cotización de una fecha posterior a hoy.");
        }

        return ObtenerCotizacionAsync(fecha, hoy, cancellationToken);
    }

    private async Task<CotizacionUsd?> ObtenerCotizacionAsync(
        DateOnly fecha,
        DateOnly hoy,
        CancellationToken cancellationToken)
    {
        // La clave es la fecha SOLICITADA. La fecha efectiva de publicación viaja dentro del valor
        // guardado (CotizacionUsd.FechaPublicacion), así que no se pierde.
        string clave = PrefijoClaveCache + fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // 1. Primero miramos la caché. Es lo más común y no hace falta esperar a nadie.
        CotizacionUsd? enCache = BuscarEnCache(clave);
        if (enCache != null)
        {
            return enCache;
        }

        // 2. No estaba: pedimos el turno de esa fecha. WaitAsync recibe el token para que, si el
        //    usuario cancela mientras espera, deje de esperar.
        SemaphoreSlim turno = _turnosPorFecha.GetOrAdd(fecha, CrearTurno);
        await turno.WaitAsync(cancellationToken);

        try
        {
            // 3. Volvemos a mirar la caché: mientras esperábamos, quien tenía el turno pudo haberla
            //    llenado. Este segundo control es el que evita la consulta repetida.
            enCache = BuscarEnCache(clave);
            if (enCache != null)
            {
                return enCache;
            }

            // 4. Consultamos la API. Si falla, la excepción sale de acá sin tocar la caché: un error
            //    nunca se guarda como si fuera una cotización.
            CotizacionUsd? cotizacion = await _buscador.BuscarUltimaCotizacionValidaAsync(fecha, cancellationToken);

            // 5. Solo guardamos una cotización válida. Si no hay publicación (null) no guardamos nada,
            //    así la próxima consulta vuelve a preguntarle al proveedor.
            if (cotizacion != null)
            {
                GuardarEnCache(clave, cotizacion, fecha, hoy);
            }

            return cotizacion;
        }
        finally
        {
            // El turno se libera siempre, aunque haya fallado la consulta; si no, la fecha quedaría
            // bloqueada para siempre.
            turno.Release();
        }
    }

    private CotizacionUsd? BuscarEnCache(string clave)
    {
        // TryGetValue no devuelve entradas vencidas: una vez que pasa su tiempo, la caché las
        // considera inexistentes. Por eso nunca se usa una cotización vencida para tapar una falla.
        bool encontrada = _cache.TryGetValue(clave, out CotizacionUsd? cotizacion);

        if (encontrada && cotizacion != null)
        {
            return cotizacion;
        }

        return null;
    }

    private void GuardarEnCache(string clave, CotizacionUsd cotizacion, DateOnly fecha, DateOnly hoy)
    {
        TimeSpan duracion;

        if (fecha == hoy)
        {
            duracion = DuracionCotizacionVigente;
        }
        else
        {
            duracion = DuracionCotizacionHistorica;
        }

        // Expiración ABSOLUTA: la entrada vence a los 15 minutos (o 24 horas) de guardada, aunque se
        // siga usando. Con una expiración "deslizante", una cotización muy consultada nunca vencería.
        var opciones = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duracion
        };

        _cache.Set(clave, cotizacion, opciones);

        _logger.LogInformation(
            "Cotización para el {FechaSolicitada} guardada en caché por {Duracion}: {Valor} UYU/USD publicada el {FechaPublicacion}.",
            cotizacion.FechaSolicitada, duracion, cotizacion.Valor, cotizacion.FechaPublicacion);
    }

    private DateOnly ObtenerFechaDeHoyEnUruguay()
    {
        // Tomamos la hora UTC del reloj (que en las pruebas se puede fijar) y la pasamos a la hora
        // de Uruguay con ZonaHorariaUruguay, como pide AGENTS.md para cualquier fecha del sistema.
        DateTime ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        DateTime ahoraEnUruguay = ZonaHorariaUruguay.ConvertirUtcALocal(ahoraUtc);

        return DateOnly.FromDateTime(ahoraEnUruguay);
    }

    private static SemaphoreSlim CrearTurno(DateOnly fecha)
    {
        // (1, 1): un solo pedido a la vez puede tener el turno de esa fecha.
        return new SemaphoreSlim(1, 1);
    }
}
