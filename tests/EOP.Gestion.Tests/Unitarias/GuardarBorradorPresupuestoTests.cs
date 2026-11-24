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
    private readonly SincronizadorBorradorPresupuesto _sincronizador = new();
    private readonly Usuario _usuario = new() { Id = 1, Activo = true, Rol = RolUsuario.Operador };

    private const string BorradorIdInicial = "borrador-inicial-1";

    private static BorradorPresupuestoDto CrearBorradorConCambios(
        int version = 1, string borradorId = BorradorIdInicial) => new()
    {
        PresupuestoId = 10,
        BorradorId = borradorId,
        HuellaEstadoBase = "HUELLA-BASE",
        Version = version,
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
    public async Task GuardarConCambiosValidos_PersisteLimpiaYConfirmaSesion()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios(version: 1));
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);

        var resultado = await casoDeUso.EjecutarAsync(
            10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", BorradorIdInicial), _usuario, CancellationToken.None);

        Assert.True(resultado.GuardadoCorrectamente);
        Assert.True(resultado.HuboCambiosGuardados);
        Assert.Equal(1, _repositorio.LlamadasGuardar);
        Assert.False(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.Equal(1, _almacen.CantidadCargas);
        Assert.Equal(1, _almacen.CantidadConfirmaciones);
        Assert.True(_almacen.CoincideConUltimoGuardadoEnSesion(10, _usuario.Id, "HUELLA-NUEVA"));
        Assert.Equal(0, _sincronizador.CantidadBloqueosActivos);
    }

    [Fact]
    public async Task GuardarSinCambiosPendientesYConPermiso_EsIdempotenteYNoEscribeEnBase()
    {
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);

        var resultado = await casoDeUso.EjecutarAsync(
            10, ContextoEdicionBorrador.Inicial, _usuario, CancellationToken.None);

        Assert.True(resultado.GuardadoCorrectamente);
        Assert.False(resultado.HuboCambiosGuardados);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
    }

    [Fact]
    public async Task GuardarSinCambiosPendientesYSinPermiso_DevuelveEdicionNoPermitida()
    {
        _repositorio.PuedeEditar = false;
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);

        var resultado = await casoDeUso.EjecutarAsync(
            10, ContextoEdicionBorrador.Inicial, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.EdicionNoPermitida, resultado.Estado);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
    }

    [Fact]
    public async Task SiLaPantallaEsperabaCambiosPeroSePerdieron_InformaMensajeExactoSinJergaTecnica()
    {
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);

        var resultado = await casoDeUso.EjecutarAsync(
            10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", BorradorIdInicial), _usuario, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.CambiosPendientesPerdidos, resultado.Estado);
        Assert.Equal(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, resultado.MensajeError);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
    }

    [Fact]
    public async Task SiOtraPestanaActualizoElBorrador_RechazaConMensajeDeOtraPestanaYConservaCambios()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios(version: 2));
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);

        var resultado = await casoDeUso.EjecutarAsync(
            10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", BorradorIdInicial), _usuario, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.BorradorDesactualizado, resultado.Estado);
        Assert.Equal(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, resultado.MensajeError);
        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.Equal(0, _repositorio.LlamadasGuardar);
    }

    [Fact]
    public async Task SiOtraPestanaYaGuardoElBorrador_InformaConflictoDePestanaEnLugarDePerdida()
    {
        _repositorio.HuellaPersistida = "HUELLA-NUEVA";
        _almacen.RegistrarHuellaGuardada(10, _usuario.Id, "HUELLA-NUEVA");
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);

        var resultado = await casoDeUso.EjecutarAsync(
            10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", BorradorIdInicial), _usuario, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.BorradorDesactualizado, resultado.Estado);
        Assert.Equal(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, resultado.MensajeError);
    }

    [Fact]
    public async Task PestanaViejaConMismaVersion1QueUnNuevoBorrador_EsRechazadaAlEditarYAlGuardar()
    {
        // El nuevo borrador también tiene Version = 1, pero posee un BorradorId distinto.
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios(version: 1, borradorId: "borrador-nuevo-2"));
        var contextoViejo = new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", "borrador-viejo-1");

        var agregar = new AgregarConceptoManual(_repositorio, _almacen, _sincronizador);
        string? errorEditar = await agregar.EjecutarAsync(
            10, TipoConcepto.AlquilerId, "No debe entrar", 300m, Moneda.UYU, null, null,
            _usuario, CancellationToken.None, contextoViejo);

        var guardar = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);
        var resultadoGuardar = await guardar.EjecutarAsync(10, contextoViejo, _usuario, CancellationToken.None);

        Assert.Equal(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, errorEditar);
        Assert.Equal(ResultadoGuardarBorrador.BorradorDesactualizado, resultadoGuardar.Estado);
        Assert.Equal(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, resultadoGuardar.MensajeError);
        Assert.Equal(0, _repositorio.LlamadasGuardar);

        // El borrador nuevo permanece intacto.
        BorradorPresupuestoDto? borradorActual = _almacen.Obtener(10, _usuario.Id);
        Assert.NotNull(borradorActual);
        Assert.Equal("borrador-nuevo-2", borradorActual.BorradorId);
        Assert.Single(borradorActual.Conceptos);
    }

    [Theory]
    [InlineData(ResultadoGuardarBorrador.ConflictoConcurrencia, "El presupuesto fue modificado mientras estabas editándolo. Tus cambios pendientes se conservaron.")]
    [InlineData(ResultadoGuardarBorrador.ReferenciaInexistente, "ya no existe en el catálogo")]
    [InlineData(ResultadoGuardarBorrador.ReferenciaDuplicada, "ya están en el presupuesto")]
    [InlineData(ResultadoGuardarBorrador.MargenExistente, "ya tiene un margen")]
    [InlineData(ResultadoGuardarBorrador.TiposBaseMargenNoDisponibles, "estén presentes")]
    [InlineData(ResultadoGuardarBorrador.DatosInvalidos, "datos inválidos")]
    public async Task AnteErrorDelRepositorio_ConservaCambiosPendientesEInforma(
        ResultadoGuardarBorrador estadoDevuelto, string fragmentoEsperado)
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios(version: 1));
        _repositorio.ResultadoGuardar = estadoDevuelto;
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);

        var resultado = await casoDeUso.EjecutarAsync(
            10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", BorradorIdInicial), _usuario, CancellationToken.None);

        Assert.Equal(estadoDevuelto, resultado.Estado);
        Assert.Contains(fragmentoEsperado, resultado.MensajeError);
        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
    }

    [Fact]
    public async Task AnteExcepcionAlPersistir_ConservaLosCambiosPendientesYLiberaElBloqueo()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios(version: 1));
        _repositorio.ExcepcionAlGuardar = new InvalidOperationException("Error simulado de base de datos.");
        var casoDeUso = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            casoDeUso.EjecutarAsync(
                10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", BorradorIdInicial), _usuario, CancellationToken.None));

        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.Equal(0, _sincronizador.CantidadBloqueosActivos);
    }

    [Fact]
    public async Task ModificacionConcurrenteConGuardadoDefinitivo_SeSerializanYLaSegundaDetectaConflicto()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorradorConCambios(version: 1));

        var guardadoInicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var permitirTerminarGuardado = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _repositorio.AlGuardar = async () =>
        {
            guardadoInicio.SetResult();
            await permitirTerminarGuardado.Task;
            _repositorio.HuellaPersistida = "HUELLA-NUEVA";
        };

        var guardar = new GuardarBorradorPresupuesto(_repositorio, _almacen, _sincronizador);
        var agregar = new AgregarConceptoManual(_repositorio, _almacen, _sincronizador);

        Task<ResultadoAccionGuardarBorrador> tareaGuardar = guardar.EjecutarAsync(
            10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", BorradorIdInicial), _usuario, CancellationToken.None);

        // Esperamos a que GuardarBorrador esté dentro del bloqueo antes de lanzar Agregar.
        await guardadoInicio.Task;

        Task<string?> tareaAgregar = agregar.EjecutarAsync(
            10, TipoConcepto.AlquilerId, "Andamio", 500m, Moneda.USD, null, null, _usuario,
            CancellationToken.None, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", BorradorIdInicial));

        await Task.Delay(30);
        Assert.False(tareaAgregar.IsCompleted);

        permitirTerminarGuardado.SetResult();

        var resultadoGuardar = await tareaGuardar;
        string? errorAgregar = await tareaAgregar;

        Assert.True(resultadoGuardar.GuardadoCorrectamente);
        Assert.Equal(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, errorAgregar);
        Assert.False(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.Equal(0, _sincronizador.CantidadBloqueosActivos);
    }

    private sealed class RepositorioFalso : IRepositorioConceptosPresupuesto
    {
        public bool PuedeEditar { get; set; } = true;
        public string HuellaPersistida { get; set; } = "HUELLA-BASE";
        public ResultadoGuardarBorrador ResultadoGuardar { get; set; } = ResultadoGuardarBorrador.Guardado;
        public Exception? ExcepcionAlGuardar { get; set; }
        public Func<Task>? AlGuardar { get; set; }
        public int LlamadasGuardar { get; private set; }

        public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<Presupuesto?>(new Presupuesto { Id = presupuestoId });

        public Task<bool> PuedeEditarEnBaseAsync(int presupuestoId, Usuario usuario, CancellationToken cancellationToken) =>
            Task.FromResult(PuedeEditar);

        public Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<BorradorPresupuestoDto?>(new BorradorPresupuestoDto
            {
                PresupuestoId = presupuestoId,
                HuellaEstadoBase = HuellaPersistida
            });

        public Task<string?> ObtenerHuellaPersistidaAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(HuellaPersistida);

        public Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(int referenciaId, CancellationToken cancellationToken) =>
            Task.FromResult<ReferenciaCatalogoPresupuestoDto?>(null);

        public Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("Alquiler");

        public async Task<ResultadoPersistenciaBorrador> GuardarBorradorSiPuedeEditarAsync(
            BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken)
        {
            LlamadasGuardar++;
            if (AlGuardar != null)
            {
                await AlGuardar();
            }

            if (ExcepcionAlGuardar != null)
            {
                throw ExcepcionAlGuardar;
            }

            return ResultadoGuardar == ResultadoGuardarBorrador.Guardado
                ? new ResultadoPersistenciaBorrador(ResultadoGuardarBorrador.Guardado, "HUELLA-NUEVA")
                : new ResultadoPersistenciaBorrador(ResultadoGuardar);
        }
    }
}
