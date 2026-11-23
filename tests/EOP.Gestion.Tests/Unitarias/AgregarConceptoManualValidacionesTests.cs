
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Tests.Unitarias;

public class AgregarConceptoManualValidacionesTests
{
    private readonly RepositorioConceptosFalso _repositorio = new();
    private readonly Usuario _usuario = new() { Id = 1 };

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public async Task TipoInvalido_SeRechaza(int tipoId)
    {
        string? error = await EjecutarAsync(tipoId);

        Assert.Equal("Seleccioná un tipo de concepto disponible.", error);
        Assert.Empty(_repositorio.ConceptosGuardados);
    }

    [Fact]
    public async Task MaterialSinReferencia_SeRechaza()
    {
        string? error = await EjecutarAsync(
            TipoConcepto.MaterialId, cantidad: 2m);

        Assert.Contains("referencia", error);
        Assert.Empty(_repositorio.ConceptosGuardados);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task MaterialConCantidadInvalida_SeRechaza(
        decimal cantidad)
    {
        string? error = await EjecutarAsync(
            TipoConcepto.MaterialId,
            referenciaId: 1,
            cantidad: cantidad);

        Assert.Contains("cantidad mayor que cero", error);
        Assert.Empty(_repositorio.ConceptosGuardados);
    }

    [Fact]
    public async Task ImporteManualObligatorio()
    {
        string? error = await EjecutarAsync(
            TipoConcepto.ManoDeObraId,
            moneda: Moneda.UYU);

        Assert.Equal("Ingresá el importe del concepto.", error);
        Assert.Empty(_repositorio.ConceptosGuardados);
    }

    [Fact]
    public async Task MonedaObligatoriaParaImporteManual()
    {
        string? error = await EjecutarAsync(
            TipoConcepto.AlquilerId,
            importe: 100m);

        Assert.Equal("Seleccioná USD o UYU.", error);
        Assert.Empty(_repositorio.ConceptosGuardados);
    }

    [Fact]
    public async Task ImporteManualValido_SeGuarda()
    {
        string? error = await EjecutarAsync(
            TipoConcepto.TrasladoId,
            descripcion: "Transporte",
            importe: 150m,
            moneda: Moneda.USD);

        Assert.Null(error);
        ConceptoPresupuesto concepto =
            Assert.Single(_repositorio.ConceptosGuardados);

        Assert.Equal("Transporte", concepto.Concepto);
        Assert.Equal(150m, concepto.PrecioUnitario);
        Assert.Equal(Moneda.USD, concepto.Moneda);
        Assert.Null(concepto.Cantidad);
        Assert.Null(concepto.ConceptoDetectado);
    }

    private Task<string?> EjecutarAsync(
        int tipoId,
        string? descripcion = null,
        decimal? importe = null,
        Moneda? moneda = null,
        int? referenciaId = null,
        decimal? cantidad = null)
    {
        var casoDeUso = new AgregarConceptoManual(_repositorio);

        return casoDeUso.EjecutarAsync(
            1, tipoId, descripcion, importe, moneda,
            referenciaId, cantidad, _usuario,
            CancellationToken.None);
    }

    private class RepositorioConceptosFalso
        : IRepositorioConceptosPresupuesto
    {
        public List<ConceptoPresupuesto> ConceptosGuardados { get; } = [];

        public Task<Presupuesto?> ObtenerPresupuestoAsync(
            int presupuestoId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<Presupuesto?>(null);
        }

        public Task<ResultadoGuardarConcepto> AgregarSiPuedeEditarAsync(
            ConceptoPresupuesto concepto,
            Usuario usuario,
            CancellationToken cancellationToken)
        {
            ConceptosGuardados.Add(concepto);

            return Task.FromResult(ResultadoGuardarConcepto.Guardado);
        }

        public Task<ResultadoSeleccionarMaterial> SeleccionarMaterialSiPuedeEditarAsync(
            int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Estas pruebas no seleccionan materiales de filas existentes.");
        }

        public Task<ResultadoSeleccionarMaterial> CambiarProveedorSiPuedeEditarAsync(
            int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Estas pruebas no cambian el proveedor de filas existentes.");
        }

        public Task<ResultadoSeleccionarMaterial> ActualizarReferenciaActualSiPuedeEditarAsync(
            int presupuestoId, int conceptoId, Usuario usuario, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Estas pruebas no actualizan la referencia de filas existentes.");
        }
    }
}
