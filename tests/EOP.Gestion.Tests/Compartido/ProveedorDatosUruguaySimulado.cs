using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EOP.Gestion.Tests.Compartido;

// Reemplaza a datosUruguay en las pruebas: se conecta como manejador del HttpClient, así que el código
// de producción arma y envía la solicitud real, pero nunca sale a internet.
//
// Por defecto imita a la API: filtra por from/to, ordena de la más reciente a la más antigua, aplica
// limit/offset y arma "meta" con has_more. Para simular fallas se reemplaza la respuesta con Responder,
// y para simular demoras o pedidos simultáneos se cierra la Compuerta.
internal sealed class ProveedorDatosUruguaySimulado : HttpMessageHandler
{
    private readonly List<(DateOnly Fecha, decimal Valor)> _publicaciones = [];
    private readonly ConcurrentQueue<Uri> _solicitudes = new();

    // Simula un proveedor que devuelve menos resultados por página que el limit pedido,
    // para obligar al código a recorrer varias páginas.
    public int TamanioMaximoDePagina { get; set; } = int.MaxValue;

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Responder { get; set; }

    // Mientras no se complete, las solicitudes quedan esperando (después de quedar anotadas).
    public TaskCompletionSource? Compuerta { get; set; }

    public IReadOnlyList<Uri> Solicitudes => _solicitudes.ToArray();

    public int CantidadDeSolicitudes => _solicitudes.Count;

    public ProveedorDatosUruguaySimulado ConPublicacion(string fecha, decimal valor)
    {
        _publicaciones.Add((DateOnly.ParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture), valor));
        return this;
    }

    public HttpClient CrearHttpClient(TimeSpan? tiempoMaximo = null) =>
        new(this, disposeHandler: false)
        {
            BaseAddress = new Uri(ClienteDatosUruguay.UrlBase),
            Timeout = tiempoMaximo ?? ClienteDatosUruguay.TiempoMaximoDeEspera
        };

    public ClienteDatosUruguay CrearCliente(ILoggerFactory? logs = null, TimeSpan? tiempoMaximo = null) =>
        new(CrearHttpClient(tiempoMaximo), (logs ?? NullLoggerFactory.Instance).CreateLogger<ClienteDatosUruguay>());

    public BuscadorCotizacionesDatosUruguay CrearBuscador(ILoggerFactory? logs = null) =>
        new(CrearCliente(logs), (logs ?? NullLoggerFactory.Instance).CreateLogger<BuscadorCotizacionesDatosUruguay>());

    public async Task EsperarSolicitudesAsync(int cantidad)
    {
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (CantidadDeSolicitudes < cantidad)
        {
            await Task.Delay(5, limite.Token);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _solicitudes.Enqueue(request.RequestUri!);

        if (Compuerta is not null)
        {
            await Compuerta.Task.WaitAsync(cancellationToken);
        }

        if (Responder is not null)
        {
            return await Responder(request, cancellationToken);
        }

        return ResponderComoLaApi(request.RequestUri!);
    }

    private HttpResponseMessage ResponderComoLaApi(Uri uri)
    {
        var parametros = QueryHelpers.ParseQuery(uri.Query);
        DateOnly? desde = parametros.TryGetValue("from", out var from) ? DateOnly.Parse(from!, CultureInfo.InvariantCulture) : null;
        DateOnly? hasta = parametros.TryGetValue("to", out var to) ? DateOnly.Parse(to!, CultureInfo.InvariantCulture) : null;
        int limite = parametros.TryGetValue("limit", out var limit) ? int.Parse(limit!, CultureInfo.InvariantCulture) : 500;
        int desplazamiento = parametros.TryGetValue("offset", out var offset) ? int.Parse(offset!, CultureInfo.InvariantCulture) : 0;

        var filtradas = _publicaciones
            .Where(p => (desde is null || p.Fecha >= desde) && (hasta is null || p.Fecha <= hasta))
            .OrderByDescending(p => p.Fecha)
            .ToList();

        var pagina = filtradas.Skip(desplazamiento).Take(Math.Min(limite, TamanioMaximoDePagina)).ToList();
        bool hayMas = desplazamiento + pagina.Count < filtradas.Count;

        return Json(ArmarJson(pagina, hayMas));
    }

    public static string ArmarJson(IEnumerable<(DateOnly Fecha, decimal Valor)> puntos, bool hayMas,
        string fuente = "BCU", string unidad = "UYU/USD")
    {
        var datos = string.Join(",", puntos.Select(p =>
            $$"""{"date":"{{p.Fecha:yyyy-MM-dd}}","series":"usd","value":{{p.Valor.ToString(CultureInfo.InvariantCulture)}}}"""));

        const string atribucion = """
            "license":"CC-BY-4.0","attribution":{"required":true,"text":"Datos de datosUruguay.com","url":"https://datosuruguay.com/dolar"}
            """;

        return $$$"""
            {"data":[{{{datos}}}],"meta":{"indicator":"usd","source":"{{{fuente}}}","unit":"{{{unidad}}}","frequency":"daily",
            "has_more":{{{(hayMas ? "true" : "false")}}},{{{atribucion}}}}}
            """;
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode codigo = HttpStatusCode.OK) =>
        new(codigo) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
