using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using Xunit;

namespace EOP.Gestion.Tests.Unitarias;

public class CalculadorMargenTests
{
    [Fact]
    public void Calcular_BaseEnUYU_CalculaMargenCorrectamente()
    {
        var subtotales = new List<TipoBaseMargenDto>
        {
            new()
            {
                TipoId = TipoConcepto.MaterialId,
                SubtotalUYU = 10000m
            }
        };

        decimal? resultado = CalculadorMargen.Calcular(
            subtotales,
            new[] { TipoConcepto.MaterialId },
            10m,
            null);

        Assert.Equal(1000m, resultado);
    }

    [Fact]
    public void Calcular_BaseEnUSD_ConvierteAntesDeCalcular()
    {
        var subtotales = new List<TipoBaseMargenDto>
        {
            new()
            {
                TipoId = TipoConcepto.MaterialId,
                SubtotalUSD = 200m
            }
        };

        decimal? resultado = CalculadorMargen.Calcular(
            subtotales,
            new[] { TipoConcepto.MaterialId },
            10m,
            40m);

        Assert.Equal(800m, resultado);
    }

    [Fact]
    public void Calcular_BaseConAmbasMonedas_CalculaCorrectamente()
    {
        var subtotales = new List<TipoBaseMargenDto>
        {
            new()
            {
                TipoId = TipoConcepto.MaterialId,
                SubtotalUYU = 10000m,
                SubtotalUSD = 200m
            }
        };

        decimal? resultado = CalculadorMargen.Calcular(
            subtotales,
            new[] { TipoConcepto.MaterialId },
            15m,
            40m);

        Assert.Equal(2700m, resultado);
    }

    [Fact]
    public void Calcular_TipoNoSeleccionado_NoAfectaMargen()
    {
        var subtotales = new List<TipoBaseMargenDto>
        {
            new()
            {
                TipoId = 1,
                SubtotalUYU = 10000m
            },
            new()
            {
                TipoId = 2,
                SubtotalUYU = 5000m
            }
        };

        decimal? resultado = CalculadorMargen.Calcular(
            subtotales,
            new[] { 1 },
            10m,
            null);

        Assert.Equal(1000m, resultado);
    }

    [Fact]
    public void Calcular_CambiaSubtotalSeleccionado_RecalculaMargen()
    {
        var tipo = new TipoBaseMargenDto
        {
            TipoId = TipoConcepto.MaterialId,
            SubtotalUYU = 10000m
        };

        var subtotales = new[] { tipo };
        var seleccionados = new[] { TipoConcepto.MaterialId };

        decimal? margenInicial = CalculadorMargen.Calcular(
            subtotales, seleccionados, 10m, null);

        tipo.SubtotalUYU = 20000m;

        decimal? margenActualizado = CalculadorMargen.Calcular(
            subtotales, seleccionados, 10m, null);

        Assert.Equal(1000m, margenInicial);
        Assert.Equal(2000m, margenActualizado);
    }

    [Fact]
    public void Calcular_BaseEnUSD_SinCotizacion_DevuelveNull()
    {
        var subtotales = new List<TipoBaseMargenDto>
        {
            new()
            {
                TipoId = TipoConcepto.MaterialId,
                SubtotalUSD = 200m
            }
        };

        decimal? resultado = CalculadorMargen.Calcular(
            subtotales,
            new[] { TipoConcepto.MaterialId },
            10m,
            null);

        Assert.Null(resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-40)]
    public void Calcular_CotizacionInvalida_DevuelveNull(
        int cotizacion)
    {
        var subtotales = new List<TipoBaseMargenDto>
        {
            new()
            {
                TipoId = TipoConcepto.MaterialId,
                SubtotalUSD = 200m
            }
        };

        decimal? resultado = CalculadorMargen.Calcular(
            subtotales,
            new[] { TipoConcepto.MaterialId },
            10m,
            cotizacion);

        Assert.Null(resultado);
    }

    [Fact]
    public void Calcular_ImportesConDecimales_NoRedondea()
    {
        var subtotales = new List<TipoBaseMargenDto>
        {
            new()
            {
                TipoId = TipoConcepto.MaterialId,
                SubtotalUSD = 10.123m
            }
        };

        decimal? resultado = CalculadorMargen.Calcular(
            subtotales,
            new[] { TipoConcepto.MaterialId },
            10m,
            40.123m);

        Assert.Equal(40.6165129m, resultado);
    }
}