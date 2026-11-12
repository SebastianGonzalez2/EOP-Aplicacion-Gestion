using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Interfaces.Materiales;
using EOP.Gestion.Core.Interfaces.Proveedores;

namespace EOP.Gestion.Tests.Unitarias;

// S1-T27: el largo del nombre y los espacios de los extremos en el alta de materiales.
public class RegistrarMaterialTests
{
    private sealed class RepositorioMaterialesFalso : IRepositorioMateriales
    {
        public Material? MaterialGuardado { get; private set; }

        public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> AgregarSiNombreDisponibleAsync(Material material, MaterialProveedor primeraRelacion, CancellationToken cancellationToken = default)
        {
            MaterialGuardado = material;
            return Task.FromResult(true);
        }

        public Task<bool> ExistePorIdAsync(int materialId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteRelacionConProveedorAsync(int materialId, int proveedorId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> AgregarRelacionSiNoExisteAsync(MaterialProveedor relacion, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteOtroConNombreNormalizadoAsync(string nombreNormalizado, int materialId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Material?> ObtenerParaEdicionAsync(int materialId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<MaterialProveedor>> ObtenerRelacionesParaEdicionAsync(int materialId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> GuardarEdicionSiNombreDisponibleAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RepositorioProveedoresFalso : IRepositorioProveedores
    {
        public Task<bool> ExistePorIdAsync(int proveedorId, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ResultadoAgregarProveedor> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private readonly RepositorioMaterialesFalso _materiales = new();
    private readonly RegistrarMaterial _registrarMaterial;

    public RegistrarMaterialTests()
    {
        _registrarMaterial = new RegistrarMaterial(_materiales, new RepositorioProveedoresFalso());
    }

    private Task<List<string>> RegistrarAsync(string? nombre) =>
        _registrarMaterial.EjecutarAsync(nombre, UnidadMedida.Metro, 1, 150m, Moneda.UYU);

    [Fact]
    public async Task ConNombreDelLargoMaximo_LoGuarda()
    {
        string nombre = new('a', RegistrarMaterial.LargoMaximoNombre);

        List<string> errores = await RegistrarAsync(nombre);

        Assert.Empty(errores);
        Assert.Equal(nombre, _materiales.MaterialGuardado?.Nombre);
    }

    [Fact]
    public async Task ConNombreMasLargoQueLaColumna_LoRechazaSinGuardar()
    {
        List<string> errores = await RegistrarAsync(new string('a', RegistrarMaterial.LargoMaximoNombre + 1));

        Assert.Equal(["El nombre del material no puede superar los 200 caracteres."], errores);
        Assert.Null(_materiales.MaterialGuardado);
    }

    [Fact]
    public async Task QuitaLosEspaciosDeLosExtremos_AntesDeMedirYDeGuardar()
    {
        string nombre = new('a', RegistrarMaterial.LargoMaximoNombre);

        List<string> errores = await RegistrarAsync($"  {nombre}\t");

        Assert.Empty(errores);
        Assert.Equal(nombre, _materiales.MaterialGuardado?.Nombre);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SinNombre_LoRechazaComoObligatorio(string? nombre)
    {
        List<string> errores = await RegistrarAsync(nombre);

        Assert.Equal(["El nombre del material es obligatorio."], errores);
        Assert.Null(_materiales.MaterialGuardado);
    }
}
