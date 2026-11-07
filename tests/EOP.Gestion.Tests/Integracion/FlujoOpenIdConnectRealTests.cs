using System.Net;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Autorizacion;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using static EOP.Gestion.Tests.Compartido.FabricaConEntraSimulado;

namespace EOP.Gestion.Tests.Integracion;

// S1-T30 — Autenticación con el manejador REAL de OpenID Connect y tokens JWT reales, firmados por un Entra
// simulado. Las demás pruebas de sesión reemplazan al manejador y arman los claims a mano, así que no pueden
// detectar errores en PKCE, en la validación del token, en el mapeo de claims ni en el tipo de claim de rol.
//
// Qué NO prueba (US-042, "No completar MFA"; RF-71; RNF-3): que Microsoft exija el segundo factor. Acá solo se
// comprueba qué hace EOP con lo que Microsoft le responde. La exigencia real se verifica a mano en el tenant.
public sealed class FlujoOpenIdConnectRealTests : IDisposable
{
    private const string ObjectIdJuan = "11111111-1111-1111-1111-111111111111";
    private const string ObjectIdOtro = "22222222-2222-2222-2222-222222222222";
    private const string CorreoJuan = "juan.perez@eop.com.uy";
    private const string NombreEnEntra = "Juan Perez en Entra";
    private const string RutaPantallaError = ManejadorFallosInicioSesion.RutaPantallaError;
    private const string CategoriaFallosDeLogin = "EOP.Gestion.WebApp.Autorizacion.ManejadorFallosInicioSesion";

