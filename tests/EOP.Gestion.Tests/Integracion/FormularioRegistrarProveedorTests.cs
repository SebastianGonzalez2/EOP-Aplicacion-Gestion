using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
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

// Igual que en FormularioRegistrarClienteTests, levantamos la app entera en memoria y le pedimos
// la pantalla por HTTP, como lo haría el navegador. Comprobamos lo que sale de verdad de la vista.
// Reemplazamos dos cosas: el login, porque acá elegimos el rol con un encabezado del pedido, y SQL
// Server, por un SQLite en memoria con las mismas tablas. El repositorio y el caso de uso son los reales.
//
// Los permisos con la sesión real están en ValidacionSesionIniciadaIntegracionTests.RegistrarProveedor.cs,
// y lo que depende de SQL Server (duplicados y concurrencia) en RegistrarProveedorSqlServerTests.cs.
// El POST del formulario está en FormularioRegistrarProveedorTests.Post.cs.
//
// Lo que estas pruebas NO cubren es el JavaScript, porque acá no hay un navegador que lo ejecute.
// Sí comprobamos lo que el script necesita: los nombres de los campos, el botón y que el archivo exista.
public partial class FormularioRegistrarProveedorTests : IClassFixture<FormularioRegistrarProveedorTests.FabricaDePrueba>
{
    private const string Ruta = "/Proveedores/Registrar";
    private const string RutaDeRecepcion = "/RecepcionDePruebaProveedores/Recibir";

    private readonly FabricaDePrueba _fabrica;

    public FormularioRegistrarProveedorTests(FabricaDePrueba fabrica)
    {
        _fabrica = fabrica;
        _fabrica.ReiniciarBase();
    }

    // ---------- Campos del formulario ----------

    [Theory]
    [InlineData("Nombre")]
    [InlineData("Rut")]
    [InlineData("PaisTelefono")]
    [InlineData("NumeroNacionalTelefono")]
    [InlineData("CorreoContacto")]
    [InlineData("CorreoFacturacion")]
    public async Task Formulario_MuestraCadaDatoDelProveedorConSuEtiquetaYSuEspacioParaErrores(string campo)
    {
        string html = await LeerFormularioAsync();

        Assert.Contains($"name=\"{campo}\"", html);
        Assert.Matches($"<label[^>]*for=\"{campo}\"", html);
        Assert.Contains($"data-valmsg-for=\"{campo}\"", html);
    }

    [Theory]
    [InlineData("input", "Nombre")]
    [InlineData("input", "Rut")]
    [InlineData("select", "PaisTelefono")]
    [InlineData("input", "NumeroNacionalTelefono")]
    public async Task Formulario_IndicaQueLosDatosObligatoriosLoSon(string etiqueta, string campo)
    {
        string html = await LeerFormularioAsync();

        Assert.Contains("aria-required=\"true\"", ObtenerEtiquetaDelCampo(html, etiqueta, campo));
        Assert.Contains("Los campos marcados con", html);
    }

    [Fact]
    public async Task Formulario_MuestraUruguayElegidoYLaListaInternacionalDePaises()
    {
        string html = await LeerFormularioAsync();

        string selector = ObtenerSelector(html, "PaisTelefono");
        Assert.Contains("<option selected=\"selected\" value=\"UY\">Uruguay (+598)</option>", selector);
        Assert.Single(Regex.Matches(selector, "selected=\"selected\""));

        List<(string Valor, string Texto)> opciones = ObtenerOpcionesDelSelector(html, "PaisTelefono");
        Assert.True(opciones.Count > 200);
        Assert.Contains(("AR", "Argentina (+54)"), opciones);
        Assert.Contains(("BR", "Brasil (+55)"), opciones);
    }

    [Fact]
    public async Task Formulario_PideElNumeroDeTelefonoSinPrefijoYConTipoTel()
    {
        string html = await LeerFormularioAsync();

        Assert.Contains("type=\"tel\"", ObtenerEtiquetaDelCampo(html, "input", "NumeroNacionalTelefono"));
        Assert.Contains("sin el prefijo internacional", html);
    }

    // Los correos son opcionales de a uno, por eso ninguno se marca como obligatorio,
    // pero el usuario tiene que enterarse de que hace falta al menos uno.
    [Theory]
    [InlineData("CorreoContacto")]
    [InlineData("CorreoFacturacion")]
    public async Task Formulario_NoMarcaComoObligatorioNingunCorreoYExplicaLaRegla(string campo)
    {
        string html = await LeerFormularioAsync();

        Assert.DoesNotContain("aria-required", ObtenerEtiquetaDelCampo(html, "input", campo));
        Assert.Contains("al menos uno de los dos correos", html);
    }

