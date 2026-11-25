using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T09 (US-007) + S3-T14 (RF-05): valida la selección expresa y aplica el cambio sobre el
// borrador pendiente sin persistir en SQL Server hasta «Guardar borrador».
public class SeleccionarMaterialDeConceptoTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly AlmacenBorradorEnMemoria _almacen = new();
    private readonly Usuario _usuario = new() { Id = 1 };

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SinReferenciaElegida_SeRechazaSinConsultarAlRepositorio(int? referenciaId)
    {
        string? error = await EjecutarAsync(referenciaId);

        Assert.Equal("Seleccioná una referencia de material y proveedor.", error);
        Assert.Equal(0, _repositorio.ConsultasPermiso);
    }

    [Fact]
    public async Task ReferenciaElegida_SeAplicaAlBorradorConservandoDetectadoYCantidadSinPersistirEnBase()
    {
        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Null(error);
        Assert.Equal(0, _repositorio.GuardadosEnBase);
        var borrador = _almacen.Obtener(3, _usuario.Id);
        Assert.NotNull(borrador);
        var fila = Assert.Single(borrador.Conceptos);
        Assert.True(fila.FueModificado);
        Assert.Equal(7, fila.MaterialProveedorId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(UnidadMedida.Metro, fila.Unidad);
        Assert.Equal(100m, fila.PrecioUnitario);
        Assert.Equal(Moneda.UYU, fila.Moneda);
        Assert.Equal("detectado por IA", fila.ConceptoDetectado);
        Assert.Equal(10m, fila.Cantidad);
    }

    [Fact]
    public async Task SinPermiso_SeRechaza()
    {
        _repositorio.PuedeEditar = false;

        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Contains("ya no está disponible para edición", error);
    }

    [Fact]
    public async Task ConceptoInexistente_SeRechaza()
    {
        _repositorio.BorradorBase.Conceptos.Clear();

        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Contains("ya no existe", error);
    }

    [Fact]
    public async Task ConceptoNoEsMaterial_SeRechaza()
    {
        _repositorio.BorradorBase.Conceptos[0].TipoConceptoId = TipoConcepto.ManoDeObraId;

        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Contains("filas de tipo Material", error);
    }

    [Fact]
    public async Task ReferenciaInexistente_SeRechaza()
    {
        _repositorio.Referencia = null;

        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Contains("no existe", error);
    }

    [Fact]
    public async Task ReferenciaDuplicada_SeRechaza()
    {
        _repositorio.BorradorBase.Conceptos.Add(new ConceptoBorradorDto
        {
            Id = 6,
            TipoConceptoId = TipoConcepto.MaterialId,
            MaterialId = 20,
            MaterialProveedorId = 7
        });

        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Contains("ya están en otra fila", error);
    }

    [Fact]
    public async Task MismaReferencia_SeRechazaSinCambios()
    {
        _repositorio.BorradorBase.Conceptos[0].MaterialId = 20;
        _repositorio.BorradorBase.Conceptos[0].MaterialProveedorId = 7;

        string? error = await EjecutarAsync(referenciaId: 7);

        Assert.Contains("ya es la seleccionada", error);
    }

    private Task<string?> EjecutarAsync(int? referenciaId) =>
        new SeleccionarMaterialDeConcepto(_repositorio, _almacen, new SincronizadorBorradorPresupuesto())
            .EjecutarAsync(3, 5, referenciaId, _usuario, CancellationToken.None);

    private class RepositorioFalso : IRepositorioConceptosPresupuesto
    {
        public bool PuedeEditar { get; set; } = true;
        public int ConsultasPermiso { get; private set; }
        public int GuardadosEnBase { get; private set; }

        public BorradorPresupuestoDto BorradorBase { get; } = new()
        {
            PresupuestoId = 3,
            HuellaEstadoBase = "BASE",
            Conceptos =
            [
                new ConceptoBorradorDto
                {
                    Id = 5,
                    TipoConceptoId = TipoConcepto.MaterialId,
                    TipoNombre = "Material",
                    ConceptoDetectado = "detectado por IA",
                    Cantidad = 10m
                }
            ]
        };

        public ReferenciaCatalogoPresupuestoDto? Referencia { get; set; } =
            new(20, 7, "Cable de cobre", UnidadMedida.Metro, "Proveedor A", 100m, Moneda.UYU);

        public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<Presupuesto?>(new Presupuesto { Id = presupuestoId });

        public Task<bool> PuedeEditarEnBaseAsync(int presupuestoId, Usuario usuario, CancellationToken cancellationToken)
        {
            ConsultasPermiso++;
            return Task.FromResult(PuedeEditar);
        }

        public Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<BorradorPresupuestoDto?>(BorradorBase.Clonar());

        public Task<string?> ObtenerHuellaPersistidaAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(BorradorBase.HuellaEstadoBase);

        public Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(int referenciaId, CancellationToken cancellationToken) =>
            Task.FromResult(Referencia);

        public Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("Material");

        public Task<ResultadoPersistenciaBorrador> GuardarBorradorSiPuedeEditarAsync(
            BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken)
        {
            GuardadosEnBase++;
            return Task.FromResult(new ResultadoPersistenciaBorrador(ResultadoGuardarBorrador.Guardado, "NUEVA"));
        }
    }
}
