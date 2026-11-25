using System.Net;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Cotizaciones;
using EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T18 — Selección de la cotización para una fecha (RF-29, RF-077, RF-103, RF-104): la del día o
// la última válida anterior, nunca una posterior; búsqueda por rango y páginas; y diferencia entre
// "no hay publicación" (null) y "no se pudo consultar" (excepción).
public sealed class BuscadorCotizacionesDatosUruguayTests : IDisposable
{
    // Noviembre de 2026: el 20 es viernes, el 21 y 22 son sábado y domingo.
    private readonly ProveedorDatosUruguaySimulado _proveedor = new ProveedorDatosUruguaySimulado()
        .ConPublicacion("2026-11-17", 40.578m)
        .ConPublicacion("2026-11-18", 40.643m)
        .ConPublicacion("2026-11-19", 40.604m)
        .ConPublicacion("2026-11-20", 40.629m)
        .ConPublicacion("2026-11-23", 40.721m)
        .ConPublicacion("2026-11-24", 40.697m);

    private readonly RegistroDeLogsDePrueba _logs = new();
    private readonly ILoggerFactory _fabricaDeLogs;

    public BuscadorCotizacionesDatosUruguayTests()
    {
        _fabricaDeLogs = LoggerFactory.Create(builder => builder.AddProvider(_logs));
    }

    public void Dispose()
    {
        _fabricaDeLogs.Dispose();
        _proveedor.Dispose();
    }

    private Task<CotizacionUsd?> BuscarAsync(string fecha) =>
        BuscarAsync(_proveedor, fecha);

    private Task<CotizacionUsd?> BuscarAsync(ProveedorDatosUruguaySimulado proveedor, string fecha) =>
        proveedor.CrearBuscador(_fabricaDeLogs).BuscarUltimaCotizacionValidaAsync(DateOnly.Parse(fecha), default);

    [Fact]
    public async Task DiaConPublicacion_DevuelveLaDeEseDia()
    {
        var cotizacion = await BuscarAsync("2026-11-23");

        Assert.Equal(new CotizacionUsd(40.721m, new DateOnly(2026, 11, 23), new DateOnly(2026, 11, 23)), cotizacion);
        Assert.True(cotizacion!.EsDeLaFechaSolicitada);
    }

    [Theory]
    [InlineData("2026-11-21")] // sábado
    [InlineData("2026-11-22")] // domingo
    public async Task FinDeSemana_DevuelveLaDelViernesConSuFechaEfectiva(string fecha)
    {
        var cotizacion = await BuscarAsync(fecha);

        Assert.NotNull(cotizacion);
        Assert.Equal(40.629m, cotizacion.Valor);
        Assert.Equal(new DateOnly(2026, 11, 20), cotizacion.FechaPublicacion);
        Assert.Equal(DateOnly.Parse(fecha), cotizacion.FechaSolicitada);
        Assert.False(cotizacion.EsDeLaFechaSolicitada);
    }

    [Fact]
    public async Task DiaSinPublicacionEnMedioDeLaSemana_DevuelveLaDelDiaHabilAnterior()
    {
        // Simula un feriado: el 19 no tuvo publicación.
        using var proveedor = new ProveedorDatosUruguaySimulado()
            .ConPublicacion("2026-11-18", 40.643m)
            .ConPublicacion("2026-11-20", 40.629m);

        var cotizacion = await BuscarAsync(proveedor, "2026-11-19");

        Assert.Equal(new DateOnly(2026, 11, 18), cotizacion!.FechaPublicacion);
        Assert.Equal(40.643m, cotizacion.Valor);
    }

