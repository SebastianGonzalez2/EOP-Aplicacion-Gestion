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

// S3-T12 (RF-020) + S3-T14 (RF-05): exige la confirmación explícita y aplica los valores vigentes
// del catálogo sobre el borrador pendiente sin persistir en SQL Server hasta «Guardar borrador».
public class ActualizarReferenciaDeConceptoTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly AlmacenBorradorEnMemoria _almacen = new();
    private readonly Usuario _usuario = new() { Id = 1 };

    [Fact]
    public async Task SinConfirmacion_SeRechazaSinConsultarAlRepositorio()
    {
        string? error = await EjecutarAsync(confirmado: false);

        Assert.Contains("Confirmá la actualización", error);
        Assert.Equal(0, _repositorio.ConsultasPermiso);
    }

    [Fact]
    public async Task Confirmada_ActualizaLosCuatroValoresEnElBorradorSinPersistirEnBase()
    {
        string? error = await EjecutarAsync(confirmado: true);

        Assert.Null(error);
        Assert.Equal(0, _repositorio.GuardadosEnBase);
        var borrador = _almacen.Obtener(3, _usuario.Id);
        Assert.NotNull(borrador);
        var fila = Assert.Single(borrador.Conceptos);
        Assert.True(fila.FueModificado);
        Assert.Equal("Cable actualizado", fila.Concepto);
        Assert.Equal(UnidadMedida.Unidad, fila.Unidad);
        Assert.Equal(150m, fila.PrecioUnitario);
        Assert.Equal(Moneda.USD, fila.Moneda);
        Assert.Equal(20, fila.MaterialId);
        Assert.Equal(7, fila.MaterialProveedorId);
    }

    [Fact]
    public async Task SinPermiso_SeRechaza()
    {
        _repositorio.PuedeEditar = false;

        string? error = await EjecutarAsync(confirmado: true);

        Assert.Contains("ya no está disponible para edición", error);
    }

    [Fact]
    public async Task ConceptoInexistente_SeRechaza()
    {
        _repositorio.BorradorBase.Conceptos.Clear();

        string? error = await EjecutarAsync(confirmado: true);

        Assert.Contains("ya no existe", error);
    }

    [Fact]
    public async Task ConceptoNoEsMaterial_SeRechaza()
    {
        _repositorio.BorradorBase.Conceptos[0].TipoConceptoId = TipoConcepto.ManoDeObraId;

        string? error = await EjecutarAsync(confirmado: true);

        Assert.Contains("filas de tipo Material", error);
    }

    [Fact]
    public async Task ConceptoSinMaterial_SeRechaza()
    {
        _repositorio.BorradorBase.Conceptos[0].MaterialId = null;
        _repositorio.BorradorBase.Conceptos[0].MaterialProveedorId = null;

        string? error = await EjecutarAsync(confirmado: true);

        Assert.Contains("todavía no tiene un material", error);
    }

    [Fact]
    public async Task ReferenciaInexistente_SeRechaza()
    {
        _repositorio.ReferenciaActual = null;

        string? error = await EjecutarAsync(confirmado: true);

        Assert.Contains("ya no existe en el catálogo", error);
    }

    [Fact]
    public async Task ValoresYaCoincidenConCatalogo_SeRechazaSinCambios()
    {
        _repositorio.ReferenciaActual = new(20, 7, "Cable de cobre", UnidadMedida.Metro, "Proveedor A", 100m, Moneda.UYU);

        string? error = await EjecutarAsync(confirmado: true);

        Assert.Contains("ya coinciden con los del catálogo", error);
        Assert.False(_almacen.TieneCambiosPendientes(3, _usuario.Id));
    }

    private Task<string?> EjecutarAsync(bool confirmado) =>
        new ActualizarReferenciaDeConcepto(_repositorio, _almacen, new SincronizadorBorradorPresupuesto())
            .EjecutarAsync(3, 5, confirmado, _usuario, CancellationToken.None);

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
                    Concepto = "Cable de cobre",
                    MaterialId = 20,
                    MaterialProveedorId = 7,
                    ProveedorNombre = "Proveedor A",
                    Unidad = UnidadMedida.Metro,
                    PrecioUnitario = 100m,
                    Moneda = Moneda.UYU,
                    Cantidad = 10m
                }
            ]
        };

        public ReferenciaCatalogoPresupuestoDto? ReferenciaActual { get; set; } =
            new(20, 7, "Cable actualizado", UnidadMedida.Unidad, "Proveedor A", 150m, Moneda.USD);

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
            Task.FromResult(ReferenciaActual);

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
