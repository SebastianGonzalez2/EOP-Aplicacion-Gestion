using EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace EOP.Gestion.Tests.Integracion;

// S3-T18 — Prueba de conexión REAL con datosUruguay. No forma parte de la suite habitual: depende de
// internet y de un servicio externo, así que solo corre si se define EOP_PRUEBAS_DATOSURUGUAY=1.
// El resultado de la ejecución manual queda documentado en docs/sprints/sprint-3/s3-t18-servicio-cotizaciones.md.
public sealed class DatosUruguayRealTests
{
    private static ServicioCotizacionesDatosUruguay CrearServicio(IMemoryCache cache)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(ClienteDatosUruguay.UrlBase),
            Timeout = ClienteDatosUruguay.TiempoMaximoDeEspera
        };
        var cliente = new ClienteDatosUruguay(httpClient, NullLogger<ClienteDatosUruguay>.Instance);
        var buscador = new BuscadorCotizacionesDatosUruguay(cliente, NullLogger<BuscadorCotizacionesDatosUruguay>.Instance);

        return new ServicioCotizacionesDatosUruguay(
            buscador, cache, TimeProvider.System, NullLogger<ServicioCotizacionesDatosUruguay>.Instance);
    }

    [FactConDatosUruguayReal]
    public async Task ObtieneLaCotizacionVigenteDelBcu()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var cotizacion = await CrearServicio(cache).ObtenerCotizacionVigenteAsync();

        Assert.NotNull(cotizacion);
        Assert.True(cotizacion.Valor > 0);
        Assert.True(cotizacion.FechaPublicacion <= cotizacion.FechaSolicitada);
        Assert.True(cotizacion.FechaSolicitada.DayNumber - cotizacion.FechaPublicacion.DayNumber <= 10);
    }

    [FactConDatosUruguayReal]
    public async Task ObtieneLaCotizacionHistoricaDeUnFinDeSemanaConocido()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        // Domingo 15/11/2026: la última publicación anterior es la del viernes 13 (S3-T03).
        var cotizacion = await CrearServicio(cache).ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 15));

        Assert.Equal(new DateOnly(2026, 11, 13), cotizacion!.FechaPublicacion);
        Assert.Equal(40.611m, cotizacion.Valor);
    }
}

public sealed class FactConDatosUruguayRealAttribute : FactAttribute
{
    public FactConDatosUruguayRealAttribute()
    {
        if (Environment.GetEnvironmentVariable("EOP_PRUEBAS_DATOSURUGUAY") != "1")
        {
            Skip = "Prueba contra la API real de datosUruguay: definí EOP_PRUEBAS_DATOSURUGUAY=1 para ejecutarla.";
        }
    }
}
