using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Tests.Compartido;

// La app entera con TODA su autenticación real: Program.cs, Microsoft.Identity.Web, el manejador real
// de OpenID Connect y la cookie real. Lo único sustituido es del otro lado del protocolo: Microsoft
// (ProveedorEntraSimulado) y la base (SQLite en memoria o, si se indica, un SQL Server de prueba).
internal sealed class FabricaConEntraSimulado : WebApplicationFactory<Program>
{
    private const string ClientSecretFicticio = "secreto-ficticio-que-no-sale-de-la-prueba";

    private readonly string? _cadenaSqlServer;
    private readonly SqliteConnection? _conexionSqlite;
    private readonly Action<IServiceCollection>? _configurarServiciosAdicionales;

    public ProveedorEntraSimulado Entra { get; } = new();

    public RegistroDeLogsDePrueba Logs { get; } = new();

    // Sin cadena se usa SQLite en memoria. La cadena de SQL Server debe venir de ServidorSqlDePrueba.
    public FabricaConEntraSimulado(
        string? cadenaSqlServer = null,
        Action<IServiceCollection>? configurarServiciosAdicionales = null)
    {
        _cadenaSqlServer = cadenaSqlServer;
        _configurarServiciosAdicionales = configurarServiciosAdicionales;

        if (cadenaSqlServer == null)
        {
            _conexionSqlite = new SqliteConnection("Data Source=:memory:");
            _conexionSqlite.Open();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Testing: ni carga los User Secrets (solo se cargan en Development) ni la página de error para desarrolladores.
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:EopDb", "Server=(localdb)\\pruebas;Database=NoSeUsa;Trusted_Connection=True");
        builder.UseSetting("https_port", "443");

        builder.ConfigureAppConfiguration((_, configuracion) => configuracion.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:ClientSecret"] = ClientSecretFicticio
        }));

        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<EopDbContext>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<EopDbContext>>();
            servicios.AddDbContext<EopDbContext>(opciones =>
            {
                if (_cadenaSqlServer != null)
                {
                    opciones.UseSqlServer(_cadenaSqlServer);
                }
                else
                {
                    opciones.UseSqlite(_conexionSqlite!);
                }
            });

            servicios.AddSingleton<ILoggerProvider>(Logs);
            servicios.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, Entra.AplicarA);
            _configurarServiciosAdicionales?.Invoke(servicios);
        });
    }

    public Navegador CrearNavegador() => new(this);

    public async Task<int> GuardarUsuarioAsync(string correo, string? entraObjectId, RolUsuario rol = RolUsuario.Operador, bool activo = true)
    {
        using var alcance = CrearAlcanceConBaseCreada();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        var usuario = new Usuario
        {
            NombreCompleto = "Usuario de prueba",
            CorreoCorporativo = correo,
            Rol = rol,
            Activo = activo,
            EntraObjectId = entraObjectId
        };

        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }

    public async Task<Usuario> LeerUsuarioAsync(int usuarioId)
    {
        using var alcance = CrearAlcanceConBaseCreada();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        return await contexto.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
    }

    public async Task ModificarUsuarioAsync(int usuarioId, Action<Usuario> cambio)
    {
        using var alcance = CrearAlcanceConBaseCreada();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        cambio(await contexto.Usuarios.SingleAsync(u => u.Id == usuarioId));
        await contexto.SaveChangesAsync();
    }

    // La base SQL Server la migra quien la creó; SQLite se crea acá la primera vez.
    private IServiceScope CrearAlcanceConBaseCreada()
    {
        var alcance = Services.CreateScope();

        if (_cadenaSqlServer == null)
        {
            alcance.ServiceProvider.GetRequiredService<EopDbContext>().Database.EnsureCreated();
        }

        return alcance;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _conexionSqlite?.Dispose();
            Entra.Dispose();
        }
    }

    // Se comporta como un navegador con las cookies: guarda las que manda el servidor, las borra cuando se lo
    // pide y las reenvía. El HttpClient no sigue las redirecciones, para poder revisar cada paso del protocolo.
    internal sealed class Navegador(FabricaConEntraSimulado fabrica)
    {
        private const string NombreCookieDeSesion = ".AspNetCore.Cookies";

        private readonly Dictionary<string, string> _cookies = [];

        private readonly HttpClient _cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

        public bool TieneSesion => _cookies.Keys.Any(nombre => nombre.StartsWith(NombreCookieDeSesion, StringComparison.Ordinal));

        public string CookiesActuales => string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}"));

        public Task<HttpResponseMessage> GetAsync(string ruta) => EnviarAsync(new HttpRequestMessage(HttpMethod.Get, ruta));

        public Task<HttpResponseMessage> PostAsync(string ruta, IEnumerable<KeyValuePair<string, string>> formulario) =>
            EnviarAsync(new HttpRequestMessage(HttpMethod.Post, ruta) { Content = new FormUrlEncodedContent(formulario) });

        public async Task<HttpResponseMessage> EnviarAsync(HttpRequestMessage pedido)
        {
            if (_cookies.Count > 0)
            {
                pedido.Headers.Add("Cookie", CookiesActuales);
            }

            HttpResponseMessage respuesta = await _cliente.SendAsync(pedido);

            if (respuesta.Headers.TryGetValues("Set-Cookie", out var cookiesRecibidas))
            {
                foreach (string cookie in cookiesRecibidas)
                {
                    string parNombreValor = cookie.Split(';')[0];
                    int separador = parNombreValor.IndexOf('=');
                    string nombre = parNombreValor[..separador];
                    string valor = parNombreValor[(separador + 1)..];

                    if (valor.Length == 0 || cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase))
                    {
                        _cookies.Remove(nombre);
                    }
                    else
                    {
                        _cookies[nombre] = valor;
                    }
                }
            }

            return respuesta;
        }

        // Hace el recorrido completo: pedir el login, "autenticarse" en Microsoft y volver a /signin-oidc con el código.
        public async Task<HttpResponseMessage> IniciarSesionAsync(
            IReadOnlyDictionary<string, object> claims, Action<ProveedorEntraSimulado.AjustesDelToken>? ajustar = null)
        {
            var parametros = await PedirLoginAsync();

            string codigo = fabrica.Entra.EmitirCodigo(parametros["nonce"], parametros["code_challenge"], claims, ajustar);

            return await PostAsync("/signin-oidc", new Dictionary<string, string>
            {
                ["code"] = codigo,
                ["state"] = parametros["state"]
            });
        }

        // Devuelve los parámetros con los que la app mandó a la persona a Microsoft.
        public async Task<Dictionary<string, string>> PedirLoginAsync()
        {
            HttpResponseMessage desafio = await GetAsync("/Cuenta/IniciarSesion");
            Assert.Equal(HttpStatusCode.Redirect, desafio.StatusCode);

            return QueryHelpers.ParseQuery(desafio.Headers.Location!.Query)
                .ToDictionary(parametro => parametro.Key, parametro => parametro.Value.ToString());
        }

        // El token antifalsificación está en el botón "Cerrar sesión" de cualquier pantalla con sesión.
        public async Task<string> ObtenerTokenAntifalsificacionAsync(string ruta = "/")
        {
            HttpResponseMessage pagina = await GetAsync(ruta);
            Match campo = Regex.Match(
                await pagina.Content.ReadAsStringAsync(),
                "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");

            Assert.True(campo.Success, $"{ruta} no incluyó el token antifalsificación.");
            return campo.Groups[1].Value;
        }
    }
}
