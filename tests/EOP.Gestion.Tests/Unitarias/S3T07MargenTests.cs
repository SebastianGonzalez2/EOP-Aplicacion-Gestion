
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

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
    public async Task RegistrarMargenValido_LlamaAlRepositorio()
    {
        var repositorio = new RepositorioMargenFalso();
        var casoDeUso = new RegistrarMargen(repositorio);
        var usuario = new Usuario { Id = 1 };

        string? error = await casoDeUso.EjecutarAsync(
            10,
            20m,
            [TipoConcepto.MaterialId, TipoConcepto.ManoDeObraId],
            usuario,
            CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(1, repositorio.Llamadas);
        Assert.Equal(10, repositorio.PresupuestoId);
        Assert.Equal(20m, repositorio.Porcentaje);
        Assert.Equal(
            [TipoConcepto.MaterialId, TipoConcepto.ManoDeObraId],
            repositorio.TiposBase);
    }

    [Fact]
    public async Task RegistroInvalido_NoLlamaAlRepositorio()
    {
        var repositorio = new RepositorioMargenFalso();
        var casoDeUso = new RegistrarMargen(repositorio);

        string? error = await casoDeUso.EjecutarAsync(
            10,
            -5m,
            [TipoConcepto.MaterialId],
            new Usuario { Id = 1 },
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(0, repositorio.Llamadas);
    }

    [Theory]
    [InlineData(ResultadoRegistrarMargen.EdicionNoPermitida,
        "no está disponible para edición")]
    [InlineData(ResultadoRegistrarMargen.MargenExistente,
        "ya tiene un margen")]
    [InlineData(ResultadoRegistrarMargen.TiposNoDisponibles,
        "estén presentes")]
    public async Task ErroresDelRepositorio_SeInforman(
        ResultadoRegistrarMargen resultado,
        string mensajeEsperado)
    {
        var repositorio = new RepositorioMargenFalso
        {
            Resultado = resultado
        };

        var casoDeUso = new RegistrarMargen(repositorio);

        string? error = await casoDeUso.EjecutarAsync(
            10,
            20m,
            [TipoConcepto.MaterialId],
            new Usuario { Id = 1 },
            CancellationToken.None);

        Assert.Contains(mensajeEsperado, error);
    }

    // Repositorio falso: permite probar el caso de uso
    // sin modificar la base de datos.
    private class RepositorioMargenFalso : IRepositorioMargenPresupuesto
    {
        public int Llamadas { get; private set; }

        public int PresupuestoId { get; private set; }

        public decimal Porcentaje { get; private set; }

        public List<int> TiposBase { get; private set; } = [];

        public ResultadoRegistrarMargen Resultado { get; set; } =
            ResultadoRegistrarMargen.Guardado;

        public Task<ResultadoRegistrarMargen> RegistrarSiPuedeEditarAsync(
            int presupuestoId,
            decimal porcentaje,
            IReadOnlyCollection<int> tiposBase,
            Usuario usuario,
            CancellationToken cancellationToken)
        {
            Llamadas++;
            PresupuestoId = presupuestoId;
            Porcentaje = porcentaje;
            TiposBase = tiposBase.ToList();

            return Task.FromResult(Resultado);
        }
    }
}
