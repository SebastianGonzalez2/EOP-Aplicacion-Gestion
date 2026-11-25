using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T15: descarte voluntario, consulta de pendientes del usuario y resolución al cerrar sesión.
public class GestionCambiosPendientesTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly AlmacenBorradorEnMemoria _almacen = new();
    private readonly SincronizadorBorradorPresupuesto _sincronizador = new();
    private readonly Usuario _usuario = new() { Id = 1, Activo = true, Rol = RolUsuario.Operador };
    private readonly Usuario _otroUsuario = new() { Id = 2, Activo = true, Rol = RolUsuario.Operador };

    private static BorradorPresupuestoDto CrearBorrador(int presupuestoId, string borradorId, int version = 1, bool conCambios = true) => new()
    {
        PresupuestoId = presupuestoId,
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
                Concepto = $"Instalación {presupuestoId}",
                PrecioUnitario = 1200m,
                Moneda = Moneda.UYU,
                EsNuevo = conCambios
            }
        ]
    };

    private DescartarCambiosPresupuesto CrearDescartar() => new(_repositorio, _almacen, _sincronizador);

    private GuardarBorradorPresupuesto CrearGuardar() => new(_repositorio, _almacen, _sincronizador);

    private ConsultarCambiosPendientesDelUsuario CrearConsultar() => new(_repositorio, _almacen, _sincronizador);

    private ResolverCambiosPendientesAlCerrarSesion CrearResolver() =>
        new(CrearGuardar(), CrearDescartar(), CrearConsultar(), _almacen, _sincronizador);

    private static List<BorradorEsperadoDto> Esperados(params (int PresupuestoId, string BorradorId, int Version)[] borradores) =>
        borradores.Select(b => new BorradorEsperadoDto(b.PresupuestoId, b.BorradorId, b.Version)).ToList();

    // ---- Descarte voluntario ----

    [Fact]
    public async Task Descartar_ConBorradorVigente_EliminaSoloEseBorradorSinTocarLaBase()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10", version: 3));
        _almacen.Guardar(20, _usuario.Id, CrearBorrador(20, "b-20"));
        _almacen.Guardar(10, _otroUsuario.Id, CrearBorrador(10, "b-otro"));

        var resultado = await CrearDescartar().EjecutarAsync(10, "b-10", 3, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoDescartarCambios.Descartado, resultado.Estado);
        Assert.Equal(DescartarCambiosPresupuesto.MensajeDescartado, resultado.Mensaje);
        Assert.Null(_almacen.Obtener(10, _usuario.Id));
        Assert.True(_almacen.TieneCambiosPendientes(20, _usuario.Id));
        Assert.True(_almacen.TieneCambiosPendientes(10, _otroUsuario.Id));
        Assert.True(_almacen.FueDescartadoEnSesion(10, _usuario.Id, "b-10"));
        Assert.Equal(0, _repositorio.LlamadasGuardar);
        Assert.Equal(1, _almacen.CantidadConfirmaciones);
        Assert.Equal(0, _sincronizador.CantidadBloqueosActivos);
    }

    [Theory]
    [InlineData("b-10", 2)]
    [InlineData("otro-borrador", 3)]
    [InlineData(null, 3)]
    public async Task Descartar_DesdePantallaDesactualizada_NoDescartaNada(string? borradorId, int version)
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10", version: 3));

        var resultado = await CrearDescartar().EjecutarAsync(10, borradorId, version, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoDescartarCambios.BorradorDesactualizado, resultado.Estado);
        Assert.Equal(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, resultado.Mensaje);
        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.Equal(0, _almacen.CantidadConfirmaciones);
    }

    [Fact]
    public async Task Descartar_SinCambiosPendientes_InformaQueNoHabiaNada()
    {
        var resultado = await CrearDescartar().EjecutarAsync(10, null, 0, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoDescartarCambios.SinCambiosPendientes, resultado.Estado);
        Assert.Equal(DescartarCambiosPresupuesto.MensajeSinCambios, resultado.Mensaje);
    }

    [Fact]
    public async Task Descartar_ConBorradorSinCambios_LoLimpiaDeLaSesion()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10", conCambios: false));

        var resultado = await CrearDescartar().EjecutarAsync(10, null, 0, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoDescartarCambios.SinCambiosPendientes, resultado.Estado);
        Assert.Null(_almacen.Obtener(10, _usuario.Id));
    }

    [Fact]
    public async Task Descartar_DespuesDeGuardarEnOtraPestana_AvisaQueYaEstabaGuardado()
    {
        _almacen.RegistrarHuellaGuardada(10, _usuario.Id, "HUELLA-BASE");

        var resultado = await CrearDescartar().EjecutarAsync(10, "b-10", 1, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoDescartarCambios.SinCambiosPendientes, resultado.Estado);
        Assert.Equal(DescartarCambiosPresupuesto.MensajeYaGuardadosEnOtraPestana, resultado.Mensaje);
    }

    [Fact]
    public async Task Descartar_DosVecesDesdeDistintasPestanas_LaSegundaInformaElDescarteAnterior()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        await CrearDescartar().EjecutarAsync(10, "b-10", 1, _usuario, CancellationToken.None);

        var segunda = await CrearDescartar().EjecutarAsync(10, "b-10", 1, _usuario, CancellationToken.None);

        Assert.Equal(ResultadoDescartarCambios.SinCambiosPendientes, segunda.Estado);
        Assert.Equal(MensajesBorradorPresupuesto.CambiosDescartadosEnOtraPestana, segunda.Mensaje);
    }

    [Fact]
    public async Task GuardarDesdePestanaVieja_DespuesDeUnDescarte_NoPersisteYExplicaElDescarte()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        await CrearDescartar().EjecutarAsync(10, "b-10", 1, _usuario, CancellationToken.None);

        var resultado = await CrearGuardar().EjecutarAsync(
            10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", "b-10"), _usuario, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.BorradorDesactualizado, resultado.Estado);
        Assert.Equal(MensajesBorradorPresupuesto.CambiosDescartadosEnOtraPestana, resultado.MensajeError);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
    }

    [Fact]
    public async Task EditarDesdePestanaVieja_DespuesDeUnDescarte_NoIniciaUnBorradorNuevoEnSilencio()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        await CrearDescartar().EjecutarAsync(10, "b-10", 1, _usuario, CancellationToken.None);

        var verificacion = await ControladorEstadoBorrador.PrepararParaEdicionAsync(
            10, _usuario.Id, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", "b-10"),
            _repositorio, _almacen, CancellationToken.None);

        Assert.Null(verificacion.BorradorParaEditar);
        Assert.Equal(ResultadoGuardarBorrador.BorradorDesactualizado, verificacion.ErrorEstado);
        Assert.Equal(MensajesBorradorPresupuesto.CambiosDescartadosEnOtraPestana, verificacion.MensajeError);
    }

    [Fact]
    public async Task PerdidaDeSesion_SinDescarteRegistrado_SigueInformandoPerdida()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _almacen.SimularPerdidaDeSesion();

        var resultado = await CrearGuardar().EjecutarAsync(
            10, new ContextoEdicionBorrador(true, 1, "HUELLA-BASE", "b-10"), _usuario, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.CambiosPendientesPerdidos, resultado.Estado);
    }

    // ---- Consulta de pendientes ----

    [Fact]
    public async Task ConsultarPendientes_DevuelveSoloLosDelUsuarioConCambiosYOrdenados()
    {
        _almacen.Guardar(30, _usuario.Id, CrearBorrador(30, "b-30", version: 2));
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _almacen.Guardar(20, _usuario.Id, CrearBorrador(20, "b-20", conCambios: false));
        _almacen.Guardar(40, _otroUsuario.Id, CrearBorrador(40, "b-40"));

        var pendientes = await CrearConsultar().EjecutarAsync(_usuario.Id, CancellationToken.None);

        Assert.Equal(
            [
                new CambioPendienteDto(10, "PRES-10", "Proyecto 10", "b-10", 1),
                new CambioPendienteDto(30, "PRES-30", "Proyecto 30", "b-30", 2)
            ],
            pendientes);
    }

    [Fact]
    public async Task ConsultarPendientes_PresupuestoInexistente_IgualSeListaParaNoEsconderlo()
    {
        _repositorio.PresupuestosInexistentes.Add(10);
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));

        var pendiente = Assert.Single(await CrearConsultar().EjecutarAsync(_usuario.Id, CancellationToken.None));

        Assert.Equal("Presupuesto 10", pendiente.Codigo);
    }

    // ---- Cierre de sesión ----

    [Fact]
    public async Task PrepararCierre_SinPendientes_VaciaLaSesion()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10", conCambios: false));

        bool puedeCerrar = await CrearResolver().PrepararCierreSinPendientesAsync(_usuario.Id, CancellationToken.None);

        Assert.True(puedeCerrar);
        Assert.Equal(1, _almacen.CantidadVaciados);
        Assert.Null(_almacen.Obtener(10, _usuario.Id));
    }

    [Fact]
    public async Task PrepararCierre_ConPendientes_NoVaciaNada()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));

        bool puedeCerrar = await CrearResolver().PrepararCierreSinPendientesAsync(_usuario.Id, CancellationToken.None);

        Assert.False(puedeCerrar);
        Assert.Equal(0, _almacen.CantidadVaciados);
        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
    }

    [Fact]
    public async Task PrepararCierre_SoloConPendientesDeOtroUsuario_PermiteCerrar()
    {
        _almacen.Guardar(10, _otroUsuario.Id, CrearBorrador(10, "b-otro"));

        Assert.True(await CrearResolver().PrepararCierreSinPendientesAsync(_usuario.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GuardarTodo_ConVariosPresupuestos_GuardaCadaUnoYPermiteCerrar()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _almacen.Guardar(20, _usuario.Id, CrearBorrador(20, "b-20", version: 4));

        var resultado = await CrearResolver().GuardarTodoAsync(
            _usuario, Esperados((10, "b-10", 1), (20, "b-20", 4)), CancellationToken.None);

        Assert.True(resultado.PuedeCerrarSesion);
        Assert.Null(resultado.MensajeGeneral);
        Assert.Equal([10, 20], _repositorio.PresupuestosGuardados);
        Assert.All(resultado.Resultados, r => Assert.True(r.Correcto));
        Assert.Equal(1, _almacen.CantidadVaciados);
    }

    [Fact]
    public async Task GuardarTodo_SiUnoFalla_NoCierraSesionYConservaLosCambiosDelQueFallo()
    {
        _repositorio.ResultadosPorPresupuesto[20] = ResultadoGuardarBorrador.ConflictoConcurrencia;
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _almacen.Guardar(20, _usuario.Id, CrearBorrador(20, "b-20"));

        var resultado = await CrearResolver().GuardarTodoAsync(
            _usuario, Esperados((10, "b-10", 1), (20, "b-20", 1)), CancellationToken.None);

        Assert.False(resultado.PuedeCerrarSesion);
        Assert.Collection(
            resultado.Resultados,
            r => Assert.True(r.Correcto),
            r =>
            {
                Assert.False(r.Correcto);
                Assert.Equal(MensajesBorradorPresupuesto.PresupuestoModificadoConPendientesConservados, r.Mensaje);
            });
        Assert.False(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.True(_almacen.TieneCambiosPendientes(20, _usuario.Id));
        Assert.Equal(0, _almacen.CantidadVaciados);
    }

    [Fact]
    public async Task GuardarTodo_SinPermisoDeEdicion_NoCierraSesionEInformaElMotivo()
    {
        _repositorio.ResultadosPorPresupuesto[10] = ResultadoGuardarBorrador.EdicionNoPermitida;
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));

        var resultado = await CrearResolver().GuardarTodoAsync(
            _usuario, Esperados((10, "b-10", 1)), CancellationToken.None);

        Assert.False(resultado.PuedeCerrarSesion);
        Assert.Equal("El presupuesto ya no está disponible para edición.", Assert.Single(resultado.Resultados).Mensaje);
        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
    }

    [Fact]
    public async Task GuardarTodo_ConErrorInesperado_PropagaLaExcepcionYConservaLosCambios()
    {
        _repositorio.ExcepcionPorPresupuesto[20] = new InvalidOperationException("falla simulada");
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _almacen.Guardar(20, _usuario.Id, CrearBorrador(20, "b-20"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearResolver().GuardarTodoAsync(
            _usuario, Esperados((10, "b-10", 1), (20, "b-20", 1)), CancellationToken.None));

        Assert.False(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.True(_almacen.TieneCambiosPendientes(20, _usuario.Id));
        Assert.Equal(0, _almacen.CantidadVaciados);
        Assert.Equal(0, _sincronizador.CantidadBloqueosActivos);
    }

    public static TheoryData<List<BorradorEsperadoDto>> ListasQueNoCoincidenConLaSesion() => new()
    {
        Esperados((10, "b-10", 1)),                              // falta un presupuesto que apareció después
        Esperados((10, "b-10", 1), (20, "b-20", 2)),             // otra pestaña cambió la versión
        Esperados((10, "b-10", 1), (20, "b-viejo", 1)),          // el borrador fue reemplazado por otro
        Esperados((10, "b-10", 1), (20, "b-20", 1), (30, "b-30", 1)), // uno ya no está pendiente
        Esperados()
    };

    [Theory]
    [MemberData(nameof(ListasQueNoCoincidenConLaSesion))]
    public async Task GuardarTodo_ConListaDesactualizada_NoGuardaNada(List<BorradorEsperadoDto> esperados)
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _almacen.Guardar(20, _usuario.Id, CrearBorrador(20, "b-20"));

        var resultado = await CrearResolver().GuardarTodoAsync(_usuario, esperados, CancellationToken.None);

        Assert.False(resultado.PuedeCerrarSesion);
        Assert.Equal(ResolverCambiosPendientesAlCerrarSesion.MensajeListaDesactualizada, resultado.MensajeGeneral);
        Assert.Empty(resultado.Resultados);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.True(_almacen.TieneCambiosPendientes(20, _usuario.Id));
    }

    [Theory]
    [MemberData(nameof(ListasQueNoCoincidenConLaSesion))]
    public async Task DescartarTodo_ConListaDesactualizada_NoDescartaNada(List<BorradorEsperadoDto> esperados)
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _almacen.Guardar(20, _usuario.Id, CrearBorrador(20, "b-20"));

        var resultado = await CrearResolver().DescartarTodoAsync(_usuario, esperados, CancellationToken.None);

        Assert.False(resultado.PuedeCerrarSesion);
        Assert.Equal(ResolverCambiosPendientesAlCerrarSesion.MensajeListaDesactualizada, resultado.MensajeGeneral);
        Assert.True(_almacen.TieneCambiosPendientes(10, _usuario.Id));
        Assert.True(_almacen.TieneCambiosPendientes(20, _usuario.Id));
        Assert.Equal(0, _almacen.CantidadVaciados);
    }

    [Fact]
    public async Task DescartarTodo_ConListaVigente_DescartaSinEscribirEnLaBaseYPermiteCerrar()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _almacen.Guardar(20, _usuario.Id, CrearBorrador(20, "b-20", version: 2));

        var resultado = await CrearResolver().DescartarTodoAsync(
            _usuario, Esperados((10, "b-10", 1), (20, "b-20", 2)), CancellationToken.None);

        Assert.True(resultado.PuedeCerrarSesion);
        Assert.Equal(2, resultado.Resultados.Count);
        Assert.Equal(0, _repositorio.LlamadasGuardar);
        Assert.Empty(_almacen.ObtenerPendientesDelUsuario(_usuario.Id));
        Assert.Equal(1, _almacen.CantidadVaciados);
    }

    [Fact]
    public async Task GuardarTodo_SiOtraPestanaAgregaUnBorradorMientrasSeGuarda_NoCierraSesion()
    {
        _almacen.Guardar(10, _usuario.Id, CrearBorrador(10, "b-10"));
        _repositorio.AlGuardar = () =>
        {
            _almacen.Guardar(50, _usuario.Id, CrearBorrador(50, "b-50"));
            return Task.CompletedTask;
        };

        var resultado = await CrearResolver().GuardarTodoAsync(
            _usuario, Esperados((10, "b-10", 1)), CancellationToken.None);

        Assert.False(resultado.PuedeCerrarSesion);
        Assert.Equal(ResolverCambiosPendientesAlCerrarSesion.MensajeListaDesactualizada, resultado.MensajeGeneral);
        Assert.True(_almacen.TieneCambiosPendientes(50, _usuario.Id));
        Assert.Equal(0, _almacen.CantidadVaciados);
    }

    private sealed class RepositorioFalso : IRepositorioConceptosPresupuesto
    {
        public Dictionary<int, ResultadoGuardarBorrador> ResultadosPorPresupuesto { get; } = [];
        public Dictionary<int, Exception> ExcepcionPorPresupuesto { get; } = [];
        public HashSet<int> PresupuestosInexistentes { get; } = [];
        public List<int> PresupuestosGuardados { get; } = [];
        public Func<Task>? AlGuardar { get; set; }
        public int LlamadasGuardar { get; private set; }

        public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult(PresupuestosInexistentes.Contains(presupuestoId)
                ? null
                : new Presupuesto { Id = presupuestoId, Codigo = $"PRES-{presupuestoId}", NombreProyecto = $"Proyecto {presupuestoId}" });

        public Task<bool> PuedeEditarEnBaseAsync(int presupuestoId, Usuario usuario, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<BorradorPresupuestoDto?>(new BorradorPresupuestoDto { PresupuestoId = presupuestoId, HuellaEstadoBase = "HUELLA-BASE" });

        public Task<string?> ObtenerHuellaPersistidaAsync(int presupuestoId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("HUELLA-BASE");

        public Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(int referenciaId, CancellationToken cancellationToken) =>
            Task.FromResult<ReferenciaCatalogoPresupuestoDto?>(null);

        public Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("Mano de obra");

        public async Task<ResultadoPersistenciaBorrador> GuardarBorradorSiPuedeEditarAsync(
            BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken)
        {
            LlamadasGuardar++;
            if (AlGuardar != null)
            {
                await AlGuardar();
            }

            if (ExcepcionPorPresupuesto.TryGetValue(borrador.PresupuestoId, out Exception? excepcion))
            {
                throw excepcion;
            }

            if (ResultadosPorPresupuesto.TryGetValue(borrador.PresupuestoId, out ResultadoGuardarBorrador resultado))
            {
                return new ResultadoPersistenciaBorrador(resultado);
            }

            PresupuestosGuardados.Add(borrador.PresupuestoId);
            return new ResultadoPersistenciaBorrador(ResultadoGuardarBorrador.Guardado, $"HUELLA-{borrador.PresupuestoId}");
        }
    }
}
