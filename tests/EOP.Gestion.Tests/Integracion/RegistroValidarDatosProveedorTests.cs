using EOP.Gestion.Core.CasosDeUso.Proveedores;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Integracion;

// Todavía ningún Controller pide ValidarDatosProveedor: lo va a hacer S2-T13. Sin esta prueba, un
// error al registrarlo o a alguna de sus dependencias recién se vería cuando llegue esa tarea.
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
}
