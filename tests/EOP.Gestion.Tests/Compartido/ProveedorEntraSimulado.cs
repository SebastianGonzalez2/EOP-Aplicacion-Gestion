using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace EOP.Gestion.Tests.Compartido;

// Un Entra ID de mentira que SÍ habla el protocolo OpenID Connect, para ejercitar el manejador real de
// ASP.NET Core (OpenIdConnectHandler) y lo que Program.cs y Microsoft.Identity.Web configuran sobre él:
// redirección con PKCE, correlación, canje del código, validación del JWT, mapeo de claims y cierre de sesión.
//
// No sale nada a la red: el documento de metadatos es estático y el canal del canje de código (Backchannel)
// es esta misma clase. Por eso NO comprueba que el Entra ID real exija MFA: eso depende de la configuración
// del tenant y se verifica a mano.
internal sealed class ProveedorEntraSimulado : HttpMessageHandler
{
    private readonly RSA _claveDeFirma = RSA.Create(2048);
    private readonly RSA _claveAjena = RSA.Create(2048);
    private readonly ConcurrentDictionary<string, CodigoEmitido> _codigosEmitidos = new();

    private string _clientId = string.Empty;
    private string _redirectUri = string.Empty;

    // El emisor de los tokens v2.0 termina en /v2.0, pero los endpoints cuelgan directamente del tenant.
    public string Emisor { get; private set; } = string.Empty;

    public string BaseDelTenant { get; private set; } = string.Empty;

    public int CanjesConPkceVerificado { get; private set; }

    // Lo que responde el endpoint de tokens en lugar de un canje normal (por ejemplo, invalid_grant).
    public Func<HttpResponseMessage>? RespuestaForzadaDelEndpointDeTokens { get; set; }

    public void AplicarA(OpenIdConnectOptions opciones)
    {
        Emisor = opciones.Authority!;
        BaseDelTenant = Emisor.EndsWith("/v2.0", StringComparison.Ordinal) ? Emisor[..^"/v2.0".Length] : Emisor;
        _clientId = opciones.ClientId!;
        _redirectUri = "https://localhost" + opciones.CallbackPath;

        var configuracion = new OpenIdConnectConfiguration
        {
            Issuer = Emisor,
            AuthorizationEndpoint = $"{BaseDelTenant}/oauth2/v2.0/authorize",
            TokenEndpoint = $"{BaseDelTenant}/oauth2/v2.0/token",
            EndSessionEndpoint = $"{BaseDelTenant}/oauth2/v2.0/logout"
        };
        configuracion.SigningKeys.Add(CrearClave(_claveDeFirma));

        opciones.Configuration = configuracion;
        opciones.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuracion);
        opciones.Backchannel = new HttpClient(this, disposeHandler: false);

