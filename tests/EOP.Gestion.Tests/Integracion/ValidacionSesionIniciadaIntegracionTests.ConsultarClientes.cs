using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static EOP.Gestion.Tests.Compartido.DatosDePruebaClientes;

namespace EOP.Gestion.Tests.Integracion;

// S1-T32 (US-052, RF-84, RF-85, RF-73): listado de clientes por HTTP, con cookie real,
// OnValidatePrincipal, [Authorize], ConsultarClientes, la vista y las tablas reales.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    private const string RutaClientes = "/Clientes";
    private const string MensajeSinClientes = "No se encontraron clientes.";

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task CadaRol_PuedeConsultarBuscarYFiltrarElListado(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookie = await PrepararSesionAsync(fabrica, rol, CrearClientes());

        AssertNombresMostrados(await LeerListadoAsync(fabrica, cookie), AnaGarcia, AnaPereira, BrunoSilva);
        AssertNombresMostrados(await LeerListadoAsync(fabrica, cookie, "?busqueda=Ana&activo=true"), AnaPereira);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/Index")]
    [InlineData("?busqueda=Ana&activo=true")]
    public async Task PedidoSinSesion_NoAccedeAlListadoNiVeDatosDeClientes(string sufijo)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        await GuardarClientesAsync(fabrica, CrearClientes());

        var respuesta = await CrearCliente(fabrica).GetAsync(RutaClientes + sufijo);
        string cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.DoesNotContain(AnaPereira, cuerpo);
        Assert.DoesNotContain(RutAnaPereira, cuerpo);
    }

    [Fact]
    public async Task Listado_MuestraCadaDatoEnSuColumnaYNoInventaCorreosAusentes()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookie = await PrepararSesionAsync(fabrica, RolUsuario.Operador, CrearClientes());

        string html = await LeerListadoAsync(fabrica, cookie);

        Assert.Equal(
            [AnaPereira, RutAnaPereira, "ana@ejemplo.com", "Sin informar", "+59899123456", "Activo"],
            ObtenerCeldasDelCliente(html, AnaPereira));
        Assert.Equal(
            [AnaGarcia, RutAnaGarcia, "Sin informar", "facturas@garcia.com", "+59899234567", "Inactivo"],
            ObtenerCeldasDelCliente(html, AnaGarcia));
        Assert.Equal(
            [BrunoSilva, RutBrunoSilva, "bruno@ejemplo.com", "facturas@bruno.com", "+59899345678", "Activo"],
            ObtenerCeldasDelCliente(html, BrunoSilva));
    }

    [Theory]
    [InlineData("", AnaGarcia, AnaPereira, BrunoSilva)]
    [InlineData("?busqueda=&activo=", AnaGarcia, AnaPereira, BrunoSilva)]
    [InlineData("?busqueda=Ana", AnaGarcia, AnaPereira)]
    [InlineData("?busqueda=" + RutBrunoSilva, BrunoSilva)]
    [InlineData("?activo=true", AnaPereira, BrunoSilva)]
    [InlineData("?activo=false", AnaGarcia)]
    [InlineData("?busqueda=Ana&activo=true", AnaPereira)]
    [InlineData("?busqueda=Ana&activo=false", AnaGarcia)]
    [InlineData("?busqueda=" + RutAnaGarcia + "&activo=false", AnaGarcia)]
    public async Task ParametrosDeLaUrl_MuestranSoloLosClientesQueCumplenLosCriterios(string consulta, params string[] esperados)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookie = await PrepararSesionAsync(fabrica, RolUsuario.Operador, CrearClientes());

        string html = await LeerListadoAsync(fabrica, cookie, consulta);

        AssertNombresMostrados(html, esperados);
        Assert.DoesNotContain(MensajeSinClientes, html);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?busqueda=Ana")]
    [InlineData("?activo=true")]
    [InlineData("?activo=false")]
    [InlineData("?busqueda=Ana&activo=true")]
    public async Task SinClientesRegistrados_MuestraElMensajeSinResultados(string consulta)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookie = await PrepararSesionAsync(fabrica, RolUsuario.Operador);

        Assert.Contains(MensajeSinClientes, await LeerListadoAsync(fabrica, cookie, consulta));
    }

    [Theory]
    [InlineData("?busqueda=Inexistente")]
    [InlineData("?busqueda=999999999999")]
    [InlineData("?busqueda=Inexistente&activo=true")]
    [InlineData("?busqueda=Pereira&activo=false")]
    [InlineData("?busqueda=Garcia&activo=true")]
    [InlineData("?busqueda=" + RutBrunoSilva + "&activo=false")]
    public async Task CriteriosSinCoincidencias_MuestranElMensajeSinResultadosYNingunCliente(string consulta)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookie = await PrepararSesionAsync(fabrica, RolUsuario.Operador, CrearClientes());

        string html = await LeerListadoAsync(fabrica, cookie, consulta);

        Assert.Contains(MensajeSinClientes, html);
        AssertNombresMostrados(html);
    }

    [Fact]
    public async Task ConsultarBuscarYFiltrar_NoModificaLosClientesRegistrados()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookie = await PrepararSesionAsync(fabrica, RolUsuario.Operador, CrearClientes());
        List<Cliente> antes = await LeerClientesAsync(fabrica);
        Assert.Equal(Nombres.Length, antes.Count);

        string[] consultas =
        [
            "",
            "?busqueda=Ana",
            "?busqueda=" + RutAnaPereira,
            "?activo=true",
            "?activo=false",
            "?busqueda=Ana&activo=true",
            "?busqueda=Inexistente"
        ];

        foreach (string consulta in consultas)
        {
            await LeerListadoAsync(fabrica, cookie, consulta);
        }

        Assert.Equivalent(antes, await LeerClientesAsync(fabrica), strict: true);
    }

    private static async Task<string> PrepararSesionAsync(FabricaConSesionReal fabrica, RolUsuario rol, params Cliente[] clientes)
    {
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        await GuardarClientesAsync(fabrica, clientes);

        return await IniciarSesionYObtenerCookieAsync(fabrica);
    }

    private static async Task GuardarClientesAsync(FabricaConSesionReal fabrica, Cliente[] clientes)
    {
        using var alcance = fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        contexto.Clientes.AddRange(clientes);
        await contexto.SaveChangesAsync();
    }

    private static async Task<List<Cliente>> LeerClientesAsync(FabricaConSesionReal fabrica)
    {
        using var alcance = fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        return await contexto.Clientes.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
    }

    private static async Task<string> LeerListadoAsync(FabricaConSesionReal fabrica, string cookie, string consulta = "")
    {
        var respuesta = await PedirConCookieAsync(fabrica, RutaClientes + consulta, cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        // Razor codifica caracteres como el "+" del WhatsApp; comparamos el texto que ve el usuario.
        return WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
    }

    // Comprueba qué clientes aparecen y en qué orden.
    private static void AssertNombresMostrados(string html, params string[] esperados)
    {
        var mostrados = Nombres
            .Where(html.Contains)
            .OrderBy(nombre => html.IndexOf(nombre, StringComparison.Ordinal));

        Assert.Equal(esperados, mostrados);
    }

    private static string[] ObtenerCeldasDelCliente(string html, string nombre)
    {
        string fila = Regex.Matches(html, "<tr>(.*?)</tr>", RegexOptions.Singleline)
            .Select(coincidencia => coincidencia.Groups[1].Value)
            .Single(contenido => contenido.Contains($"<td>{nombre}</td>"));

        return Regex.Matches(fila, "<td>(.*?)</td>", RegexOptions.Singleline)
            .Select(celda => Regex.Replace(celda.Groups[1].Value, "<[^>]+>", string.Empty).Trim())
            .ToArray();
    }
}