    // ---------- Cuentas bancarias ----------

    [Fact]
    public async Task Formulario_ArrancaConUnaSolaCuentaYSinOpcionDeQuitarla()
    {
        string html = await LeerFormularioAsync();

        Assert.Single(Regex.Matches(html, "<fieldset class=\"cuenta-bancaria "));
        Assert.Contains("Cuenta bancaria 1", html);

        foreach (string campo in new[] { "Banco", "NumeroCuenta", "Titular", "Moneda" })
        {
            Assert.Contains($"name=\"CuentasBancarias[0].{campo}\"", html);
            Assert.Contains($"for=\"CuentasBancarias_0__{campo}\"", html);
            Assert.Contains($"data-valmsg-for=\"CuentasBancarias[0].{campo}\"", html);
        }

        Assert.DoesNotContain("CuentasBancarias[1]", html);

        string botonQuitar = Regex.Match(html, "<button[^>]*boton-quitar-cuenta[^>]*>").Value;
        Assert.Contains("hidden", botonQuitar);
    }

    [Fact]
    public async Task Formulario_OfreceBancoSoloDeLaListaFijaDelRequerimiento()
    {
        string html = await LeerFormularioAsync();

        // Están escritos acá a propósito, y no leídos del enum: así la prueba
        // compara lo que ve el usuario contra el texto de RF-23.
        var opcionesEsperadas = new List<(string Valor, string Texto)>
        {
            ("", "Seleccioná un banco"),
            ("1", "Banco de la República Oriental del Uruguay (BROU)"),
            ("2", "Banco Hipotecario del Uruguay (BHU)"),
            ("3", "Banco Bandes Uruguay S.A."),
            ("4", "Banco Bilbao Vizcaya Argentaria Uruguay S.A. (BBVA)"),
            ("5", "Banco BTG Pactual Uruguay S.A."),
            ("6", "Banco de la Nación Argentina — Sucursal Uruguay"),
            ("7", "Banco Itaú Uruguay S.A."),
            ("8", "Banco Santander S.A."),
            ("9", "Banque Heritage (Uruguay) S.A."),
            ("10", "Citibank N.A. — Sucursal Uruguay"),
            ("11", "Scotiabank Uruguay S.A.")
        };

        Assert.Equal(opcionesEsperadas, ObtenerOpcionesDelSelector(html, "CuentasBancarias[0].Banco"));
    }

    [Fact]
    public async Task Formulario_OfreceMonedaSoloUyuYUsd()
    {
        string html = await LeerFormularioAsync();

        var opcionesEsperadas = new List<(string Valor, string Texto)>
        {
            ("", "Seleccioná una moneda"),
            ("1", "UYU — Pesos uruguayos"),
            ("2", "USD — Dólares estadounidenses")
        };

        Assert.Equal(opcionesEsperadas, ObtenerOpcionesDelSelector(html, "CuentasBancarias[0].Moneda"));
    }

    // Si viniera un banco ya elegido, alguien podría guardar una cuenta en el banco
    // equivocado sin haberlo mirado nunca.
    [Theory]
    [InlineData("CuentasBancarias[0].Banco")]
    [InlineData("CuentasBancarias[0].Moneda")]
    public async Task Formulario_NoTraeNingunaOpcionElegidaDeAntemano(string campo)
    {
        string html = await LeerFormularioAsync();

        Assert.DoesNotContain("selected", ObtenerSelector(html, campo));
    }

    [Fact]
    public async Task Formulario_TieneLoQueNecesitaElScriptParaAgregarYQuitarCuentas()
    {
        HttpClient cliente = CrearCliente("Administrador");
        string html = await LeerHtmlAsync(await cliente.GetAsync(Ruta));

        Assert.Contains("id=\"lista-cuentas-bancarias\"", html);
        Assert.Contains("Agregar otra cuenta bancaria", html);

        // Tienen que ser type="button": con otro tipo, cada clic intentaría enviar el formulario.
        Assert.Contains("<button type=\"button\" id=\"boton-agregar-cuenta\"", html);
        Assert.Contains("<button type=\"button\" class=\"boton-quitar-cuenta ", html);

        string rutaDelScript = Regex.Match(html, "<script src=\"(/js/cuentas-bancarias-formulario\\.js[^\"]*)\"").Groups[1].Value;
        Assert.NotEmpty(rutaDelScript);

        HttpResponseMessage script = await cliente.GetAsync(rutaDelScript);
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
    }