    [Fact]
    public async Task ElPrimerPasoConsultaUnRangoAcotadoQueTerminaEnLaFechaPedida()
    {
        await BuscarAsync("2026-11-24");

        var uri = Assert.Single(_proveedor.Solicitudes);
        Assert.Contains("from=2026-11-10&to=2026-11-24", uri.Query);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-40.5)]
    public async Task ValorCeroONegativo_SeDescartaYSeUsaLaAnteriorValida(decimal valorInvalido)
    {
        using var proveedor = new ProveedorDatosUruguaySimulado()
            .ConPublicacion("2026-11-20", 40.629m)
            .ConPublicacion("2026-11-23", valorInvalido);

        var cotizacion = await BuscarAsync(proveedor, "2026-11-23");

        Assert.Equal(new DateOnly(2026, 11, 20), cotizacion!.FechaPublicacion);
        Assert.Contains(_logs.Entradas, e => e.Nivel == LogLevel.Warning && e.Mensaje.Contains("no es mayor que cero"));
    }

    [Fact]
    public async Task PublicacionPosteriorALaFechaPedida_NuncaSeUsa()
    {
        // Un proveedor que ignora el "to" y manda también una cotización posterior.
        _proveedor.Responder = (_, _) => Task.FromResult(ProveedorDatosUruguaySimulado.Json(
            ProveedorDatosUruguaySimulado.ArmarJson(
                [(new DateOnly(2026, 11, 24), 40.697m), (new DateOnly(2026, 11, 20), 40.629m)], hayMas: false)));

        var cotizacion = await BuscarAsync("2026-11-22");

        Assert.Equal(new DateOnly(2026, 11, 20), cotizacion!.FechaPublicacion);
        Assert.Contains(_logs.Entradas, e => e.Nivel == LogLevel.Warning && e.Mensaje.Contains("posterior"));
    }

    [Fact]
    public async Task RangoConVariasPaginas_LasRecorreTodasYEligeLaMasReciente()
    {
        _proveedor.TamanioMaximoDePagina = 2;

        var cotizacion = await BuscarAsync("2026-11-24");

        Assert.Equal(new DateOnly(2026, 11, 24), cotizacion!.FechaPublicacion);
        Assert.Equal(
            ["offset=0", "offset=2", "offset=4"],
            _proveedor.Solicitudes.Select(u => u.Query.Split('&').Last()));
    }

    [Fact]
    public async Task RangoConVariasPaginas_NoDependeDelOrdenParaElegirLaMasReciente()
    {
        // Página 1 trae una válida vieja; la más reciente válida llega en la página 2.
        var paginas = new Queue<string>(
        [
            ProveedorDatosUruguaySimulado.ArmarJson([(new DateOnly(2026, 11, 24), 0m), (new DateOnly(2026, 11, 17), 40.578m)], hayMas: true),
            ProveedorDatosUruguaySimulado.ArmarJson([(new DateOnly(2026, 11, 23), 40.721m)], hayMas: false)
        ]);
        _proveedor.Responder = (_, _) => Task.FromResult(ProveedorDatosUruguaySimulado.Json(paginas.Dequeue()));

        var cotizacion = await BuscarAsync("2026-11-24");

        Assert.Equal(new DateOnly(2026, 11, 23), cotizacion!.FechaPublicacion);
    }

    [Fact]
    public async Task SinValidasEnElRangoAcotado_BuscaHaciaAtrasSinLimiteInferior()
    {
        using var proveedor = new ProveedorDatosUruguaySimulado().ConPublicacion("2026-09-30", 39.9m);

        var cotizacion = await BuscarAsync(proveedor, "2026-11-24");

        Assert.Equal(new CotizacionUsd(39.9m, new DateOnly(2026, 9, 30), new DateOnly(2026, 11, 24)), cotizacion);
        Assert.Equal(2, proveedor.CantidadDeSolicitudes);
        Assert.DoesNotContain("from=", proveedor.Solicitudes[1].Query);
        Assert.Contains("to=2026-11-09", proveedor.Solicitudes[1].Query);
    }

    [Fact]
    public async Task BusquedaHaciaAtras_RecorrePaginasHastaEncontrarUnaValida()
    {
        using var proveedor = new ProveedorDatosUruguaySimulado { TamanioMaximoDePagina = 2 }
            .ConPublicacion("2026-10-01", 39.5m)
            .ConPublicacion("2026-10-02", 0m)
            .ConPublicacion("2026-10-05", -1m)
            .ConPublicacion("2026-10-06", 0m)
            .ConPublicacion("2026-10-07", 0m);

        var cotizacion = await BuscarAsync(proveedor, "2026-11-24");

        Assert.Equal(new DateOnly(2026, 10, 1), cotizacion!.FechaPublicacion);
        Assert.Equal(4, proveedor.CantidadDeSolicitudes); // 1 del rango + 3 páginas hacia atrás
    }

    [Fact]
    public async Task BusquedaHaciaAtras_CortaEnLaPrimeraValidaSinPedirMasPaginas()
    {
        using var proveedor = new ProveedorDatosUruguaySimulado { TamanioMaximoDePagina = 2 }
            .ConPublicacion("2026-09-01", 39.1m)
            .ConPublicacion("2026-09-02", 39.2m)
            .ConPublicacion("2026-09-03", 39.3m)
            .ConPublicacion("2026-09-04", 39.4m);

        var cotizacion = await BuscarAsync(proveedor, "2026-11-24");

        Assert.Equal(new DateOnly(2026, 9, 4), cotizacion!.FechaPublicacion);
        Assert.Equal(2, proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task SinNingunaPublicacionValidaEnOAntesDeLaFecha_DevuelveNull()
    {
        using var proveedor = new ProveedorDatosUruguaySimulado()
            .ConPublicacion("2026-11-25", 40.7m) // posterior: no cuenta
            .ConPublicacion("2026-11-01", 0m);

        var cotizacion = await BuscarAsync(proveedor, "2026-11-24");

        Assert.Null(cotizacion);
        Assert.Contains(_logs.Entradas, e => e.Nivel == LogLevel.Warning && e.Mensaje.Contains("ninguna cotización válida"));
    }

    [Fact]
    public async Task ProveedorSinDatos_DevuelveNullSoloDespuesDeConfirmarQueNoHayMasResultados()
    {
        using var proveedor = new ProveedorDatosUruguaySimulado();

        var cotizacion = await BuscarAsync(proveedor, "1990-01-10");

        Assert.Null(cotizacion);
        Assert.Equal(2, proveedor.CantidadDeSolicitudes); // el rango acotado vacío no alcanza para afirmarlo
    }

    [Fact]
    public async Task FallaDelProveedorEnElRango_NoSeInterpretaComoAusenciaDePublicacion()
    {
        _proveedor.Responder = (_, _) => Task.FromResult(ProveedorDatosUruguaySimulado.Json("{}", HttpStatusCode.ServiceUnavailable));

        var excepcion = await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => BuscarAsync("2026-11-24"));

        Assert.Equal(MotivoCotizacionNoDisponible.ErrorDelProveedor, excepcion.Motivo);
    }

    [Fact]
    public async Task FallaDelProveedorEnLaBusquedaHaciaAtras_NoSeInterpretaComoAusenciaDePublicacion()
    {
        int solicitud = 0;
        _proveedor.Responder = (_, _) => Task.FromResult(++solicitud == 1
            ? ProveedorDatosUruguaySimulado.Json(ProveedorDatosUruguaySimulado.ArmarJson([], hayMas: false))
            : ProveedorDatosUruguaySimulado.Json("{}", HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => BuscarAsync("2026-11-24"));
    }

    [Fact]
    public async Task BusquedaHaciaAtrasConResultadosFueraDeOrden_SeRechaza()
    {
        int solicitud = 0;
        _proveedor.Responder = (_, _) => Task.FromResult(ProveedorDatosUruguaySimulado.Json(++solicitud == 1
            ? ProveedorDatosUruguaySimulado.ArmarJson([], hayMas: false)
            : ProveedorDatosUruguaySimulado.ArmarJson(
                [(new DateOnly(2026, 9, 1), 39.1m), (new DateOnly(2026, 9, 30), 39.9m)], hayMas: false)));

        var excepcion = await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => BuscarAsync("2026-11-24"));

        Assert.Equal(MotivoCotizacionNoDisponible.RespuestaInvalida, excepcion.Motivo);
    }

    [Fact]
    public async Task DemasiadasPaginasSinValidas_SeInformaBusquedaIncompletaYNoAusencia()
    {
        using var proveedor = new ProveedorDatosUruguaySimulado { TamanioMaximoDePagina = 1 };
        for (int dia = 1; dia <= 30; dia++)
        {
            proveedor.ConPublicacion($"2026-09-{dia:00}", 0m);
        }

        var excepcion = await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => BuscarAsync(proveedor, "2026-11-24"));

        Assert.Equal(MotivoCotizacionNoDisponible.BusquedaIncompleta, excepcion.Motivo);
        Assert.Equal(BuscadorCotizacionesDatosUruguay.MaximoDePaginasPorBusqueda, proveedor.CantidadDeSolicitudes);
    }
}
