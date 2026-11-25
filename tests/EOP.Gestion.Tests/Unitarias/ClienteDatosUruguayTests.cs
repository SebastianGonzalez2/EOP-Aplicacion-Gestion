using System.Net;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T18 — Comunicación con datosUruguay: armado de la consulta, lectura del JSON y traducción de
// cada falla a CotizacionNoDisponibleException con su motivo.
public sealed class ClienteDatosUruguayTests : IDisposable
{
    private static readonly DateOnly Hasta = new(2026, 11, 24);

    private readonly ProveedorDatosUruguaySimulado _proveedor = new();
    private readonly RegistroDeLogsDePrueba _logs = new();
    private readonly ILoggerFactory _fabricaDeLogs;

    public ClienteDatosUruguayTests()
    {
        _fabricaDeLogs = LoggerFactory.Create(builder => builder.AddProvider(_logs));
    }

    public void Dispose()
    {
        _fabricaDeLogs.Dispose();
        _proveedor.Dispose();
    }

    private Task<PaginaCotizacionesDatosUruguay> PedirPaginaAsync(
        DateOnly? desde = null, CancellationToken cancellationToken = default, TimeSpan? tiempoMaximo = null) =>
        _proveedor.CrearCliente(_fabricaDeLogs, tiempoMaximo).ObtenerPaginaAsync(desde, Hasta, 100, 0, cancellationToken);

    private void ResponderSiempre(string json, HttpStatusCode codigo = HttpStatusCode.OK) =>
        _proveedor.Responder = (_, _) => Task.FromResult(ProveedorDatosUruguaySimulado.Json(json, codigo));

    private async Task<CotizacionNoDisponibleException> PedirYEsperarFallaAsync(TimeSpan? tiempoMaximo = null) =>
        await Assert.ThrowsAsync<CotizacionNoDisponibleException>(() => PedirPaginaAsync(tiempoMaximo: tiempoMaximo));

    [Fact]
    public async Task ArmaLaConsultaConRangoLimiteYDesplazamiento()
    {
        await _proveedor.CrearCliente().ObtenerPaginaAsync(new DateOnly(2026, 11, 10), Hasta, 100, 200, default);

        var uri = Assert.Single(_proveedor.Solicitudes);
        Assert.Equal(
            "https://datosuruguay.com/api/v1/exchange-rates/usd?from=2026-11-10&to=2026-11-24&limit=100&offset=200",
            uri.ToString());
    }

    [Fact]
    public async Task SinFechaDesde_NoEnviaElParametroFrom()
    {
        await _proveedor.CrearCliente().ObtenerPaginaAsync(null, Hasta, 100, 0, default);

        Assert.DoesNotContain("from=", Assert.Single(_proveedor.Solicitudes).Query);
    }

    [Fact]
    public async Task DeserializaUnaRespuestaRealDelProveedor()
    {
        // Respuesta copiada de la API el 25/11/2026 (limit=3).
        ResponderSiempre("""
            {"data":[{"date":"2026-11-24","series":"usd","value":40.697},{"date":"2026-11-23","series":"usd","value":40.721},
            {"date":"2026-11-20","series":"usd","value":40.629}],"meta":{"indicator":"usd","source":"BCU","unit":"UYU/USD",
            "frequency":"daily","last_updated":"2026-11-24","count":3,"limit":3,"offset":0,"total":9,"has_more":true,
            "license":"CC-BY-4.0","attribution":{"required":true,"text":"Datos de datosUruguay.com",
            "url":"https://datosuruguay.com/dolar?utm_source=api&utm_medium=attribution&utm_campaign=backlinks"}}}
            """);

        var pagina = await PedirPaginaAsync();

        Assert.True(pagina.HayMasPaginas);
        Assert.Equal(
            [
                new RegistroCotizacionDatosUruguay(new DateOnly(2026, 11, 24), 40.697m),
                new RegistroCotizacionDatosUruguay(new DateOnly(2026, 11, 23), 40.721m),
                new RegistroCotizacionDatosUruguay(new DateOnly(2026, 11, 20), 40.629m)
            ],
            pagina.Registros);
    }

    [Fact]
    public async Task ConservaTodosLosDecimalesDelValorSinRedondear()
    {
        ResponderSiempre("""
            {"data":[{"date":"2026-11-24","series":"usd","value":40.6971234567891234}],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}
            """);

        var pagina = await PedirPaginaAsync();

        Assert.Equal(40.6971234567891234m, Assert.Single(pagina.Registros).Valor);
    }

    [Fact]
    public async Task PaginaVaciaSinMasResultados_EsValida()
    {
        ResponderSiempre("""{"data":[],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""");

        var pagina = await PedirPaginaAsync();

        Assert.Empty(pagina.Registros);
        Assert.False(pagina.HayMasPaginas);
    }

    [Fact]
    public async Task ConservaLosValoresCeroONegativosParaQueLosDescarteElBuscador()
    {
        ResponderSiempre("""
            {"data":[{"date":"2026-11-24","series":"usd","value":0},{"date":"2026-11-23","series":"usd","value":-1.5}],
            "meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}
            """);

        var pagina = await PedirPaginaAsync();

        Assert.Equal([0m, -1.5m], pagina.Registros.Select(r => r.Valor));
    }

