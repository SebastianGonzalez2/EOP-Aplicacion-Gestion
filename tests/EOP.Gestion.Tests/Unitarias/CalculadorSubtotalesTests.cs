
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Tests.Unitarias;

public class CalculadorSubtotalesTests
{
    [Fact]
    public void Material_CalculaCantidadPorPrecioUnitario()
    {
        var concepto = new ConceptoPresupuesto
        {
            TipoConceptoId = TipoConcepto.MaterialId,
            Cantidad = 10m,
            PrecioUnitario = 150m
        };

        decimal? subtotal = CalculadorSubtotales.Calcular(concepto);

        Assert.Equal(1500m, subtotal);
    }

    [Theory]
    [InlineData(TipoConcepto.ManoDeObraId)]
    [InlineData(TipoConcepto.AlquilerId)]
    [InlineData(TipoConcepto.TrasladoId)]
    [InlineData(TipoConcepto.CostosIndirectosId)]
    public void ConceptoManual_UtilizaImporteDirecto(int tipoId)
    {
        var concepto = new ConceptoPresupuesto
        {
            TipoConceptoId = tipoId,
            PrecioUnitario = 2000m
        };

        decimal? subtotal = CalculadorSubtotales.Calcular(concepto);

        Assert.Equal(2000m, subtotal);
    }

    [Fact]
    public void MaterialSinCantidad_NoCalculaSubtotal()
    {
        var concepto = new ConceptoPresupuesto
        {
            TipoConceptoId = TipoConcepto.MaterialId,
            PrecioUnitario = 150m
        };

        Assert.Null(CalculadorSubtotales.Calcular(concepto));
    }

    [Fact]
    public void MaterialSinPrecio_NoCalculaSubtotal()
    {
        var concepto = new ConceptoPresupuesto
        {
            TipoConceptoId = TipoConcepto.MaterialId,
            Cantidad = 10m
        };

        Assert.Null(CalculadorSubtotales.Calcular(concepto));
    }

    [Fact]
    public void Material_ConservaDecimalesSinRedondear()
    {
        var concepto = new ConceptoPresupuesto
        {
            TipoConceptoId = TipoConcepto.MaterialId,
            Cantidad = 1.25m,
            PrecioUnitario = 10.125m
        };

        Assert.Equal(12.65625m, CalculadorSubtotales.Calcular(concepto));
    }

    [Fact]
    public void Margen_NoSeCalculaComoImporteDirecto()
    {
        var concepto = new ConceptoPresupuesto
        {
            TipoConceptoId = TipoConcepto.MargenId,
            PorcentajeMargen = 10m
        };

        Assert.Null(CalculadorSubtotales.Calcular(concepto));
    }

    [Fact]
    public void TipoDesconocido_NoCalculaSubtotal()
    {
        var concepto = new ConceptoPresupuesto
        {
            TipoConceptoId = 999,
            PrecioUnitario = 100m
        };

        Assert.Null(CalculadorSubtotales.Calcular(concepto));
    }
}