    // ---------- Model binding ----------

    // Tomamos los nombres de los campos del HTML real y hacemos con ellos lo mismo que el script:
    // cambiar el 0 por el índice de cada cuenta. Si la vista generara nombres que el
    // ViewModel no entiende, esta prueba se daría cuenta.
    [Fact]
    public async Task Binding_ConLosNombresDelFormulario_RecibeTodasLasCuentasSinMezclarlas()
    {
        string html = await LeerFormularioAsync();
        Dictionary<string, string> datos = DatosValidosDelProveedor();
        AgregarCuentaConLosNombresDelFormulario(datos, html, 0, "1", "000111", "Primer titular", "1");
        AgregarCuentaConLosNombresDelFormulario(datos, html, 1, "8", "000222", "Segundo titular", "2");
        AgregarCuentaConLosNombresDelFormulario(datos, html, 2, "11", "000333", "Tercer titular", "1");

        DatosRecibidosPorLaPrueba recibido = await EnviarARecepcionAsync(datos);

        Assert.Empty(recibido.Errores);
        Assert.Equal("Proveedor de prueba", recibido.Modelo.Nombre);
        Assert.Equal(3, recibido.Modelo.CuentasBancarias.Count);

        Assert.Equal(Banco.BancoRepublicaOrientalDelUruguay, recibido.Modelo.CuentasBancarias[0].Banco);
        Assert.Equal("000111", recibido.Modelo.CuentasBancarias[0].NumeroCuenta);
        Assert.Equal("Primer titular", recibido.Modelo.CuentasBancarias[0].Titular);
        Assert.Equal(Moneda.UYU, recibido.Modelo.CuentasBancarias[0].Moneda);

        Assert.Equal(Banco.BancoSantander, recibido.Modelo.CuentasBancarias[1].Banco);
        Assert.Equal("000222", recibido.Modelo.CuentasBancarias[1].NumeroCuenta);
        Assert.Equal("Segundo titular", recibido.Modelo.CuentasBancarias[1].Titular);
        Assert.Equal(Moneda.USD, recibido.Modelo.CuentasBancarias[1].Moneda);

        Assert.Equal(Banco.ScotiabankUruguay, recibido.Modelo.CuentasBancarias[2].Banco);
        Assert.Equal("000333", recibido.Modelo.CuentasBancarias[2].NumeroCuenta);
    }

    // Esta prueba muestra por qué el script vuelve a numerar las cuentas cuando se quita una.
    // Sin renumerar, quitar la del medio deja [0] y [2], y la [2] se pierde en silencio.
    [Fact]
    public async Task Binding_ConUnHuecoEnLosIndices_PierdeLasCuentasPosteriores()
    {
        string html = await LeerFormularioAsync();
        Dictionary<string, string> datos = DatosValidosDelProveedor();
        AgregarCuentaConLosNombresDelFormulario(datos, html, 0, "1", "000111", "Primer titular", "1");
        AgregarCuentaConLosNombresDelFormulario(datos, html, 2, "8", "000333", "Tercer titular", "2");

        DatosRecibidosPorLaPrueba recibido = await EnviarARecepcionAsync(datos);

        Assert.Equal("000111", Assert.Single(recibido.Modelo.CuentasBancarias).NumeroCuenta);
    }

    [Fact]
    public async Task Binding_SinNingunaCuenta_PideAlMenosUna()
    {
        DatosRecibidosPorLaPrueba recibido = await EnviarARecepcionAsync(DatosValidosDelProveedor());

        Assert.Empty(recibido.Modelo.CuentasBancarias);
        Assert.Contains("Ingresá al menos una cuenta bancaria.", recibido.Errores);
    }