    [Theory]
    [InlineData("esto no es JSON")]
    [InlineData("<html><body>Error</body></html>")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("""{"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[]}""")]
    [InlineData("""{"data":[],"meta":{"source":"BCU","unit":"UYU/USD"}}""")]
    [InlineData("""{"data":[{"series":"usd","value":40.1}],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[{"date":"2026-11-24","series":"usd"}],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[{"date":"2026-11-24","series":"usd","value":null}],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[{"date":"2026-11-24","series":"usd","value":"40.1"}],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[{"date":"24/11/2026","series":"usd","value":40.1}],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[{"date":"2026-11-24","series":"eur","value":47.1}],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[null],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[{"date":"2026-11-24","series":"usd","value":40.1}],"meta":{"source":"INE","unit":"UYU/USD","has_more":false}}""")]
    [InlineData("""{"data":[{"date":"2026-11-24","series":"usd","value":40.1}],"meta":{"source":"BCU","unit":"USD/UYU","has_more":false}}""")]
    [InlineData("""{"data":[],"meta":{"source":"BCU","unit":"UYU/USD","has_more":true}}""")]
    [InlineData("""{"data":[{"date":"2026-11-24","series":"usd","value":1e40}],"meta":{"source":"BCU","unit":"UYU/USD","has_more":false}}""")]
    public async Task RespuestaInvalidaOIncompleta_SeInformaComoRespuestaInvalida(string json)
    {
        ResponderSiempre(json);

        var excepcion = await PedirYEsperarFallaAsync();

        Assert.Equal(MotivoCotizacionNoDisponible.RespuestaInvalida, excepcion.Motivo);
        Assert.Contains(_logs.Entradas, e => e.Nivel == LogLevel.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task CodigoDeErrorHttp_SeInformaComoErrorDelProveedorYSeRegistra(HttpStatusCode codigo)
    {
        ResponderSiempre("""{"error":{"code":"x","message":"y","detail":"z"}}""", codigo);

        var excepcion = await PedirYEsperarFallaAsync();

        Assert.Equal(MotivoCotizacionNoDisponible.ErrorDelProveedor, excepcion.Motivo);
        Assert.Contains(_logs.Entradas, e => e.Nivel == LogLevel.Error && e.Valores.Contains($"CodigoHttp={(int)codigo}"));
    }

    [Fact]
    public async Task Http429_SeInformaComoLimiteDeSolicitudesYRegistraElTiempoDeEspera()
    {
        _proveedor.Responder = (_, _) =>
        {
            var respuesta = ProveedorDatosUruguaySimulado.Json(
                """{"error":{"code":"too_many_requests","message":"Demasiadas solicitudes."}}""", HttpStatusCode.TooManyRequests);
            respuesta.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
            return Task.FromResult(respuesta);
        };

        var excepcion = await PedirYEsperarFallaAsync();

        Assert.Equal(MotivoCotizacionNoDisponible.LimiteDeSolicitudesExcedido, excepcion.Motivo);
        Assert.Contains("unos minutos", excepcion.Message);
        Assert.Contains(_logs.Entradas, e => e.Nivel == LogLevel.Warning && e.Valores.Contains("SegundosDeEspera=7"));
    }

    [Fact]
    public async Task FallaDeConexion_SeInformaComoFallaDeConexion()
    {
        _proveedor.Responder = (_, _) => throw new HttpRequestException("No se puede establecer una conexión.");

        var excepcion = await PedirYEsperarFallaAsync();

        Assert.Equal(MotivoCotizacionNoDisponible.FallaDeConexion, excepcion.Motivo);
        Assert.IsType<HttpRequestException>(excepcion.InnerException);
        Assert.Contains(_logs.Entradas, e => e.Nivel == LogLevel.Warning && e.Excepcion is HttpRequestException);
    }

    [Fact]
    public async Task ProveedorQueNoResponde_SeInformaComoTiempoDeEsperaAgotado()
    {
        _proveedor.Responder = async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            throw new InvalidOperationException("No debería llegar acá.");
        };

        var excepcion = await PedirYEsperarFallaAsync(TimeSpan.FromMilliseconds(100));

        Assert.Equal(MotivoCotizacionNoDisponible.TiempoDeEsperaAgotado, excepcion.Motivo);
        Assert.Contains("tardó demasiado", excepcion.Message);
    }

    [Fact]
    public async Task CancelacionDeQuienLlama_NoSeConfundeConUnaFallaDelProveedor()
    {
        using var cancelacion = new CancellationTokenSource();
        await cancelacion.CancelAsync();

        var excepcion = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => PedirPaginaAsync(cancellationToken: cancelacion.Token));

        Assert.IsNotType<CotizacionNoDisponibleException>(excepcion);
        Assert.DoesNotContain(_logs.Entradas, e => e.Nivel >= LogLevel.Warning);
    }

    [Fact]
    public async Task CancelacionMientrasEsperaLaRespuesta_SePropagaComoCancelacion()
    {
        _proveedor.Compuerta = new TaskCompletionSource();
        using var cancelacion = new CancellationTokenSource();

        var pedido = PedirPaginaAsync(cancellationToken: cancelacion.Token);
        await _proveedor.EsperarSolicitudesAsync(1);
        await cancelacion.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pedido);
    }

    [Fact]
    public async Task RegistraCadaLlamadaAlServicioExterno()
    {
        await PedirPaginaAsync();

        Assert.Contains(_logs.Entradas, e =>
            e.Nivel == LogLevel.Information && e.Mensaje.Contains("exchange-rates/usd?to=2026-11-24"));
    }

    [Fact]
    public void ElMensajeDeLaFallaEsGenericoYSePuedeMostrar()
    {
        var excepcion = new CotizacionNoDisponibleException(
            MotivoCotizacionNoDisponible.ErrorDelProveedor, new HttpRequestException("detalle técnico interno"));

        Assert.StartsWith("No se pudo obtener la cotización del dólar", excepcion.Message);
        Assert.DoesNotContain("detalle técnico", excepcion.Message);
    }
}
