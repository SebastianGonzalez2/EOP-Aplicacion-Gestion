using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EOP.Gestion.Tests.Integracion;

// A diferencia de las pruebas unitarias, acá levantamos la app ENTERA en memoria
// (con Program.cs, los middlewares y las rutas) y le hacemos pedidos HTTP de verdad.
// Así comprobamos que las piezas funcionan juntas: por ejemplo, que la política
// "todo cerrado por defecto" realmente esté activa.
//
// Para no depender de Microsoft (internet, una cuenta real, el MFA), reemplazamos
// solo el login por un esquema de prueba. Todo lo demás es la configuración real.
public class SesionIntegracionTests(SesionIntegracionTests.FabricaDePrueba fabrica)
    : IClassFixture<SesionIntegracionTests.FabricaDePrueba>
{
    [Fact]
    public void Configuracion_ExigeUsuarioAutenticadoPorDefecto()
    {
        var opciones = fabrica.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(opciones.FallbackPolicy);
        Assert.Contains(
            opciones.FallbackPolicy.Requirements,
            requisito => requisito is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void Configuracion_CookieDeSesionSoloPorHttpsYSinAccesoDesdeJavaScript()
    {
        var opciones = fabrica.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.True(opciones.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, opciones.Cookie.SecurePolicy);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Privacy")]
    public async Task PantallaProtegida_SinSesion_NoSeMuestra(string ruta)
    {
        var respuesta = await CrearCliente().GetAsync(ruta);

        // 401 es lo que responde el esquema de prueba cuando "pide login".
        // Con la configuración real, en su lugar habría una redirección a Microsoft.
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/Cuenta/SesionCerrada")]
    [InlineData("/Cuenta/ErrorInicioSesion")]
    [InlineData("/Home/Error")]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css")]
    public async Task PantallaPublica_SinSesion_SeMuestra(string ruta)
    {
        var respuesta = await CrearCliente().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Inicio_ConSesion_MuestraNombreYFormularioSeguroDeCierre()
    {
        var respuesta = await CrearCliente(conSesion: true).GetAsync("/");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(EsquemaDePrueba.NombreUsuario, html);
        Assert.Contains("action=\"/Cuenta/CerrarSesion\"", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task CerrarSesion_PorGet_NoSeAcepta()
    {
        var respuesta = await CrearCliente(conSesion: true).GetAsync("/Cuenta/CerrarSesion");

        // Como CerrarSesion solo tiene [HttpPost], para un GET esa acción
        // "no existe" y MVC responde 404. Lo importante: no cierra la sesión.
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task CerrarSesion_SinTokenAntifalsificacion_SeRechaza()
    {
        var respuesta = await CrearCliente(conSesion: true)
            .PostAsync("/Cuenta/CerrarSesion", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    private HttpClient CrearCliente(bool conSesion = false)
    {
        // AllowAutoRedirect = false: queremos ver la respuesta tal cual,
        // sin que el cliente siga las redirecciones solo.
        var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        if (conSesion)
        {
            cliente.DefaultRequestHeaders.Add(EsquemaDePrueba.Encabezado, "si");
        }

        return cliente;
    }

    // Arma la app para las pruebas. Es igual a la real salvo dos cosas:
    // - una cadena de conexión ficticia (la app no se conecta a la base al arrancar);
    // - el login de Microsoft se reemplaza por el esquema de prueba.
    public sealed class FabricaDePrueba : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:EopDb",
                "Server=(localdb)\\pruebas;Database=NoSeUsa;Trusted_Connection=True");

            builder.ConfigureTestServices(servicios =>
            {
                servicios.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, EsquemaDePrueba>(EsquemaDePrueba.Nombre, null);

                servicios.PostConfigure<AuthenticationOptions>(opciones =>
                {
                    opciones.DefaultScheme = EsquemaDePrueba.Nombre;
                    opciones.DefaultAuthenticateScheme = EsquemaDePrueba.Nombre;
                    opciones.DefaultChallengeScheme = EsquemaDePrueba.Nombre;
                });
            });
        }
    }

    // Un "login de mentira": si el pedido trae el encabezado de prueba, lo da
    // por autenticado; si no, lo trata como anónimo. Cuando hay que pedir login,
    // responde 401 en vez de mandar a Microsoft.
    private sealed class EsquemaDePrueba(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador)
        : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, codificador)
    {
        public const string Nombre = "Prueba";
        public const string Encabezado = "X-Sesion-Prueba";
        public const string NombreUsuario = "Usuario de Prueba";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(Encabezado))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identidad = new ClaimsIdentity([new Claim("name", NombreUsuario)], Nombre);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), Nombre);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