    private readonly FabricaConEntraSimulado _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private static Dictionary<string, object> ClaimsDeEntra(string? oid = ObjectIdJuan, string? correo = CorreoJuan)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = "sujeto-de-prueba",
            ["tid"] = "157a4c11-5aee-40d6-ae99-583f426daf4e",
            ["name"] = NombreEnEntra
        };

        if (oid != null)
        {
            claims["oid"] = oid;
        }

        if (correo != null)
        {
            claims["preferred_username"] = correo;
        }

        return claims;
    }

    private static void AssertLoginRechazado(HttpResponseMessage respuesta, Navegador navegador)
    {
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RutaPantallaError, respuesta.Headers.Location?.OriginalString);
        Assert.False(navegador.TieneSesion, "La app creó una sesión en un inicio de sesión que debía rechazar.");
    }

    private async Task AssertNoEntraAPantallasProtegidasAsync(Navegador navegador)
    {
        HttpResponseMessage respuesta = await navegador.GetAsync("/Home/Privacy");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith(_fabrica.Entra.BaseDelTenant, respuesta.Headers.Location?.ToString());
    }

    // ---------- La salida hacia Microsoft ----------

    [Fact]
    public async Task PantallaProtegidaSinSesion_MandaAEntraIdConFlujoDeCodigoYPkce()
    {
        HttpResponseMessage respuesta = await _fabrica.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        }).GetAsync("/Home/Privacy");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        Uri destino = respuesta.Headers.Location!;
        Assert.Equal("login.microsoftonline.com", destino.Host);

        // El tenant de EOP y no "common" u "organizations": solo se aceptan cuentas de este directorio.
        Assert.Equal("/157a4c11-5aee-40d6-ae99-583f426daf4e/oauth2/v2.0/authorize", destino.AbsolutePath);

        var parametros = QueryHelpers.ParseQuery(destino.Query);
        Assert.Equal("code", parametros["response_type"]);
        Assert.Equal("S256", parametros["code_challenge_method"]);
        Assert.NotEmpty(parametros["code_challenge"].ToString());
        Assert.NotEmpty(parametros["state"].ToString());
        Assert.NotEmpty(parametros["nonce"].ToString());
        Assert.Equal("https://localhost/signin-oidc", parametros["redirect_uri"]);
        Assert.Contains("openid", parametros["scope"].ToString());

        // El secreto de la aplicación nunca puede viajar en una URL que pasa por el navegador.
        Assert.False(parametros.ContainsKey("client_secret"));
        Assert.DoesNotContain("secreto-ficticio", destino.ToString());
    }

    [Fact]
    public async Task PedidoPorHttp_SeRedirigeAHttps()
    {
        // RNF-9. El cliente de prueba de las demás pruebas ya usa https, así que nunca pasaría por acá.
        HttpResponseMessage respuesta = await _fabrica.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost")
        }).GetAsync("/Home/Privacy");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, respuesta.StatusCode);
        Assert.Equal("https://localhost/Home/Privacy", respuesta.Headers.Location?.ToString());
    }

    // ---------- Inicio de sesión con un token real ----------

    [Fact]
    public async Task UsuarioVinculadoYActivo_CompletaElFlujoYLaCookieSirveEnLosPedidosSiguientes()
    {
        await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan);
        Navegador navegador = _fabrica.CrearNavegador();

        HttpResponseMessage login = await navegador.IniciarSesionAsync(ClaimsDeEntra());

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location?.OriginalString);
        Assert.True(navegador.TieneSesion);
        Assert.Equal(1, _fabrica.Entra.CanjesConPkceVerificado);

        HttpResponseMessage pantalla = await navegador.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, pantalla.StatusCode);
        Assert.Contains(NombreEnEntra, await pantalla.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CookieDeSesionEmitida_LlevaLosAtributosDeSeguridadEnLaRespuestaReal()
    {
        await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan);

        HttpResponseMessage login = await _fabrica.CrearNavegador().IniciarSesionAsync(ClaimsDeEntra());

        string cookie = login.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(".AspNetCore.Cookies", StringComparison.Ordinal));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(RolUsuario.Administrador, true, true)]
    [InlineData(RolUsuario.Supervisor, false, true)]
    [InlineData(RolUsuario.Operador, false, false)]
    public async Task RolEfectivo_EsElDeEop_AunqueElTokenDeEntraTraigaOtroRol(RolUsuario rolEnEop, bool administraUsuarios, bool registraMateriales)
    {
        // El token real llega con el claim de roles que usa Entra ID, con un valor que le daría todos los permisos.
        await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan, rolEnEop);
        Dictionary<string, object> claims = ClaimsDeEntra();
        claims["roles"] = new[] { "Administrador" };
        claims["role"] = "Administrador";
        Navegador navegador = _fabrica.CrearNavegador();
        await navegador.IniciarSesionAsync(claims);

        await AssertPermisoAsync(navegador, "/Usuarios/Registrar", administraUsuarios);
        await AssertPermisoAsync(navegador, "/Materiales/Registrar", registraMateriales);
    }

    private static async Task AssertPermisoAsync(Navegador navegador, string ruta, bool permitido)
    {
        HttpResponseMessage respuesta = await navegador.GetAsync(ruta);

        if (permitido)
        {
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        }
    }

    // ---------- Vinculación ----------

    [Fact]
    public async Task PrimerAcceso_VinculaConElOidDelTokenYLosAccesosPosterioresUsanEsaVinculacion()
    {
        int idJuan = await _fabrica.GuardarUsuarioAsync(CorreoJuan, entraObjectId: null);

        await _fabrica.CrearNavegador().IniciarSesionAsync(ClaimsDeEntra());
        Assert.Equal(ObjectIdJuan, (await _fabrica.LeerUsuarioAsync(idJuan)).EntraObjectId);

        // RF-69 / RF-76: se le actualiza el correo en EOP y Microsoft sigue informando el anterior.
        await _fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.CorreoCorporativo = "juan.nuevo@eop.com.uy");
        Navegador segundoAcceso = _fabrica.CrearNavegador();

        await segundoAcceso.IniciarSesionAsync(ClaimsDeEntra());

        Assert.True(segundoAcceso.TieneSesion);
        Usuario juan = await _fabrica.LeerUsuarioAsync(idJuan);
        Assert.Equal(ObjectIdJuan, juan.EntraObjectId);
        Assert.Equal("juan.nuevo@eop.com.uy", juan.CorreoCorporativo);
    }

    [Fact]
    public async Task PrimerAcceso_SinPreferredUsername_VinculaConElClaimDeCorreoDelToken()
    {
        int idJuan = await _fabrica.GuardarUsuarioAsync(CorreoJuan, entraObjectId: null);
        Dictionary<string, object> claims = ClaimsDeEntra(correo: null);
        claims["email"] = CorreoJuan;
        Navegador navegador = _fabrica.CrearNavegador();

        await navegador.IniciarSesionAsync(claims);

        Assert.True(navegador.TieneSesion);
        Assert.Equal(ObjectIdJuan, (await _fabrica.LeerUsuarioAsync(idJuan)).EntraObjectId);
    }

    // ---------- Identidades que Microsoft autenticó, pero EOP no habilita ----------

    [Theory]
    [InlineData("no registrada")]
    [InlineData("inactivo vinculado")]
    [InlineData("inactivo sin vincular")]
    [InlineData("correo ya vinculado a otra identidad")]
    [InlineData("token sin oid")]
    [InlineData("primer acceso sin correo")]
    public async Task IdentidadNoHabilitadaEnEop_ConTokenValido_NoObtieneSesion(string situacion)
    {
        Dictionary<string, object> claims = ClaimsDeEntra();
        int? idUsuario = null;

        switch (situacion)
        {
            case "no registrada":
                break;
            case "inactivo vinculado":
                idUsuario = await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan, activo: false);
                break;
            case "inactivo sin vincular":
                idUsuario = await _fabrica.GuardarUsuarioAsync(CorreoJuan, entraObjectId: null, activo: false);
                break;
            case "correo ya vinculado a otra identidad":
                idUsuario = await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdOtro);
                break;
            case "token sin oid":
                idUsuario = await _fabrica.GuardarUsuarioAsync(CorreoJuan, entraObjectId: null);
                claims.Remove("oid");
                break;
            case "primer acceso sin correo":
                idUsuario = await _fabrica.GuardarUsuarioAsync(CorreoJuan, entraObjectId: null);
                claims.Remove("preferred_username");
                break;
        }

        string? objectIdOriginal = idUsuario is { } id ? (await _fabrica.LeerUsuarioAsync(id)).EntraObjectId : null;
        Navegador navegador = _fabrica.CrearNavegador();

        HttpResponseMessage respuesta = await navegador.IniciarSesionAsync(claims);

        AssertLoginRechazado(respuesta, navegador);
        await AssertNoEntraAPantallasProtegidasAsync(navegador);

        if (idUsuario is { } idGuardado)
        {
            Assert.Equal(objectIdOriginal, (await _fabrica.LeerUsuarioAsync(idGuardado)).EntraObjectId);
        }
    }

    // ---------- Tokens que no se pueden aceptar ----------

    [Theory]
    [InlineData("firmado con otra clave")]
    [InlineData("audiencia de otra aplicación")]
    [InlineData("emisor de otro tenant")]
    [InlineData("vencido")]
    [InlineData("nonce de otro inicio de sesión")]
    public async Task TokenInvalido_NoCreaSesionNiVincula_YTerminaEnLaPantallaDeError(string defecto)
    {
        int idJuan = await _fabrica.GuardarUsuarioAsync(CorreoJuan, entraObjectId: null, RolUsuario.Administrador);
        Navegador navegador = _fabrica.CrearNavegador();

        HttpResponseMessage respuesta = await navegador.IniciarSesionAsync(ClaimsDeEntra(), ajustes =>
        {
            switch (defecto)
            {
                case "firmado con otra clave":
                    ajustes.FirmadoConOtraClave = true;
                    break;
                case "audiencia de otra aplicación":
                    ajustes.Audiencia = "99999999-0000-0000-0000-000000000000";
                    break;
                case "emisor de otro tenant":
                    ajustes.Emisor = "https://login.microsoftonline.com/99999999-0000-0000-0000-000000000000/v2.0";
                    break;
                case "vencido":
                    ajustes.Vencido = true;
                    break;
                case "nonce de otro inicio de sesión":
                    ajustes.Nonce = "nonce-robado";
                    break;
            }
        });

        AssertLoginRechazado(respuesta, navegador);
        Assert.Null((await _fabrica.LeerUsuarioAsync(idJuan)).EntraObjectId);
    }

    // Un token sin firma que llega en el canje del código se acepta: lo permite OpenID Connect Core 3.1.3.7 porque
    // viene directo del endpoint de Microsoft, por TLS y sin pasar por el navegador. Por eso el ataque a cubrir es
    // este otro: que alguien le mande el token a /signin-oidc desde el navegador, sin pasar por Microsoft.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdTokenFalsificadoEnviadoDesdeElNavegador_SeRechazaAunqueElEstadoSeaValido(bool sinFirma)
    {
        int idJuan = await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan, RolUsuario.Administrador);
        Navegador navegador = _fabrica.CrearNavegador();
        var parametros = await navegador.PedirLoginAsync();

        string tokenFalso = _fabrica.Entra.CrearIdTokenFalsificado(parametros["nonce"], ClaimsDeEntra(), sinFirma);

        HttpResponseMessage respuesta = await navegador.PostAsync("/signin-oidc", new Dictionary<string, string>
        {
            ["id_token"] = tokenFalso,
            ["state"] = parametros["state"]
        });

        AssertLoginRechazado(respuesta, navegador);
        Assert.Equal(ObjectIdJuan, (await _fabrica.LeerUsuarioAsync(idJuan)).EntraObjectId);
    }

    // ---------- Fallas informadas por Microsoft ----------

    [Theory]
    [InlineData("interaction_required")] // Falta completar el segundo factor.
    [InlineData("access_denied")]        // Canceló o le denegaron el acceso.
    [InlineData("consent_required")]
    public async Task EntraIdVuelveConError_NoCreaSesion_YElLogDeEopSoloGuardaElCodigo(string codigoDeError)
    {
        await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan);
        Navegador navegador = _fabrica.CrearNavegador();
        var parametros = await navegador.PedirLoginAsync();

        HttpResponseMessage respuesta = await navegador.PostAsync("/signin-oidc", new Dictionary<string, string>
        {
            ["state"] = parametros["state"],
            ["error"] = codigoDeError,
            ["error_description"] = "AADSTS50076: se requiere MFA para juan.perez@eop.com.uy"
        });

        AssertLoginRechazado(respuesta, navegador);

        EntradaDeLog entrada = Assert.Single(_fabrica.Logs.Entradas, e => e.Categoria == CategoriaFallosDeLogin);
        Assert.Equal(LogLevel.Warning, entrada.Nivel);
        Assert.Contains(codigoDeError, entrada.Mensaje);
        Assert.DoesNotContain("AADSTS50076", entrada.Mensaje);
        Assert.DoesNotContain("@", entrada.Mensaje);
        Assert.Null(entrada.Excepcion);
    }

    [Theory]
    [InlineData("estado alterado")]
    [InlineData("sin la cookie de correlación")]
    public async Task RespuestaQueNoCorrespondeAUnLoginIniciadoDesdeEsteNavegador_SeRechaza(string defecto)
    {
        // Es la defensa frente a que alguien le haga iniciar sesión a otra persona con una cuenta ajena (login CSRF).
        await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan);
        Navegador navegador = _fabrica.CrearNavegador();
        var parametros = await navegador.PedirLoginAsync();
        string codigo = _fabrica.Entra.EmitirCodigo(parametros["nonce"], parametros["code_challenge"], ClaimsDeEntra());

        Navegador navegadorDeLaRespuesta = defecto == "sin la cookie de correlación" ? _fabrica.CrearNavegador() : navegador;
        string estado = defecto == "estado alterado" ? "estado-inventado" : parametros["state"];

        HttpResponseMessage respuesta = await navegadorDeLaRespuesta.PostAsync("/signin-oidc", new Dictionary<string, string>
        {
            ["code"] = codigo,
            ["state"] = estado
        });

        AssertLoginRechazado(respuesta, navegadorDeLaRespuesta);
        Assert.Equal(0, _fabrica.Entra.CanjesConPkceVerificado);

        // No lo informó Microsoft: el log deja constancia, pero no hay un código de error de Entra para registrar.
        Assert.Contains("sin código", Assert.Single(_fabrica.Logs.Entradas, e => e.Categoria == CategoriaFallosDeLogin).Mensaje);
    }

    [Theory]
    [InlineData("rechaza el código")]
    [InlineData("error interno")]
    [InlineData("no responde")]
    public async Task EndpointDeTokensFalla_NoCreaSesionNiEscapaUnError500(string falla)
    {
        // RNF-13: si Microsoft falla, la app no se rompe e informa al usuario.
        await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan);
        _fabrica.Entra.RespuestaForzadaDelEndpointDeTokens = falla switch
        {
            "rechaza el código" => () => ProveedorEntraSimulado.CrearRespuestaDeError(HttpStatusCode.BadRequest, "invalid_grant"),
            "error interno" => () => ProveedorEntraSimulado.CrearRespuestaDeError(HttpStatusCode.InternalServerError, "server_error"),
            _ => () => throw new HttpRequestException("login.microsoftonline.com no responde")
        };
        Navegador navegador = _fabrica.CrearNavegador();

        HttpResponseMessage respuesta = await navegador.IniciarSesionAsync(ClaimsDeEntra());

        AssertLoginRechazado(respuesta, navegador);
    }

    // ---------- Cierre de sesión ----------

    [Fact]
    public async Task CerrarSesion_BorraLaCookieVaAEntraIdYVuelveALaPantallaDeConfirmacion()
    {
        await _fabrica.GuardarUsuarioAsync(CorreoJuan, ObjectIdJuan);
        Navegador navegador = _fabrica.CrearNavegador();
        await navegador.IniciarSesionAsync(ClaimsDeEntra());
        string token = await navegador.ObtenerTokenAntifalsificacionAsync();

        HttpResponseMessage cierre = await navegador.PostAsync("/Cuenta/CerrarSesion", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        });

        // Se borró la sesión de EOP y se le pide a Microsoft que cierre la suya.
        Assert.False(navegador.TieneSesion, "Cerrar sesión no borró la cookie de EOP.");
        Assert.Equal(HttpStatusCode.Redirect, cierre.StatusCode);
        Uri destino = cierre.Headers.Location!;
        Assert.Equal("login.microsoftonline.com", destino.Host);
        Assert.EndsWith("/oauth2/v2.0/logout", destino.AbsolutePath);

        var parametros = QueryHelpers.ParseQuery(destino.Query);
        Assert.Equal("https://localhost/signout-callback-oidc", parametros["post_logout_redirect_uri"]);

        // Microsoft vuelve a la app, y la app lleva a la pantalla de confirmación.
        HttpResponseMessage regreso = await navegador.GetAsync($"/signout-callback-oidc?state={Uri.EscapeDataString(parametros["state"].ToString())}");
        Assert.Equal(HttpStatusCode.Redirect, regreso.StatusCode);
        Assert.Equal("/Cuenta/SesionCerrada", regreso.Headers.Location?.OriginalString);

        HttpResponseMessage confirmacion = await navegador.GetAsync("/Cuenta/SesionCerrada");
        Assert.Equal(HttpStatusCode.OK, confirmacion.StatusCode);
        Assert.DoesNotContain(NombreEnEntra, await confirmacion.Content.ReadAsStringAsync());

        await AssertNoEntraAPantallasProtegidasAsync(navegador);
    }

    [Fact]
    public async Task CerrarSesion_SinSesion_MandaAIniciarSesionEnVezDeRomperse()
    {
        Navegador navegador = _fabrica.CrearNavegador();

        HttpResponseMessage respuesta = await navegador.PostAsync("/Cuenta/CerrarSesion", new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith(_fabrica.Entra.BaseDelTenant, respuesta.Headers.Location?.ToString());
    }
}