    [Theory]
    [InlineData("Banco", "Seleccioná el banco de la cuenta.")]
    [InlineData("NumeroCuenta", "Ingresá el número de cuenta.")]
    [InlineData("Titular", "Ingresá el titular de la cuenta.")]
    [InlineData("Moneda", "Seleccioná la moneda de la cuenta.")]
    public async Task Binding_ConUnaCuentaIncompleta_MuestraElErrorDeEseCampo(string campo, string mensaje)
    {
        string html = await LeerFormularioAsync();
        Dictionary<string, string> datos = DatosValidosDelProveedor();
        AgregarCuentaConLosNombresDelFormulario(datos, html, 0, "1", "000111", "Primer titular", "1");
        datos[$"CuentasBancarias[0].{campo}"] = "";

        DatosRecibidosPorLaPrueba recibido = await EnviarARecepcionAsync(datos);

        Assert.Equal(mensaje, Assert.Single(recibido.Errores));
    }

    [Theory]
    [InlineData("Nombre", "El nombre del proveedor es obligatorio.")]
    [InlineData("Rut", "El RUT es obligatorio.")]
    [InlineData("PaisTelefono", "Seleccioná el país del teléfono.")]
    [InlineData("NumeroNacionalTelefono", "Ingresá el número de teléfono.")]
    public async Task Binding_SinUnDatoObligatorio_DaElMensajeEnEspanol(string campo, string mensaje)
    {
        string html = await LeerFormularioAsync();
        Dictionary<string, string> datos = DatosValidosDelProveedor();
        AgregarCuentaConLosNombresDelFormulario(datos, html, 0, "1", "000111", "Primer titular", "1");
        datos[campo] = "";

        DatosRecibidosPorLaPrueba recibido = await EnviarARecepcionAsync(datos);

        Assert.Equal(mensaje, Assert.Single(recibido.Errores));
    }

    // Si el POST no manda el país, no tiene que asumirse Uruguay.
    [Fact]
    public async Task Binding_SinPaisEnElPedido_NoAsumeUruguay()
    {
        string html = await LeerFormularioAsync();
        Dictionary<string, string> datos = DatosValidosDelProveedor();
        AgregarCuentaConLosNombresDelFormulario(datos, html, 0, "1", "000111", "Primer titular", "1");
        datos.Remove("PaisTelefono");

        DatosRecibidosPorLaPrueba recibido = await EnviarARecepcionAsync(datos);

        Assert.Equal("Seleccioná el país del teléfono.", Assert.Single(recibido.Errores));
    }

    // La lista de países la carga el servidor: un pedido armado a mano no puede cambiarla.
    [Fact]
    public async Task Binding_ConUnaListaDePaisesEnElPedido_LaIgnora()
    {
        string html = await LeerFormularioAsync();
        Dictionary<string, string> datos = DatosValidosDelProveedor();
        AgregarCuentaConLosNombresDelFormulario(datos, html, 0, "1", "000111", "Primer titular", "1");
        datos["PaisesTelefono[0].Value"] = "ZZ";
        datos["PaisesTelefono[0].Text"] = "País inventado";

        DatosRecibidosPorLaPrueba recibido = await EnviarARecepcionAsync(datos);

        Assert.Empty(recibido.Errores);
        Assert.Empty(recibido.Modelo.PaisesTelefono);
    }

    // Editar el HTML del desplegable no permite meter un banco o una moneda que no están en la lista.
    [Theory]
    [InlineData("Banco", "0")]
    [InlineData("Banco", "12")]
    [InlineData("Banco", "Banco inventado")]
    [InlineData("Moneda", "0")]
    [InlineData("Moneda", "3")]
    [InlineData("Moneda", "EUR")]
    public async Task Binding_ConUnaOpcionFueraDeLaLista_LaRechaza(string campo, string valorManipulado)
    {
        string html = await LeerFormularioAsync();
        Dictionary<string, string> datos = DatosValidosDelProveedor();
        AgregarCuentaConLosNombresDelFormulario(datos, html, 0, "1", "000111", "Primer titular", "1");
        datos[$"CuentasBancarias[0].{campo}"] = valorManipulado;

        DatosRecibidosPorLaPrueba recibido = await EnviarARecepcionAsync(datos);

        Assert.NotEmpty(recibido.Errores);
    }

    // ---------- El envío del formulario ----------

    [Fact]
    public async Task Formulario_YaNoAvisaQueNoGuardaYTieneElBotonHabilitado()
    {
        string html = await LeerFormularioAsync();

        Assert.DoesNotContain("El guardado todavía no está disponible.", html);

        string botonEnviar = Assert.Single(Regex.Matches(html, "<button[^>]*type=\"submit\"[^>]*>[^<]*Registrar proveedor")).Value;
        Assert.DoesNotContain("disabled", botonEnviar);
    }

