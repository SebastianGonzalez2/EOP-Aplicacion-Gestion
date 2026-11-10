
using System.Net;
using EOP.Gestion.Tests.Compartido;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EOP.Gestion.Tests.Integracion;

public class ListadoProveedoresTests
    : IClassFixture<FormularioRegistrarProveedorTests.FabricaDePrueba>
{
    private readonly FormularioRegistrarProveedorTests.FabricaDePrueba _fabrica;

    public ListadoProveedoresTests(
        FormularioRegistrarProveedorTests.FabricaDePrueba fabrica)
    {
        _fabrica = fabrica;
        _fabrica.ReiniciarBase();
    }

    [Fact]
    public async Task SinProveedores_MuestraMensajeDeListaVacia()
    {
        using HttpClient cliente = CrearCliente("Administrador");

        HttpResponseMessage respuesta =
            await cliente.GetAsync("/Proveedores");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Contains("No se encontraron proveedores.", html);
        Assert.Contains("Todavía no hay proveedores registrados.", html);
    }

    [Fact]
    public async Task ConProveedores_MuestraLosDatosEnOrden()
    {
        var zeta = DatosDePruebaProveedores.CrearProveedor(
            "Zeta", "215521750017");

        var alfa = DatosDePruebaProveedores.CrearProveedor(
            "Alfa", "210000000019");

        await _fabrica.GuardarProveedorAsync(zeta);
        await _fabrica.GuardarProveedorAsync(alfa);

        using HttpClient cliente = CrearCliente("Administrador");

        HttpResponseMessage respuesta =
            await cliente.GetAsync("/Proveedores");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        string html = WebUtility.HtmlDecode(
    await respuesta.Content.ReadAsStringAsync());

        Assert.Contains("Alfa", html);
        Assert.Contains("Zeta", html);
        Assert.Contains(alfa.Rut, html);
        Assert.Contains(zeta.Telefono, html);
        Assert.Contains("Se muestran 2 proveedores.", html);

        // Alfa debe aparecer antes de Zeta.
        int posicionAlfa = html.IndexOf("Alfa", StringComparison.Ordinal);
        int posicionZeta = html.IndexOf("Zeta", StringComparison.Ordinal);

        Assert.True(posicionAlfa < posicionZeta);
    }

    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Supervisor", true)]
    [InlineData("Operador", false)]
    public async Task Listado_RespetaLosPermisosDelBotonRegistrar(
        string rol, bool puedeRegistrar)
    {
        using HttpClient cliente = CrearCliente(rol);

        HttpResponseMessage respuesta =
            await cliente.GetAsync("/Proveedores");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        string html = await respuesta.Content.ReadAsStringAsync();

        if (puedeRegistrar)
        {
            Assert.Contains("href=\"/Proveedores/Registrar\"", html);
        }
        else
        {
            Assert.DoesNotContain(
                "href=\"/Proveedores/Registrar\"", html);
        }
    }

    private HttpClient CrearCliente(string rol)
    {
        HttpClient cliente = _fabrica.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });

        cliente.DefaultRequestHeaders.Add("X-Rol-De-Prueba", rol);

        return cliente;
    }
}
