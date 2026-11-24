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

public class GuardarBorradorPresupuestoTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly AlmacenBorradorEnMemoria _almacen = new();
    private readonly Usuario _usuario = new() { Id = 1, Activo = true, Rol = RolUsuario.Operador };

    private static BorradorPresupuestoDto CrearBorradorConCambios() => new()
    {
        PresupuestoId = 10,
        HuellaEstadoBase = "HUELLA-BASE",
        Conceptos =
        [
            new ConceptoBorradorDto
            {
                Id = -1,
                TipoConceptoId = TipoConcepto.ManoDeObraId,
                TipoNombre = "Mano de obra",
                Concepto = "Instalación",
                PrecioUnitario = 1200m,
                Moneda = Moneda.UYU,
                EsNuevo = true
            }
        ]
    };

    [Fact]
    public void CalculadorHuella_EsDeterministicoYNormalizaEscalaDecimal()
    {
        var conceptoA = new ConceptoPresupuesto
        {
            Id = 1,
            PresupuestoId = 10,
            TipoConceptoId = TipoConcepto.MaterialId,
            Concepto = "Cable",
            MaterialId = 2,
            MaterialProveedorId = 3,
            Cantidad = 10m,
            Unidad = UnidadMedida.Metro,
            PrecioUnitario = 100m,
            Moneda = Moneda.UYU
        };
        var conceptoB = new ConceptoPresupuesto
        {
            Id = 1,
            PresupuestoId = 10,
            TipoConceptoId = TipoConcepto.MaterialId,
            Concepto = "Cable",
            MaterialId = 2,
            MaterialProveedorId = 3,
            Cantidad = 10.0000000000m,
            Unidad = UnidadMedida.Metro,
            PrecioUnitario = 100.0000m,
            Moneda = Moneda.UYU
        };

        string huellaA = CalculadorHuellaBorrador.Calcular([conceptoA], []);
        string huellaB = CalculadorHuellaBorrador.Calcular([conceptoB], []);

        Assert.Equal(huellaA, huellaB);

        conceptoB.PrecioUnitario = 101m;
        Assert.NotEqual(huellaA, CalculadorHuellaBorrador.Calcular([conceptoB], []));
    }

    [Fact]
    public void ValidadorBorrador_RechazaReferenciasDuplicadasYSegundoMargen()
    {
        var duplicado = new BorradorPresupuestoDto
        {
            PresupuestoId = 10,
            Conceptos =
            [
                new ConceptoBorradorDto { Id = 1, TipoConceptoId = TipoConcepto.MaterialId, MaterialId = 1, MaterialProveedorId = 5 },
                new ConceptoBorradorDto
                {
                    Id = -1, TipoConceptoId = TipoConcepto.MaterialId, MaterialId = 1, MaterialProveedorId = 5,
                    Concepto = "Cable", Unidad = UnidadMedida.Metro, Cantidad = 2m, PrecioUnitario = 10m, Moneda = Moneda.UYU, EsNuevo = true
                }
            ]
        };

        var dobleMargen = new BorradorPresupuestoDto
        {
            PresupuestoId = 10,
            Conceptos =
            [
                new ConceptoBorradorDto { Id = 1, TipoConceptoId = TipoConcepto.ManoDeObraId, PrecioUnitario = 100m, Moneda = Moneda.UYU },
                new ConceptoBorradorDto { Id = 2, TipoConceptoId = TipoConcepto.MargenId, PorcentajeMargen = 10m },
                new ConceptoBorradorDto { Id = -1, TipoConceptoId = TipoConcepto.MargenId, PorcentajeMargen = 15m, TiposBaseMargen = [TipoConcepto.ManoDeObraId], EsNuevo = true }
            ]
        };

        Assert.Contains("ya están en el presupuesto", ValidadorBorradorPresupuesto.ValidarBorrador(duplicado));
        Assert.Contains("ya tiene un margen", ValidadorBorradorPresupuesto.ValidarBorrador(dobleMargen));
    }

    [Fact]
    public async Task GuardarConCambiosValidos_PersisteYLimpiaElAlmacen()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios());
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen);

        var resultado = await casoDeUso.EjecutarAsync(10, esperabaCambiosPendientes: true, _usuario, CancellationToken.None);

        Assert.True(resultado.GuardadoCorrectamente);
        Assert.True(resultado.HuboCambiosGuardados);
        Assert.Equal(1, _repositorio.LlamadasGuardar);
        Assert.False(_almacen.TieneCambiosPendientes(10, _usuario.Id));
    }

    [Fact]
    public async Task GuardarSinCambiosPendientesYConPermiso_EsIdempotenteYNoEscribeEnBase()
    {
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen);

        var resultado = await casoDeUso.EjecutarAsync(10, esperabaCambiosPendientes: false, _usuario, CancellationToken.None);

        Assert.True(resultado.GuardadoCorrectamente);
        Assert.False(resultado.HuboCambiosGuardados);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
    }

    [Fact]
    public async Task GuardarSinCambiosPendientesYSinPermiso_DevuelveEdicionNoPermitida()
    {
        _repositorio.PuedeEditar = false;
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen);

        var resultado = await casoDeUso.EjecutarAsync(10, esperabaCambiosPendientes: false, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.EdicionNoPermitida, resultado.Estado);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
    }

    [Fact]
    public async Task SiLaPantallaEsperabaCambiosPeroLaSesionSePerdio_InformaErrorSinSilenciar()
    {
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen);

        var resultado = await casoDeUso.EjecutarAsync(10, esperabaCambiosPendientes: true, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.CambiosPendientesPerdidos, resultado.Estado);
        Assert.Contains("No se encontraron los cambios pendientes", resultado.MensajeError);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
    }

    [Theory]
    [InlineData(ResultadoGuardarBorrador.ConflictoConcurrencia, "modificado desde otra operación")]
    [InlineData(ResultadoGuardarBorrador.ReferenciaInexistente, "ya no existe en el catálogo")]
    [InlineData(ResultadoGuardarBorrador.ReferenciaDuplicada, "ya están en el presupuesto")]
    [InlineData(ResultadoGuardarBorrador.MargenExistente, "ya tiene un margen")]
    [InlineData(ResultadoGuardarBorrador.TiposBaseMargenNoDisponibles, "estén presentes")]
    [InlineData(ResultadoGuardarBorrador.DatosInvalidos, "datos inválidos")]
    public async Task AnteErrorDelRepositorio_ConservaCambiosPendientesEInforma(
        ResultadoGuardarBorrador estadoDevuelto, string fragmentoEsperado)
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios());
        _repositorio.ResultadoGuardar = estadoDevuelto;
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen);

        var resultado = await casoDeUso.EjecutarAsync(10, esperabaCambiosPendientes: true, _usuario, CancellationToken.None);

        Assert.Equal(estadoDevuelto, resultado.Estado);
        Assert.Contains(fragmentoEsperado, resultado.MensajeError);
        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
    }

    [Fact]
    public async Task AnteExcepcionAlPersistir_ConservaLosCambiosPendientesParaReintento()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios());
        _repositorio.ExcepcionAlGuardar = new InvalidOperationException("Error simulado de base de datos.");
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            casoDeUso.EjecutarAsync(10, esperabaCambiosPendientes: true, _usuario, CancellationToken.None));

        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
    }

    private sealed class RepositorioFalso : IRepositorioConceptosPresupuesto
    {
        public bool PuedeEditar { get; set; } = true;
        public ResultadoGuardarBorrador ResultadoGuardar { get; set; } = ResultadoGuardarBorrador.Guardado;
        public Exception? ExcepcionAlGuardar { get; set; }
        public int LlamadasGuardar { get; private set; }

        public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<Presupuesto?>(new Presupuesto { Id = presupuestoId });

        public Task<bool> PuedeEditarEnBaseAsync(int presupuestoId, Usuario usuario, CancellationToken cancellationToken) =>
            Task.FromResult(PuedeEditar);

        public Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(int presupuestoId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(int referenciaId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ResultadoGuardarBorrador> GuardarBorradorSiPuedeEditarAsync(
            BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken)
        {
            LlamadasGuardar++;
            if (ExcepcionAlGuardar != null)
            {
                throw ExcepcionAlGuardar;
            }

            return Task.FromResult(ResultadoGuardar);
        }
    }
}