    // RF-23 pide confirmación expresa. Acá no hay navegador para ver el cuadro, pero sí podemos
    // comprobar que el script que se le entrega al usuario la pide.
    [Fact]
    public async Task Formulario_EntregaElScriptQuePideConfirmarElAlta()
    {
        HttpClient cliente = CrearCliente("Administrador");
        string html = await LeerHtmlAsync(await cliente.GetAsync(Ruta));
        string rutaDelScript = Regex.Match(html, "<script src=\"(/js/cuentas-bancarias-formulario\\.js[^\"]*)\"").Groups[1].Value;

        string script = await cliente.GetStringAsync(rutaDelScript);

        Assert.Contains("window.confirm(", script);
    }

    [Fact]
    public async Task Formulario_IncluyeElTokenAntifalsificacion()
    {
        string html = await LeerFormularioAsync();

        // Miramos solo este formulario, porque el botón "Cerrar sesión" de la barra de arriba tiene su propio token.
        Match formulario = Regex.Match(html, "<form method=\"post\" action=\"/Proveedores/Registrar\">.*?</form>", RegexOptions.Singleline);
        Assert.True(formulario.Success, "No se encontró el formulario de alta de proveedores.");

        Assert.Single(Regex.Matches(formulario.Value, "name=\"__RequestVerificationToken\""));
    }

    // ---------- Enlace en el menú ----------

    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Supervisor", true)]
    [InlineData("Operador", false)]
    public async Task Menu_MuestraElEnlaceSoloALosRolesQuePuedenRegistrar(string rol, bool debeVerElEnlace)
    {
        string html = await LeerHtmlAsync(await CrearCliente(rol).GetAsync("/"));

        if (debeVerElEnlace)
        {
            Assert.Contains("href=\"/Proveedores/Registrar\"", html);
        }
        else
        {
            Assert.DoesNotContain("/Proveedores", html);
        }
    }

    // ---------- Ayudas ----------

    private static Dictionary<string, string> DatosValidosDelProveedor() => new()
    {
        ["Nombre"] = "Proveedor de prueba",
        ["Rut"] = "215521750017",
        ["PaisTelefono"] = "UY",
        ["NumeroNacionalTelefono"] = "099123456",
        ["CorreoContacto"] = "proveedor@ejemplo.com",
        ["CorreoFacturacion"] = ""
    };

    // Busca en el HTML los campos de la cuenta 0 y los usa para armar la cuenta del índice pedido.
    private static void AgregarCuentaConLosNombresDelFormulario(
        Dictionary<string, string> datos, string html, int indice, string banco, string numeroCuenta, string titular, string moneda)
    {
        var valoresPorCampo = new Dictionary<string, string>
        {
            ["Banco"] = banco,
            ["NumeroCuenta"] = numeroCuenta,
            ["Titular"] = titular,
            ["Moneda"] = moneda
        };

        MatchCollection camposDelFormulario = Regex.Matches(html, "name=\"CuentasBancarias\\[0\\]\\.([^\"]+)\"");
        Assert.Equal(valoresPorCampo.Count, camposDelFormulario.Count);

        foreach (Match campoDelFormulario in camposDelFormulario)
        {
            string nombreDelCampo = campoDelFormulario.Groups[1].Value;
            datos[$"CuentasBancarias[{indice}].{nombreDelCampo}"] = valoresPorCampo[nombreDelCampo];
        }
    }

    private async Task<DatosRecibidosPorLaPrueba> EnviarARecepcionAsync(Dictionary<string, string> datos)
    {
        HttpResponseMessage respuesta = await CrearCliente("Administrador").PostAsync(RutaDeRecepcion, new FormUrlEncodedContent(datos));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        DatosRecibidosPorLaPrueba? recibido = await respuesta.Content.ReadFromJsonAsync<DatosRecibidosPorLaPrueba>();
        Assert.NotNull(recibido);

        return recibido;
    }

