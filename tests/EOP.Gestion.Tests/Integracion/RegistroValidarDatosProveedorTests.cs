using EOP.Gestion.Core.CasosDeUso.Proveedores;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Integracion;

// Comprueba que los casos de uso de proveedores estén bien registrados, con todas sus dependencias.
// Un error acá recién se vería al pedir la pantalla, y con un mensaje poco claro.
public class RegistroValidarDatosProveedorTests : IClassFixture<FormularioRegistrarProveedorTests.FabricaDePrueba>
{
    private readonly FormularioRegistrarProveedorTests.FabricaDePrueba _fabrica;

    public RegistroValidarDatosProveedorTests(FormularioRegistrarProveedorTests.FabricaDePrueba fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public void ValidarDatosProveedor_DebePoderCrearseConLosServiciosDeLaAplicacion()
    {
        // Es Scoped, igual que el DbContext que usa su repositorio: vive lo que dura un pedido.
        using IServiceScope alcance = _fabrica.Services.CreateScope();

        ValidarDatosProveedor? validador = alcance.ServiceProvider.GetService<ValidarDatosProveedor>();

        Assert.NotNull(validador);
    }

    [Fact]
    public void RegistrarProveedor_DebePoderCrearseConLosServiciosDeLaAplicacion()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();

        RegistrarProveedor? casoDeUso = alcance.ServiceProvider.GetService<RegistrarProveedor>();

        Assert.NotNull(casoDeUso);
    }
}
