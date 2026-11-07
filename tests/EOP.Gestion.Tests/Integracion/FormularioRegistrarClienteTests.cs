using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EOP.Gestion.Tests.Integracion;

// Igual que en SesionIntegracionTests, levantamos la app entera en memoria y le hablamos
// por HTTP, como lo haría el navegador. Solo reemplazamos el login y la base de datos.
// Así probamos de punta a punta lo que no se ve en una prueba unitaria: que la vista
// muestre el país elegido y que un POST armado a mano tampoco pueda saltarse las validaciones.
public class FormularioRegistrarClienteTests : IClassFixture<FormularioRegistrarClienteTests.FabricaDePrueba>
{
    private const string Ruta = "/Clientes/Registrar";

    private readonly FabricaDePrueba _fabrica;

    public FormularioRegistrarClienteTests(FabricaDePrueba fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Repositorio.ClienteGuardado = null;
        _fabrica.Repositorio.RutDisponibleAlGuardar = true;
    }

    // Otra solicitud registró el mismo RUT entre la validación y el guardado: el usuario tiene
    // que ver el formulario con el motivo, no la página de error genérica.
    [Fact]
    public async Task Post_SiElRutSeRegistroMientrasTanto_MuestraElMotivoEnElFormulario()
    {
        _fabrica.Repositorio.RutDisponibleAlGuardar = false;

        HttpResponseMessage respuesta = await EnviarFormularioAsync(CrearCliente(), DatosValidos());
        string html = await LeerHtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(_fabrica.Repositorio.ClienteGuardado);
        Assert.Contains("Ya existe un cliente registrado con ese RUT.", html);
        Assert.DoesNotContain("El cliente se registró correctamente.", html);
    }

    [Fact]
    public async Task Formulario_AlAbrir_MuestraUruguayElegidoYListaInternacional()
    {
        string html = await LeerHtmlAsync(await CrearCliente().GetAsync(Ruta));

        Assert.Equal("UY", ObtenerPaisElegido(html));
        Assert.True(Regex.Count(html, "<option") > 200);
        Assert.Contains(">Uruguay (+598)</option>", html);
        Assert.Contains(">Argentina (+54)</option>", html);
        Assert.Contains(">Brasil (+55)</option>", html);
        Assert.Contains(">España (+34)</option>", html);
    }

    [Fact]
    public async Task Post_Valido_GuardaElNumeroInternacionalYVuelveAlFormularioVacio()
    {
        HttpClient cliente = CrearCliente();

        HttpResponseMessage respuesta = await EnviarFormularioAsync(cliente, DatosValidos());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.NotNull(_fabrica.Repositorio.ClienteGuardado);
        Assert.Equal("+59899123456", _fabrica.Repositorio.ClienteGuardado.NumeroWhatsApp);

        // Se mantiene el comportamiento de antes: mensaje de éxito y listo para registrar otro.
        string html = await LeerHtmlAsync(await cliente.GetAsync(respuesta.Headers.Location));

        Assert.Contains("El cliente se registró correctamente.", html);
        Assert.Equal("UY", ObtenerPaisElegido(html));
        Assert.Equal(string.Empty, ObtenerNumeroEscrito(html));
    }

    [Fact]
    public async Task Post_NumeroInvalido_ConservaPaisYNumeroYNoGuarda()
    {
        Dictionary<string, string> datos = DatosValidos();
        datos["PaisWhatsApp"] = "AR";
        datos["NumeroNacionalWhatsApp"] = "011 123";

        HttpResponseMessage respuesta = await EnviarFormularioAsync(CrearCliente(), datos);
        string html = await LeerHtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(_fabrica.Repositorio.ClienteGuardado);
        Assert.Contains("no es válido para Argentina (+54)", html);
        Assert.Equal("AR", ObtenerPaisElegido(html));
        Assert.Equal("011 123", ObtenerNumeroEscrito(html));
        Assert.True(Regex.Count(html, "<option") > 200);
    }

    [Fact]
    public async Task Post_OtroDatoInvalido_ConservaPaisYNumeroYNoGuarda()
    {
        Dictionary<string, string> datos = DatosValidos();
        datos["Rut"] = "215521750018";
        datos["PaisWhatsApp"] = "ES";
        datos["NumeroNacionalWhatsApp"] = "612 34 56 78";

        HttpResponseMessage respuesta = await EnviarFormularioAsync(CrearCliente(), datos);
        string html = await LeerHtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(_fabrica.Repositorio.ClienteGuardado);
        Assert.Contains("El RUT ingresado no es válido.", html);
        Assert.Equal("ES", ObtenerPaisElegido(html));
        Assert.Equal("612 34 56 78", ObtenerNumeroEscrito(html));
    }

    [Fact]
    public async Task Post_NumeroVacio_MuestraElErrorYConservaElPais()
    {
        Dictionary<string, string> datos = DatosValidos();
        datos["PaisWhatsApp"] = "BR";
        datos["NumeroNacionalWhatsApp"] = "";

        HttpResponseMessage respuesta = await EnviarFormularioAsync(CrearCliente(), datos);
        string html = await LeerHtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(_fabrica.Repositorio.ClienteGuardado);
        Assert.Contains("Ingresá el número de WhatsApp.", html);
        Assert.Equal("BR", ObtenerPaisElegido(html));
    }

