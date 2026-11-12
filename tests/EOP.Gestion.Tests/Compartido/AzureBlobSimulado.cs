using System.Net;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using EOP.Gestion.Infrastructure.Almacenamiento;

namespace EOP.Gestion.Tests.Compartido;

/// <summary>
/// Azure Blob Storage simulado: el SDK real arma y envía los pedidos HTTP, pero en vez de salir a
/// internet los atiende esta clase. Permite probar la integración sin cuenta de Azure, sin red
/// y sin credenciales, y ver exactamente qué le mandaríamos a Azure.
/// </summary>
internal sealed class AzureBlobSimulado
{
    public const string TokenDePrueba = "token-de-prueba-no-real";

    public List<PedidoRecibido> Pedidos { get; } = [];

    public RegistroDeLogsDePrueba Logs { get; } = new();

    // Lo que contesta el "Azure" simulado. Por defecto: 201 (guardado correcto).
    public Func<PedidoRecibido, HttpResponseMessage> Responder { get; set; } = _ => Respuesta(HttpStatusCode.Created);

    public TokenCredential Credencial { get; set; } = new CredencialDePrueba();

    public ServicioAlmacenamientoBlob CrearServicio()
    {
        var opciones = new BlobClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(new HttpClient(new Manejador(this))),
        };
        // Sin reintentos: si el simulado devuelve un error, el test lo ve enseguida y sin esperas.
        opciones.Retry.MaxRetries = 0;

        var contenedor = new BlobContainerClient(
            new Uri("https://cuentadeprueba.blob.core.windows.net/documentos-eop"),
            Credencial,
            opciones);

        var fabricaDeLogs = LoggerFactory.Create(constructor => constructor.AddProvider(Logs));
        return new ServicioAlmacenamientoBlob(contenedor, fabricaDeLogs.CreateLogger<ServicioAlmacenamientoBlob>());
    }

    public static HttpResponseMessage Respuesta(HttpStatusCode estado, string? codigoDeError = null)
    {
        var respuesta = new HttpResponseMessage(estado);
        respuesta.Headers.TryAddWithoutValidation("ETag", "\"0x8DC0000000000000\"");
        respuesta.Content = new ByteArrayContent([]);
        respuesta.Content.Headers.LastModified = DateTimeOffset.UtcNow;
        if (codigoDeError != null)
        {
            respuesta.Headers.TryAddWithoutValidation("x-ms-error-code", codigoDeError);
        }
        return respuesta;
    }

    public static HttpResponseMessage RespuestaConArchivo(byte[] contenido, string? tipoContenido)
    {
        var respuesta = Respuesta(HttpStatusCode.OK);
        respuesta.Content = new ByteArrayContent(contenido);
        respuesta.Content.Headers.LastModified = DateTimeOffset.UtcNow;
        if (tipoContenido != null)
        {
            respuesta.Content.Headers.TryAddWithoutValidation("Content-Type", tipoContenido);
        }
        respuesta.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");
        return respuesta;
    }

    internal sealed record PedidoRecibido(
        HttpMethod Metodo,
        Uri Direccion,
        IReadOnlyDictionary<string, string> Encabezados,
        byte[] Cuerpo);

    private sealed class Manejador(AzureBlobSimulado simulado) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken cancellationToken)
        {
            var encabezados = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var encabezado in solicitud.Headers.Concat(solicitud.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()))
            {
                encabezados[encabezado.Key] = string.Join(",", encabezado.Value);
            }

            byte[] cuerpo = solicitud.Content == null ? [] : await solicitud.Content.ReadAsByteArrayAsync(cancellationToken);
            var pedido = new PedidoRecibido(solicitud.Method, solicitud.RequestUri!, encabezados, cuerpo);
            simulado.Pedidos.Add(pedido);

            return simulado.Responder(pedido);
        }
    }

    private sealed class CredencialDePrueba : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(TokenDePrueba, DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(GetToken(requestContext, cancellationToken));
    }

    /// <summary>Credencial que siempre falla, como pasaría sin sesión de Azure o sin identidad administrada.</summary>
    internal sealed class CredencialQueFalla : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            throw new CredentialUnavailableException("detalle interno de la credencial");

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            throw new CredentialUnavailableException("detalle interno de la credencial");
    }
}
