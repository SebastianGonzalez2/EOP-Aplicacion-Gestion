using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EOP.Gestion.Tests.Integracion;

// Levantamos la app entera en memoria y pedimos la pantalla por HTTP, como lo haría el navegador,
// para comprobar lo que sale de verdad de la vista. Las opciones del select están escritas a mano
// en la vista, así que nada más que estas pruebas avisa si una unidad queda afuera o mal rotulada.
public class FormularioRegistrarMaterialTests : IClassFixture<FormularioRegistrarMaterialTests.FabricaDePrueba>
{
    private const string Ruta = "/Materiales/Registrar";

    private readonly FabricaDePrueba _fabrica;

    public FormularioRegistrarMaterialTests(FabricaDePrueba fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Formulario_MuestraLasCatorceUnidadesConSuRotuloYSuNumero()
    {
        HttpClient cliente = CrearCliente();

        string html = await LeerHtmlAsync(await cliente.GetAsync(Ruta));

        var opcionesEsperadas = new List<(string Valor, string Texto)>
        {
            ("", "Seleccioná una unidad"),
            (((int)UnidadMedida.Milimetro).ToString(), "mm"),
            (((int)UnidadMedida.Centimetro).ToString(), "cm"),
            (((int)UnidadMedida.Metro).ToString(), "m"),
            (((int)UnidadMedida.Kilometro).ToString(), "km"),
            (((int)UnidadMedida.CentimetroCuadrado).ToString(), "cm²"),
            (((int)UnidadMedida.MetroCuadrado).ToString(), "m²"),
            (((int)UnidadMedida.CentimetroCubico).ToString(), "cm³"),
            (((int)UnidadMedida.MetroCubico).ToString(), "m³"),
            (((int)UnidadMedida.Mililitro).ToString(), "ml"),
            (((int)UnidadMedida.Litro).ToString(), "litro"),
            (((int)UnidadMedida.Gramo).ToString(), "g"),
            (((int)UnidadMedida.Kilogramo).ToString(), "kg"),
            (((int)UnidadMedida.Tonelada).ToString(), "t"),
            (((int)UnidadMedida.Unidad).ToString(), "unidad")
        };

        Assert.Equal(opcionesEsperadas, ObtenerOpcionesDeUnidad(html));
    }

    // Si mañana alguien agrega una unidad al enum y se olvida de la vista, esta prueba lo avisa.
    [Fact]
    public async Task Formulario_OfreceTodasLasUnidadesDelEnum()
    {
        HttpClient cliente = CrearCliente();

        string html = await LeerHtmlAsync(await cliente.GetAsync(Ruta));

        List<(string Valor, string Texto)> opciones = ObtenerOpcionesDeUnidad(html);

        foreach (UnidadMedida unidad in Enum.GetValues<UnidadMedida>())
        {
            string valor = ((int)unidad).ToString();

            Assert.Contains(opciones, opcion => opcion.Valor == valor);
        }
    }

    // Mandamos el formulario sin nombre para que vuelva con errores: así comprobamos que el
    // servidor acepta la unidad y que el select la conserva elegida, aun dentro de un grupo.
    [Fact]
    public async Task Enviar_ConUnidadNueva_LaAceptaYLaConservaSeleccionada()
    {
        HttpClient cliente = CrearCliente();

        string html = await EnviarConUnidadAsync(cliente, ((int)UnidadMedida.Unidad).ToString());

        Match opcionElegida = Regex.Match(html, "<option[^>]*value=\"14\"[^>]*>");

        Assert.True(opcionElegida.Success, "No se encontró la opción de la unidad.");
        Assert.Contains("selected", opcionElegida.Value);
        Assert.DoesNotMatch(PatronDeErrorEnUnidad, html);
    }

    // El navegador solo ofrece las opciones de la lista, pero cualquiera puede mandar otro
    // número a mano. Por eso esto se tiene que frenar en el servidor (RNF-10 / RNF-18).
    [Theory]
    [InlineData("0")]
    [InlineData("15")]
    [InlineData("99")]
    public async Task Enviar_ConUnidadFueraDeLaLista_MuestraElError(string unidad)
    {
        HttpClient cliente = CrearCliente();

        string html = await EnviarConUnidadAsync(cliente, unidad);

        Assert.Matches(PatronDeErrorEnUnidad, html);
    }

    private const string PatronDeErrorEnUnidad = "<span[^>]*field-validation-error[^>]*data-valmsg-for=\"Unidad\"";

    private static async Task<string> EnviarConUnidadAsync(HttpClient cliente, string unidad)
    {
        string htmlDelFormulario = await LeerHtmlAsync(await cliente.GetAsync(Ruta));

        string token = Regex.Match(
            htmlDelFormulario,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var datos = new Dictionary<string, string>
        {
            ["Nombre"] = "",
            ["Unidad"] = unidad,
            ["ProveedorId"] = "1",
            ["PrecioUnitario"] = "150",
            ["Moneda"] = "UYU",
            ["__RequestVerificationToken"] = token
        };

        HttpResponseMessage respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(datos));

        return await LeerHtmlAsync(respuesta);
    }

    private static async Task<string> LeerHtmlAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        return await respuesta.Content.ReadAsStringAsync();
    }