    private async Task<string> LeerFormularioAsync()
    {
        HttpResponseMessage respuesta = await CrearCliente("Administrador").GetAsync(Ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        return await LeerHtmlAsync(respuesta);
    }

    // Razor escribe "República" como "Rep&#xFA;blica". Para el navegador es lo mismo,
    // así que lo decodificamos para poder comparar el texto que ve el usuario.
    private static async Task<string> LeerHtmlAsync(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    private static string ObtenerEtiquetaDelCampo(string html, string etiqueta, string campo)
    {
        Match encontrada = Regex.Match(html, $"<{etiqueta}[^>]*name=\"{Regex.Escape(campo)}\"[^>]*>");
        Assert.True(encontrada.Success, $"No se encontró el campo {campo}.");

        return encontrada.Value;
    }

    private static string ObtenerSelector(string html, string campo)
    {
        Match encontrado = Regex.Match(html, $"<select[^>]*name=\"{Regex.Escape(campo)}\"[^>]*>.*?</select>", RegexOptions.Singleline);
        Assert.True(encontrado.Success, $"No se encontró el selector {campo}.");

        return encontrado.Value;
    }

    private static List<(string Valor, string Texto)> ObtenerOpcionesDelSelector(string html, string campo)
    {
        var opciones = new List<(string Valor, string Texto)>();

        foreach (Match opcion in Regex.Matches(ObtenerSelector(html, campo), "<option[^>]*value=\"([^\"]*)\"[^>]*>([^<]*)</option>"))
        {
            opciones.Add((opcion.Groups[1].Value, opcion.Groups[2].Value));
        }

        return opciones;
    }

    private HttpClient CrearCliente(string rol)
    {
        HttpClient cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        cliente.DefaultRequestHeaders.Add(EsquemaDePrueba.EncabezadoRol, rol);

        return cliente;
    }

    public sealed class FabricaDePrueba : WebApplicationFactory<Program>
    {
        // Mientras la conexión siga abierta, la base en memoria existe. La comparten todos los
        // pedidos, como una base real.
        private readonly SqliteConnection _conexion = new("Data Source=:memory:");

        public FabricaDePrueba()
        {
            _conexion.Open();
        }

        // La fábrica se comparte entre las pruebas de la clase, así que cada una empieza vaciando
        // las tablas: si no, el proveedor de una chocaría por duplicado con el de la anterior.
        public void ReiniciarBase()
        {
            using IServiceScope alcance = Services.CreateScope();
            EopDbContext contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

            contexto.Database.EnsureCreated();
            contexto.CuentasBancarias.ExecuteDelete();
            contexto.Proveedores.ExecuteDelete();
        }

        public async Task GuardarProveedorAsync(Proveedor proveedor)
        {
            using IServiceScope alcance = Services.CreateScope();
            EopDbContext contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

            contexto.Proveedores.Add(proveedor);
            await contexto.SaveChangesAsync();
        }

        public async Task<List<Proveedor>> LeerProveedoresAsync()
        {
            using IServiceScope alcance = Services.CreateScope();
            EopDbContext contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

            return await contexto.Proveedores
                .AsNoTracking()
                .Include(proveedor => proveedor.CuentasBancarias)
                .ToListAsync();
        }

        public async Task<int> ContarCuentasAsync()
        {
            using IServiceScope alcance = Services.CreateScope();
            EopDbContext contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

            return await contexto.CuentasBancarias.CountAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _conexion.Dispose();
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:EopDb",
                "Server=(localdb)\\pruebas;Database=NoSeUsa;Trusted_Connection=True");

            builder.ConfigureTestServices(servicios =>
            {
                // Sacamos la configuración de SQL Server y ponemos SQLite. Si no sacáramos las dos
                // registraciones, EF Core vería dos proveedores de base y fallaría.
                servicios.RemoveAll<DbContextOptions<EopDbContext>>();
                servicios.RemoveAll<IDbContextOptionsConfiguration<EopDbContext>>();
                servicios.AddDbContext<EopDbContext>(opciones => opciones.UseSqlite(_conexion));

                // Le mostramos a MVC el Controller que solo existe en las pruebas.
                servicios.AddControllersWithViews()
                    .AddApplicationPart(typeof(RecepcionDePruebaProveedoresController).Assembly);

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
    }

    // Un "login de mentira" que da por autenticado a todo pedido, con el rol que indique el encabezado.
    private sealed class EsquemaDePrueba(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador)
        : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, codificador)
    {
        public const string Nombre = "Prueba";
        public const string EncabezadoRol = "X-Rol-De-Prueba";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new("name", "Usuario de Prueba"),
                new(ClaimTypes.Role, Request.Headers[EncabezadoRol].ToString())
            };

            var identidad = new ClaimsIdentity(claims, Nombre);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), Nombre);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