        // El validador de emisor de Microsoft.Identity.Web consulta metadatos por la red. Sin él, el manejador
        // real exige que el emisor sea exactamente el del documento de metadatos, que acá es el del tenant.
        opciones.TokenValidationParameters.IssuerValidator = null;
    }

    // Lo que haría Microsoft después de que la persona se autentica: guarda los datos y devuelve un código de un solo uso.
    public string EmitirCodigo(string nonce, string codeChallenge, IReadOnlyDictionary<string, object> claims, Action<AjustesDelToken>? ajustar = null)
    {
        var ajustes = new AjustesDelToken { Nonce = nonce };
        ajustar?.Invoke(ajustes);

        string codigo = Guid.NewGuid().ToString("N");
        _codigosEmitidos[codigo] = new CodigoEmitido(codeChallenge, CrearIdToken(claims, ajustes));
        return codigo;
    }

    // Lo que armaría un atacante: un token con todos los datos correctos, sin firma o firmado con una clave propia.
    public string CrearIdTokenFalsificado(string nonce, IReadOnlyDictionary<string, object> claims, bool sinFirma) =>
        CrearIdToken(claims, new AjustesDelToken { Nonce = nonce, SinFirma = sinFirma, FirmadoConOtraClave = !sinFirma });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken cancellationToken)
    {
        if (RespuestaForzadaDelEndpointDeTokens is { } respuestaForzada)
        {
            return respuestaForzada();
        }

        if (pedido.RequestUri?.AbsolutePath.EndsWith("/oauth2/v2.0/token", StringComparison.Ordinal) != true)
        {
            throw new InvalidOperationException($"La app intentó llamar a {pedido.RequestUri?.Host}: estas pruebas no deben salir a la red.");
        }

        var datos = QueryHelpers.ParseQuery(await pedido.Content!.ReadAsStringAsync(cancellationToken));

        if (!datos.TryGetValue("code", out var codigo) || !_codigosEmitidos.TryRemove(codigo.ToString(), out CodigoEmitido? emitido))
        {
            return CrearRespuestaDeError(HttpStatusCode.BadRequest, "invalid_grant");
        }

        bool canjeValido =
            datos["grant_type"] == "authorization_code"
            && datos["client_id"] == _clientId
            && datos["redirect_uri"] == _redirectUri
            && datos.TryGetValue("code_verifier", out var verificador)
            && CalcularDesafioPkce(verificador.ToString()) == emitido.CodeChallenge;

        if (!canjeValido)
        {
            return CrearRespuestaDeError(HttpStatusCode.BadRequest, "invalid_grant");
        }

        CanjesConPkceVerificado++;

        return CrearRespuestaJson(HttpStatusCode.OK, new { id_token = emitido.IdToken, access_token = "token-de-acceso-simulado", token_type = "Bearer", expires_in = 3600 });
    }

    public static HttpResponseMessage CrearRespuestaDeError(HttpStatusCode estado, string error) =>
        CrearRespuestaJson(estado, new { error, error_description = "Detalle del error con datos de usuario@eop.com.uy" });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _claveDeFirma.Dispose();
            _claveAjena.Dispose();
        }
    }

    private string CrearIdToken(IReadOnlyDictionary<string, object> claims, AjustesDelToken ajustes)
    {
        var contenido = new Dictionary<string, object>(claims)
        {
            ["nonce"] = ajustes.Nonce,
            ["ver"] = "2.0"
        };

        DateTime ahora = DateTime.UtcNow;
        DateTime vencimiento = ajustes.Vencido ? ahora.AddHours(-1) : ahora.AddHours(1);
        DateTime inicio = ajustes.Vencido ? ahora.AddHours(-2) : ahora.AddMinutes(-1);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = ajustes.Emisor ?? Emisor,
            Audience = ajustes.Audiencia ?? _clientId,
            Claims = contenido,
            IssuedAt = inicio,
            NotBefore = inicio,
            Expires = vencimiento,
            SigningCredentials = ajustes.SinFirma
                ? null
                : new SigningCredentials(CrearClave(ajustes.FirmadoConOtraClave ? _claveAjena : _claveDeFirma), SecurityAlgorithms.RsaSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string CalcularDesafioPkce(string verificador) =>
        Base64UrlTextEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verificador)));

    // Siempre el mismo kid: una firma con otra clave tiene que fallar por la firma, no porque no encuentre la clave.
    private static RsaSecurityKey CrearClave(RSA rsa) => new(rsa) { KeyId = "clave-de-prueba" };

    private static HttpResponseMessage CrearRespuestaJson(HttpStatusCode estado, object contenido) =>
        new(estado)
        {
            Content = new StringContent(JsonSerializer.Serialize(contenido), Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
        };

    private sealed record CodigoEmitido(string CodeChallenge, string IdToken);

    public sealed class AjustesDelToken
    {
        public required string Nonce { get; set; }
        public string? Emisor { get; set; }
        public string? Audiencia { get; set; }
        public bool Vencido { get; set; }
        public bool FirmadoConOtraClave { get; set; }
        public bool SinFirma { get; set; }
    }
}