    private static List<(string Valor, string Texto)> ObtenerOpcionesDeUnidad(string html)
    {
        Match selector = Regex.Match(html, "<select[^>]*id=\"Unidad\".*?</select>", RegexOptions.Singleline);
        Assert.True(selector.Success, "No se encontró el select de unidades.");

        var opciones = new List<(string Valor, string Texto)>();

        foreach (Match opcion in Regex.Matches(selector.Value, "<option[^>]*value=\"([^\"]*)\"[^>]*>([^<]*)</option>"))
        {
            opciones.Add((opcion.Groups[1].Value, WebUtility.HtmlDecode(opcion.Groups[2].Value)));
        }

        return opciones;
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

    public sealed class FabricaDePrueba : WebApplicationFactory<Program>
    {
        // Mientras la conexión siga abierta, la base en memoria existe. El formulario la
        // necesita porque consulta los proveedores para armar su desplegable.
        private readonly SqliteConnection _conexion = new("Data Source=:memory:");

        public FabricaDePrueba()
        {
            _conexion.Open();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(_conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            contexto.Database.EnsureCreated();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Program.cs exige una cadena de conexión al arrancar; no se usa
            // porque abajo reemplazamos SQL Server por SQLite.
            builder.UseSetting(
                "ConnectionStrings:EopDb",
                "Server=(localdb)\\pruebas;Database=NoSeUsa;Trusted_Connection=True");

            builder.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<DbContextOptions<EopDbContext>>();
                servicios.RemoveAll<IDbContextOptionsConfiguration<EopDbContext>>();
                servicios.AddDbContext<EopDbContext>(opciones => opciones.UseSqlite(_conexion));

                servicios.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, EsquemaDePrueba>(EsquemaDePrueba.Nombre, null);

                servicios.PostConfigure<AuthenticationOptions>(opciones =>
                {
                    opciones.DefaultScheme = EsquemaDePrueba.Nombre;
                    opciones.DefaultAuthenticateScheme = EsquemaDePrueba.Nombre;
                    opciones.DefaultChallengeScheme = EsquemaDePrueba.Nombre;
                    opciones.DefaultForbidScheme = EsquemaDePrueba.Nombre;
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _conexion.Dispose();
            }
        }
    }

    // Un "login de mentira" que da por autenticado a todo pedido como Administrador.
    private sealed class EsquemaDePrueba(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador)
        : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, codificador)
    {
        public const string Nombre = "Prueba";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new("name", "Usuario de Prueba"),
                new(ClaimTypes.Role, "Administrador")
            };

            var identidad = new ClaimsIdentity(claims, Nombre);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), Nombre);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
