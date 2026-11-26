using System.Net;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Cotizaciones;
using EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T18 — Servicio de cotizaciones: fecha actual de Uruguay, caché compartida con expiración
// absoluta (15 min la vigente, 24 h las históricas), sin guardar errores ni ausencias, y una sola
// consulta al proveedor cuando varios pedidos de la misma fecha llegan a la vez (DT-12).
public sealed class ServicioCotizacionesDatosUruguayTests : IDisposable
{
    // 25/11/2026 15:00 UTC = 12:00 en Uruguay (UTC−3).
    private static readonly DateTimeOffset MiercolesAlMediodiaEnUruguay = new(2026, 11, 25, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = new(2026, 11, 25);
    private static readonly DateOnly Ayer = new(2026, 11, 24);

    private readonly ProveedorDatosUruguaySimulado _proveedor = new ProveedorDatosUruguaySimulado()
        .ConPublicacion("2026-11-19", 40.604m)
        .ConPublicacion("2026-11-20", 40.629m)
        .ConPublicacion("2026-11-23", 40.721m)
        .ConPublicacion("2026-11-24", 40.697m);

    private readonly RelojAjustable _reloj = new(MiercolesAlMediodiaEnUruguay);
    private readonly MemoryCache _cache;
    private readonly ServicioCotizacionesDatosUruguay _servicio;

    public ServicioCotizacionesDatosUruguayTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions { Clock = _reloj });
        _servicio = CrearServicio(_cache);
    }

    public void Dispose()
    {
        _cache.Dispose();
        _proveedor.Dispose();
    }

    private ServicioCotizacionesDatosUruguay CrearServicio(IMemoryCache cache) =>
        new(_proveedor.CrearBuscador(), cache, _reloj, NullLogger<ServicioCotizacionesDatosUruguay>.Instance);

    private void ProveedorCaido() =>
        _proveedor.Responder = (_, _) => Task.FromResult(ProveedorDatosUruguaySimulado.Json("{}", HttpStatusCode.ServiceUnavailable));

    private void ProveedorRecuperado() => _proveedor.Responder = null;

    // ---------- Cotización vigente e histórica ----------

    [Fact]
    public async Task Vigente_SinPublicacionDeHoy_DevuelveLaUltimaAnteriorConSuFecha()
    {
        var cotizacion = await _servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(new CotizacionUsd(40.697m, Ayer, Hoy), cotizacion);
    }

    [Fact]
    public async Task Vigente_ConPublicacionDeHoy_DevuelveLaDeHoy()
    {
        _proveedor.ConPublicacion("2026-11-25", 40.8123m);

        var cotizacion = await _servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(new CotizacionUsd(40.8123m, Hoy, Hoy), cotizacion);
    }

    [Fact]
    public async Task Vigente_UsaLaFechaDeUruguayYNoLaDeUtc()
    {
        // 26/11 01:30 UTC todavía es 25/11 22:30 en Uruguay.
        var reloj = new RelojAjustable(new DateTimeOffset(2026, 11, 26, 1, 30, 0, TimeSpan.Zero));
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = reloj });
        var servicio = new ServicioCotizacionesDatosUruguay(
            _proveedor.CrearBuscador(), cache, reloj, NullLogger<ServicioCotizacionesDatosUruguay>.Instance);

        var cotizacion = await servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(Hoy, cotizacion!.FechaSolicitada);
        Assert.Contains("to=2026-11-25", _proveedor.Solicitudes[0].Query);
    }

    [Fact]
    public async Task Historica_DevuelveLaDeLaFechaPedida()
    {
        var cotizacion = await _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 19));

        Assert.Equal(new CotizacionUsd(40.604m, new DateOnly(2026, 11, 19), new DateOnly(2026, 11, 19)), cotizacion);
    }

    [Fact]
    public async Task Historica_DeUnDomingo_DevuelveLaDelViernes()
    {
        var cotizacion = await _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 22));

        Assert.Equal(new DateOnly(2026, 11, 20), cotizacion!.FechaPublicacion);
        Assert.Equal(new DateOnly(2026, 11, 22), cotizacion.FechaSolicitada);
    }

    [Fact]
    public async Task Historica_DeUnaFechaFutura_SeRechazaSinConsultarAlProveedor()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _servicio.ObtenerCotizacionHistoricaAsync(Hoy.AddDays(1)));

        Assert.Equal(0, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task Historica_DeHoy_EsLaMismaQueLaVigente()
    {
        var historica = await _servicio.ObtenerCotizacionHistoricaAsync(Hoy);
        var vigente = await _servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(historica, vigente);
        Assert.Equal(1, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task SinPublicacion_DevuelveNull()
    {
        var cotizacion = await _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(1990, 1, 10));

        Assert.Null(cotizacion);
    }

    [Fact]
    public async Task FallaDelProveedor_SeInformaConExcepcionYNoComoAusencia()
    {
        ProveedorCaido();

        var excepcion = await Assert.ThrowsAsync<CotizacionNoDisponibleException>(
            () => _servicio.ObtenerCotizacionVigenteAsync());

        Assert.Equal(MotivoCotizacionNoDisponible.ErrorDelProveedor, excepcion.Motivo);
    }

    // ---------- Caché ----------

    [Fact]
    public async Task ReutilizaLaCotizacionEnCacheSinVolverAConsultar()
    {
        var primera = await _servicio.ObtenerCotizacionVigenteAsync();
        var segunda = await _servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(primera, segunda);
        Assert.Equal(1, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task LaCacheEsCompartidaEntreInstanciasDelServicio()
    {
        // Dos pedidos de usuarios distintos usan la misma IMemoryCache de la aplicación.
        await CrearServicio(_cache).ObtenerCotizacionVigenteAsync();
        await CrearServicio(_cache).ObtenerCotizacionVigenteAsync();

        Assert.Equal(1, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task Vigente_SigueEnCacheHastaLos15MinutosYDespuesSeVuelveAConsultar()
    {
        await _servicio.ObtenerCotizacionVigenteAsync();

        _reloj.Avanzar(TimeSpan.FromMinutes(14) + TimeSpan.FromSeconds(59));
        await _servicio.ObtenerCotizacionVigenteAsync();
        Assert.Equal(1, _proveedor.CantidadDeSolicitudes);

        _reloj.Avanzar(TimeSpan.FromSeconds(2));
        _proveedor.ConPublicacion("2026-11-25", 40.9m);
        var renovada = await _servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(2, _proveedor.CantidadDeSolicitudes);
        Assert.Equal(new CotizacionUsd(40.9m, Hoy, Hoy), renovada);
    }

    [Fact]
    public async Task Historica_SigueEnCacheHastaLas24HorasYDespuesSeVuelveAConsultar()
    {
        var fecha = new DateOnly(2026, 11, 20);
        await _servicio.ObtenerCotizacionHistoricaAsync(fecha);

        _reloj.Avanzar(TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59));
        await _servicio.ObtenerCotizacionHistoricaAsync(fecha);
        Assert.Equal(1, _proveedor.CantidadDeSolicitudes);

        _reloj.Avanzar(TimeSpan.FromMinutes(2));
        await _servicio.ObtenerCotizacionHistoricaAsync(fecha);
        Assert.Equal(2, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task LaExpiracionNoDisparaConsultasPorSiSola()
    {
        await _servicio.ObtenerCotizacionVigenteAsync();

        _reloj.Avanzar(TimeSpan.FromHours(2));
        _cache.Compact(0); // fuerza a la caché a revisar sus entradas vencidas

        Assert.Equal(1, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task FechasDistintasUsanEntradasDistintas()
    {
        var del23 = await _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 23));
        var del20 = await _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 20));
        var del23OtraVez = await _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 23));

        Assert.Equal(40.721m, del23!.Valor);
        Assert.Equal(40.629m, del20!.Valor);
        Assert.Equal(del23, del23OtraVez);
        Assert.Equal(2, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task FechasQueCaenEnLaMismaPublicacion_ConservanCadaUnaSuFechaSolicitada()
    {
        var sabado = await _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 21));
        var domingo = await _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 22));

        Assert.Equal(new CotizacionUsd(40.629m, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 21)), sabado);
        Assert.Equal(new CotizacionUsd(40.629m, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 22)), domingo);
    }

    [Fact]
    public async Task VigenteYDeAyer_NoSeMezclanEnLaCache()
    {
        _proveedor.ConPublicacion("2026-11-25", 40.8m);

        var vigente = await _servicio.ObtenerCotizacionVigenteAsync();
        var deAyer = await _servicio.ObtenerCotizacionHistoricaAsync(Ayer);

        Assert.Equal(Hoy, vigente!.FechaPublicacion);
        Assert.Equal(Ayer, deAyer!.FechaPublicacion);
    }

    [Fact]
    public async Task AlCambiarElDiaEnUruguay_LaVigenteSeConsultaDeNuevo()
    {
        await _servicio.ObtenerCotizacionVigenteAsync();

        _reloj.Avanzar(TimeSpan.FromHours(12) + TimeSpan.FromMinutes(1)); // 26/11 00:01 en Uruguay
        var cotizacion = await _servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(new DateOnly(2026, 11, 26), cotizacion!.FechaSolicitada);
        Assert.Equal(2, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task UnaFalla_NoSeGuardaEnCacheYElSiguientePedidoVuelveAConsultar()
    {
        ProveedorCaido();
        await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => _servicio.ObtenerCotizacionVigenteAsync());

        ProveedorRecuperado();
        var cotizacion = await _servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(40.697m, cotizacion!.Valor);
        Assert.Equal(2, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task LaAusenciaDePublicacion_NoSeGuardaEnCache()
    {
        var fecha = new DateOnly(1990, 1, 10);
        await _servicio.ObtenerCotizacionHistoricaAsync(fecha);
        int solicitudesDeLaPrimeraBusqueda = _proveedor.CantidadDeSolicitudes;

        await _servicio.ObtenerCotizacionHistoricaAsync(fecha);

        Assert.Equal(2 * solicitudesDeLaPrimeraBusqueda, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task UnaRespuestaInvalida_NoSeGuardaEnCache()
    {
        _proveedor.Responder = (_, _) => Task.FromResult(ProveedorDatosUruguaySimulado.Json("no es json"));
        await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => _servicio.ObtenerCotizacionVigenteAsync());

        Assert.Equal(0, _cache.Count);
    }

    [Fact]
    public async Task UnaEntradaVencida_NoSeUsaParaOcultarUnaFallaDelProveedor()
    {
        await _servicio.ObtenerCotizacionVigenteAsync();

        _reloj.Avanzar(TimeSpan.FromMinutes(16));
        ProveedorCaido();

        await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => _servicio.ObtenerCotizacionVigenteAsync());
    }

    [Fact]
    public async Task UnaEntradaVigente_SeSigueUsandoAunqueElProveedorEsteCaido()
    {
        var antes = await _servicio.ObtenerCotizacionVigenteAsync();

        ProveedorCaido();
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        var despues = await _servicio.ObtenerCotizacionVigenteAsync();

        Assert.Equal(antes, despues);
    }

    // ---------- Concurrencia y cancelación ----------

    [Fact]
    public async Task PedidosSimultaneosDeLaMismaFecha_ConsultanAlProveedorUnaSolaVez()
    {
        _proveedor.Compuerta = new TaskCompletionSource();

        var pedidos = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => _servicio.ObtenerCotizacionVigenteAsync()))
            .ToArray();

        await _proveedor.EsperarSolicitudesAsync(1);
        await Task.Delay(100); // margen para que los demás pedidos lleguen al turno de la fecha
        _proveedor.Compuerta.SetResult();

        var resultados = await Task.WhenAll(pedidos);

        Assert.Equal(1, _proveedor.CantidadDeSolicitudes);
        Assert.All(resultados, r => Assert.Equal(new CotizacionUsd(40.697m, Ayer, Hoy), r));
    }

    [Fact]
    public async Task PedidosSimultaneosDeFechasDistintas_NoSeEsperanEntreSi()
    {
        _proveedor.Compuerta = new TaskCompletionSource();

        var del20 = _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 20));
        var del23 = _servicio.ObtenerCotizacionHistoricaAsync(new DateOnly(2026, 11, 23));

        await _proveedor.EsperarSolicitudesAsync(2); // las dos consultas salieron sin esperar a la otra
        _proveedor.Compuerta.SetResult();

        Assert.Equal(40.629m, (await del20)!.Valor);
        Assert.Equal(40.721m, (await del23)!.Valor);
    }

    [Fact]
    public async Task SiLaPrimeraConsultaSimultaneaFalla_LaSiguienteVuelveAIntentarYNoRecibeElError()
    {
        _proveedor.Compuerta = new TaskCompletionSource();
        int solicitud = 0;
        _proveedor.Responder = (_, _) => Task.FromResult(Interlocked.Increment(ref solicitud) == 1
            ? ProveedorDatosUruguaySimulado.Json("{}", HttpStatusCode.ServiceUnavailable)
            : ProveedorDatosUruguaySimulado.Json(ProveedorDatosUruguaySimulado.ArmarJson([(Ayer, 40.697m)], hayMas: false)));

        var primero = _servicio.ObtenerCotizacionVigenteAsync();
        await _proveedor.EsperarSolicitudesAsync(1);
        var segundo = _servicio.ObtenerCotizacionVigenteAsync();
        _proveedor.Compuerta.SetResult();

        await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => primero);
        Assert.Equal(40.697m, (await segundo)!.Valor);
        Assert.Equal(2, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task CancelarMientrasSeEsperaElTurno_DejaDeEsperarSinAfectarAlOtroPedido()
    {
        _proveedor.Compuerta = new TaskCompletionSource();
        using var cancelacion = new CancellationTokenSource();

        var primero = _servicio.ObtenerCotizacionVigenteAsync();
        await _proveedor.EsperarSolicitudesAsync(1);
        var segundo = _servicio.ObtenerCotizacionVigenteAsync(cancelacion.Token);
        await cancelacion.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => segundo);

        _proveedor.Compuerta.SetResult();
        Assert.Equal(40.697m, (await primero)!.Valor);
        Assert.Equal(1, _proveedor.CantidadDeSolicitudes);
    }

    [Fact]
    public async Task CancelarLaConsulta_NoGuardaNadaEnCacheYLiberaElTurno()
    {
        _proveedor.Compuerta = new TaskCompletionSource();
        using var cancelacion = new CancellationTokenSource();

        var cancelado = _servicio.ObtenerCotizacionVigenteAsync(cancelacion.Token);
        await _proveedor.EsperarSolicitudesAsync(1);
        await cancelacion.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelado);
        Assert.Equal(0, _cache.Count);

        _proveedor.Compuerta.SetResult();
        var siguiente = await _servicio.ObtenerCotizacionVigenteAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(40.697m, siguiente!.Valor);
    }
}