    [Theory]
    [InlineData("Nombre", "El nombre del cliente es obligatorio.")]
    [InlineData("Rut", "El RUT es obligatorio.")]
    public async Task Post_CampoObligatorioVacio_MuestraElMensajeEnEspanol(string campo, string mensaje)
    {
        Dictionary<string, string> datos = DatosValidos();
        datos[campo] = "";

        HttpResponseMessage respuesta = await EnviarFormularioAsync(CrearCliente(), datos);
        string html = await LeerHtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(_fabrica.Repositorio.ClienteGuardado);
        Assert.Contains($">{mensaje}</span>", html);
        Assert.DoesNotContain("field is required", html);
    }

    // Lo que pasaría si alguien edita el HTML del desplegable: no se guarda, y tampoco
    // lo cambiamos a Uruguay sin avisar, porque el usuario podría no darse cuenta.
    [Fact]
    public async Task Post_PaisManipulado_NoGuardaYConservaElNumero()
    {
        Dictionary<string, string> datos = DatosValidos();
        datos["PaisWhatsApp"] = "ZZ";

        HttpResponseMessage respuesta = await EnviarFormularioAsync(CrearCliente(), datos);
        string html = await LeerHtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(_fabrica.Repositorio.ClienteGuardado);
        Assert.Contains("Seleccioná un país válido", html);
        Assert.Null(ObtenerPaisElegido(html));
        Assert.Equal("099123456", ObtenerNumeroEscrito(html));
    }

    // Si el POST no manda el país, no tiene que asumirse Uruguay.
    [Fact]
    public async Task Post_SinPais_NoGuarda()
    {
        Dictionary<string, string> datos = DatosValidos();
        datos.Remove("PaisWhatsApp");

        HttpResponseMessage respuesta = await EnviarFormularioAsync(CrearCliente(), datos);
        string html = await LeerHtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(_fabrica.Repositorio.ClienteGuardado);
        Assert.Contains("Seleccioná el país del número de WhatsApp.", html);
    }

    // El campo viejo ya no existe en el formulario: mandarlo igual no
    // tiene que servir para meter un número sin pasar por el país.
    [Fact]
    public async Task Post_CampoViejoNumeroWhatsApp_SeIgnora()
    {
        Dictionary<string, string> datos = DatosValidos();
        datos.Remove("NumeroNacionalWhatsApp");
        datos["NumeroWhatsApp"] = "+59899123456";

        HttpResponseMessage respuesta = await EnviarFormularioAsync(CrearCliente(), datos);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(_fabrica.Repositorio.ClienteGuardado);
    }

    private static Dictionary<string, string> DatosValidos() => new()
    {
        ["Nombre"] = "Empresa de prueba",
        ["Rut"] = "215521750017",
        ["CorreoContacto"] = "cliente@ejemplo.com",
        ["PaisWhatsApp"] = "UY",
        ["NumeroNacionalWhatsApp"] = "099123456"
    };

    // Primero abrimos el formulario, como haría el navegador, para conseguir
    // el token antifalsificación. Sin él, el POST se rechaza antes de llegar al Controller.
    private static async Task<HttpResponseMessage> EnviarFormularioAsync(HttpClient cliente, Dictionary<string, string> datos)
    {
        string html = await LeerHtmlAsync(await cliente.GetAsync(Ruta));

        string token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var formulario = new Dictionary<string, string>(datos)
        {
            ["__RequestVerificationToken"] = token
        };

        return await cliente.PostAsync(Ruta, new FormUrlEncodedContent(formulario));
    }

    // Razor escribe "España (+34)" como "Espa&#xF1;a (&#x2B;34)". Para el navegador es
    // lo mismo, así que lo decodificamos para poder comparar el texto que ve el usuario.
    private static async Task<string> LeerHtmlAsync(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    private static string? ObtenerPaisElegido(string html)
    {
        Match elegido = Regex.Match(html, "<option selected=\"selected\" value=\"([^\"]*)\"");

        return elegido.Success ? elegido.Groups[1].Value : null;
    }

    private static string ObtenerNumeroEscrito(string html)
    {
        string input = Regex.Match(html, "<input[^>]*name=\"NumeroNacionalWhatsApp\"[^>]*>").Value;

        return Regex.Match(input, "value=\"([^\"]*)\"").Groups[1].Value;
    }

    private HttpClient CrearCliente()
    {
        HttpClient cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        return cliente;
    }

    public sealed class RepositorioClientesEnMemoria : IRepositorioClientes
    {
        public Cliente? ClienteGuardado { get; set; }

        // En false simula que otra solicitud registró el mismo RUT entre la validación y el guardado.
        public bool RutDisponibleAlGuardar { get; set; } = true;

        public Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> AgregarSiRutDisponibleAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            if (RutDisponibleAlGuardar)
            {
                ClienteGuardado = cliente;
            }

            return Task.FromResult(RutDisponibleAlGuardar);
        }
    }

    public sealed class FabricaDePrueba : WebApplicationFactory<Program>
    {
        public RepositorioClientesEnMemoria Repositorio { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:EopDb",
                "Server=(localdb)\\pruebas;Database=NoSeUsa;Trusted_Connection=True");

            builder.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton<IRepositorioClientes>(Repositorio);

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

    // Un "login de mentira" que da por autenticado todo pedido: los
    // permisos de esta pantalla no son lo que se está probando acá.
    private sealed class EsquemaDePrueba(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador)
        : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, codificador)
    {
        public const string Nombre = "Prueba";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identidad = new ClaimsIdentity([new Claim("name", "Usuario de Prueba")], Nombre);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), Nombre);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
