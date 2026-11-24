using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

public class AgregarConceptoManualValidacionesTests
{
    private readonly RepositorioConceptosFalso _repositorio = new();
    private readonly AlmacenBorradorEnMemoria _almacen = new();
    private readonly Usuario _usuario = new() { Id = 1, Activo = true, Rol = RolUsuario.Operador };

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public async Task TipoInvalido_SeRechaza(int tipoId)
    {
        string? error = await EjecutarAsync(tipoId);

        Assert.Equal("Seleccioná un tipo de concepto disponible.", error);
        Assert.False(_almacen.TieneCambiosPendientes(1, _usuario.Id));
        Assert.Equal(0, _repositorio.GuardadosEnBase);
    }

    [Fact]
    public async Task MaterialSinReferencia_SeRechaza()
    {
        string? error = await EjecutarAsync(
            TipoConcepto.MaterialId, cantidad: 2m);

        Assert.Contains("referencia", error);
        Assert.False(_almacen.TieneCambiosPendientes(1, _usuario.Id));
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
        Assert.False(_almacen.TieneCambiosPendientes(1, _usuario.Id));
    }

    [Fact]
    public async Task ImporteManualObligatorio()
    {
        string? error = await EjecutarAsync(
            TipoConcepto.ManoDeObraId,
            moneda: Moneda.UYU);

        Assert.Equal("Ingresá el importe del concepto.", error);
        Assert.False(_almacen.TieneCambiosPendientes(1, _usuario.Id));
    }

    [Fact]
    public async Task MonedaObligatoriaParaImporteManual()
    {
        string? error = await EjecutarAsync(
            TipoConcepto.AlquilerId,
            importe: 100m);

        Assert.Equal("Seleccioná USD o UYU.", error);
        Assert.False(_almacen.TieneCambiosPendientes(1, _usuario.Id));
    }

    [Fact]
    public async Task ImporteManualValido_SeAgregaAlBorradorSinPersistirEnBase()
    {
        string? error = await EjecutarAsync(
            TipoConcepto.TrasladoId,
            descripcion: "Transporte",
            importe: 150m,
            moneda: Moneda.USD);

        Assert.Null(error);
        Assert.Equal(0, _repositorio.GuardadosEnBase);

        BorradorPresupuestoDto? borrador = _almacen.Obtener(1, _usuario.Id);
        Assert.NotNull(borrador);
        ConceptoBorradorDto concepto = Assert.Single(borrador.Conceptos);

        Assert.True(concepto.EsNuevo);
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
        var casoDeUso = new AgregarConceptoManual(_repositorio, _almacen);

        return casoDeUso.EjecutarAsync(
            1, tipoId, descripcion, importe, moneda,
            referenciaId, cantidad, _usuario,
            CancellationToken.None);
    }

    private class RepositorioConceptosFalso
        : IRepositorioConceptosPresupuesto
    {
        public int GuardadosEnBase { get; private set; }

        public Task<Presupuesto?> ObtenerPresupuestoAsync(
            int presupuestoId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Presupuesto?>(new Presupuesto { Id = presupuestoId });

        public Task<bool> PuedeEditarEnBaseAsync(
            int presupuestoId, Usuario usuario, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(
            int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<BorradorPresupuestoDto?>(new BorradorPresupuestoDto
            {
                PresupuestoId = presupuestoId,
                HuellaEstadoBase = "BASE"
            });

        public Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(
            int referenciaId, CancellationToken cancellationToken) =>
            Task.FromResult<ReferenciaCatalogoPresupuestoDto?>(new ReferenciaCatalogoPresupuestoDto(
                10, referenciaId, "Cable", UnidadMedida.Metro, "Proveedor A", 100m, Moneda.UYU));

        public Task<string?> ObtenerNombreTipoConceptoAsync(
            int tipoId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("Traslado");

        public Task<ResultadoGuardarBorrador> GuardarBorradorSiPuedeEditarAsync(
            BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken)
        {
            GuardadosEnBase++;
            return Task.FromResult(ResultadoGuardarBorrador.Guardado);
        }
    }
}
