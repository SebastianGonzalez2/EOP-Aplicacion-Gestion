using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T07: comprobamos la selección de la base del margen
// sin necesitar una conexión a SQL Server.
public class S3T07MargenTests
{
    [Fact]
    public void AgruparSubtotales_MantieneLasMonedasSeparadas()
    {
        var filas = new List<FilaConceptoDto>
        {
            new(1, TipoConcepto.MaterialId, "Material",
                null, null, null, null, null,
                2m, null, 100m, Moneda.UYU),

            new(2, TipoConcepto.MaterialId, "Material",
                null, null, null, null, null,
                1m, null, 50m, Moneda.USD),

            new(3, TipoConcepto.MargenId, "Margen",
                null, null, null, null, null,
                null, null, null, null)
        };

        var resultado = AgrupadorSubtotalesPorTipo.Agrupar(filas);

        // El margen no aparece como opción para su propia base.
        TipoBaseMargenDto material = Assert.Single(resultado);

        Assert.Equal(TipoConcepto.MaterialId, material.TipoId);
        Assert.Equal(200m, material.SubtotalUYU);
        Assert.Equal(50m, material.SubtotalUSD);
    }

    [Fact]
    public void PorcentajeObligatorio()
    {
        string? error = ValidadorBaseMargen.ObtenerError(
            null, [TipoConcepto.MaterialId]);

        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100000)]
    public void PorcentajeFueraDeRango_SeRechaza(int porcentaje)
    {
        string? error = ValidadorBaseMargen.ObtenerError(
            porcentaje, [TipoConcepto.MaterialId]);

        Assert.NotNull(error);
    }

    [Fact]
    public void PorcentajeConMasDeCuatroDecimales_SeRechaza()
    {
        string? error = ValidadorBaseMargen.ObtenerError(
            10.12345m, [TipoConcepto.MaterialId]);

        Assert.Contains("4 decimales", error);
    }

    [Fact]
    public void NoSeleccionarTipos_SeRechaza()
    {
        string? error = ValidadorBaseMargen.ObtenerError(
            20m, []);

        Assert.Contains("al menos un tipo", error);
    }

    [Fact]
    public void TiposRepetidos_SeRechazan()
    {
        string? error = ValidadorBaseMargen.ObtenerError(
            20m,
            [TipoConcepto.MaterialId, TipoConcepto.MaterialId]);

        Assert.Contains("repetir", error);
    }

    [Fact]
    public void MargenComoBase_SeRechaza()
    {
        string? error = ValidadorBaseMargen.ObtenerError(
            20m, [TipoConcepto.MargenId]);

        Assert.Contains("no puede utilizarse", error);
    }

    [Fact]
    public void TipoDesconocido_SeRechaza()
    {
        string? error = ValidadorBaseMargen.ObtenerError(
            20m, [999]);

        Assert.Contains("tipos de concepto válidos", error);
    }

    [Fact]
    public void PorcentajeYTiposValidos_SeAceptan()
    {
        string? error = ValidadorBaseMargen.ObtenerError(
            20m,
            [TipoConcepto.MaterialId, TipoConcepto.ManoDeObraId]);

        Assert.Null(error);
    }

    [Fact]
    public async Task RegistrarMargenValido_LoAgregaAlBorradorSinPersistirEnBase()
    {
        var repositorio = new RepositorioConceptosFalso
        {
            BorradorBase = new BorradorPresupuestoDto
            {
                PresupuestoId = 10,
                HuellaEstadoBase = "BASE",
                Conceptos =
                [
                    new ConceptoBorradorDto { Id = 1, TipoConceptoId = TipoConcepto.MaterialId, TipoNombre = "Material" },
                    new ConceptoBorradorDto { Id = 2, TipoConceptoId = TipoConcepto.ManoDeObraId, TipoNombre = "Mano de obra" }
                ]
            }
        };
        var almacen = new AlmacenBorradorEnMemoria();
        var casoDeUso = new RegistrarMargen(repositorio, almacen, new SincronizadorBorradorPresupuesto());
        var usuario = new Usuario { Id = 1 };

        string? error = await casoDeUso.EjecutarAsync(
            10,
            20m,
            [TipoConcepto.MaterialId, TipoConcepto.ManoDeObraId],
            usuario,
            CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(0, repositorio.GuardadosEnBase);
        BorradorPresupuestoDto? borrador = almacen.Obtener(10, usuario.Id);
        Assert.NotNull(borrador);
        ConceptoBorradorDto margen = Assert.Single(borrador.Conceptos, c => c.TipoConceptoId == TipoConcepto.MargenId);
        Assert.True(margen.EsNuevo);
        Assert.Equal(20m, margen.PorcentajeMargen);
        Assert.Equal([TipoConcepto.MaterialId, TipoConcepto.ManoDeObraId], margen.TiposBaseMargen);
    }

    [Fact]
    public async Task RegistroInvalido_NoConsultaElRepositorioNiModificaElBorrador()
    {
        var repositorio = new RepositorioConceptosFalso();
        var almacen = new AlmacenBorradorEnMemoria();
        var casoDeUso = new RegistrarMargen(repositorio, almacen, new SincronizadorBorradorPresupuesto());

        string? error = await casoDeUso.EjecutarAsync(
            10,
            -5m,
            [TipoConcepto.MaterialId],
            new Usuario { Id = 1 },
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(0, repositorio.ConsultasPermiso);
        Assert.False(almacen.TieneCambiosPendientes(10, 1));
    }

    [Fact]
    public async Task SinPermiso_SeRechaza()
    {
        var repositorio = new RepositorioConceptosFalso { PuedeEditar = false };
        var almacen = new AlmacenBorradorEnMemoria();
        var casoDeUso = new RegistrarMargen(repositorio, almacen, new SincronizadorBorradorPresupuesto());

        string? error = await casoDeUso.EjecutarAsync(
            10, 20m, [TipoConcepto.MaterialId], new Usuario { Id = 1 }, CancellationToken.None);

        Assert.Contains("no está disponible para edición", error);
    }

    [Fact]
    public async Task SegundoMargenEnElBorrador_SeRechaza()
    {
        var repositorio = new RepositorioConceptosFalso
        {
            BorradorBase = new BorradorPresupuestoDto
            {
                PresupuestoId = 10,
                HuellaEstadoBase = "BASE",
                Conceptos =
                [
                    new ConceptoBorradorDto { Id = 1, TipoConceptoId = TipoConcepto.MaterialId, TipoNombre = "Material" },
                    new ConceptoBorradorDto { Id = 2, TipoConceptoId = TipoConcepto.MargenId, TipoNombre = "Margen", PorcentajeMargen = 10m }
                ]
            }
        };
        var almacen = new AlmacenBorradorEnMemoria();
        var casoDeUso = new RegistrarMargen(repositorio, almacen, new SincronizadorBorradorPresupuesto());

        string? error = await casoDeUso.EjecutarAsync(
            10, 20m, [TipoConcepto.MaterialId], new Usuario { Id = 1 }, CancellationToken.None);

        Assert.Contains("ya tiene un margen", error);
    }

    [Fact]
    public async Task TiposAusentesEnElBorrador_SeRechazan()
    {
        var repositorio = new RepositorioConceptosFalso
        {
            BorradorBase = new BorradorPresupuestoDto
            {
                PresupuestoId = 10,
                HuellaEstadoBase = "BASE",
                Conceptos =
                [
                    new ConceptoBorradorDto { Id = 1, TipoConceptoId = TipoConcepto.ManoDeObraId, TipoNombre = "Mano de obra" }
                ]
            }
        };
        var almacen = new AlmacenBorradorEnMemoria();
        var casoDeUso = new RegistrarMargen(repositorio, almacen, new SincronizadorBorradorPresupuesto());

        string? error = await casoDeUso.EjecutarAsync(
            10, 20m, [TipoConcepto.MaterialId], new Usuario { Id = 1 }, CancellationToken.None);

        Assert.Contains("estén presentes", error);
    }

    // Repositorio falso: permite probar el caso de uso
    // sin modificar la base de datos.
    private class RepositorioConceptosFalso : IRepositorioConceptosPresupuesto
    {
        public bool PuedeEditar { get; set; } = true;
        public int ConsultasPermiso { get; private set; }
        public int GuardadosEnBase { get; private set; }
        public BorradorPresupuestoDto BorradorBase { get; set; } = new() { PresupuestoId = 10, HuellaEstadoBase = "BASE" };

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
            Task.FromResult<ReferenciaCatalogoPresupuestoDto?>(null);

        public Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("Margen");

        public Task<ResultadoPersistenciaBorrador> GuardarBorradorSiPuedeEditarAsync(
            BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken)
        {
            GuardadosEnBase++;
            return Task.FromResult(new ResultadoPersistenciaBorrador(ResultadoGuardarBorrador.Guardado, "NUEVA"));
        }
    }
}
