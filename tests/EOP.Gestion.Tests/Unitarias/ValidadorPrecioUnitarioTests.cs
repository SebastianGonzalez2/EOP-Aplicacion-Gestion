using System.ComponentModel.DataAnnotations;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.WebApp.ViewModels.Materiales;

namespace EOP.Gestion.Tests.Unitarias;

// S2-T22: el precio unitario debe entrar en la columna decimal(28,10) (18 enteros y 10 decimales).
public class ValidadorPrecioUnitarioTests
{
    [Theory]
    [InlineData("0,0000000001")]
    [InlineData("1")]
    [InlineData("150,25")]
    [InlineData("1234,5678901234")]
    [InlineData("5,00000000000")]
    [InlineData("999999999999999999")]
    [InlineData("999999999999999999,9999999999")]
    public void PrecioQueEntraEnLaColumna_EsValido(string texto)
    {
        Assert.Null(ValidadorPrecioUnitario.ObtenerError(decimal.Parse(texto, System.Globalization.CultureInfo.GetCultureInfo("es-UY"))));
    }

    [Fact]
    public void ValorMaximo_CoincideConLaColumna()
    {
        Assert.Equal(999999999999999999.9999999999m, ValidadorPrecioUnitario.ValorMaximo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-0,0000000001")]
    public void SinPrecioOCeroONegativo_DiceQueDebeSerMayorQueCero(string? texto)
    {
        decimal? precio = texto == null ? null : decimal.Parse(texto, System.Globalization.CultureInfo.GetCultureInfo("es-UY"));

        Assert.Equal(ValidadorPrecioUnitario.MensajeNoPositivo, ValidadorPrecioUnitario.ObtenerError(precio));
    }

    [Theory]
    [InlineData("1000000000000000000")]
    [InlineData("1000000000000000000,5")]
    [InlineData("79228162514264337593543950335")]
    public void PrecioConMasDe18DigitosEnteros_DiceQueEsDemasiadoGrande(string texto)
    {
        decimal precio = decimal.Parse(texto, System.Globalization.CultureInfo.GetCultureInfo("es-UY"));

        Assert.Equal(ValidadorPrecioUnitario.MensajeDemasiadoGrande, ValidadorPrecioUnitario.ObtenerError(precio));
    }

    [Theory]
    [InlineData("0,00000000001")]
    [InlineData("1,00000000001")]
    [InlineData("99999999999999999,99999999991")]
    public void PrecioConMasDe10Decimales_DiceQueTieneDemasiadosDecimales(string texto)
    {
        decimal precio = decimal.Parse(texto, System.Globalization.CultureInfo.GetCultureInfo("es-UY"));

        Assert.Equal(ValidadorPrecioUnitario.MensajeDemasiadosDecimales, ValidadorPrecioUnitario.ObtenerError(precio));
    }

    // Los tres formularios usan el mismo atributo: si uno se olvida de ponerlo, falla acá.
    public static TheoryData<object> FormulariosConPrecio => new()
    {
        new RegistrarMaterialViewModel { Nombre = "M", Unidad = EOP.Gestion.Core.Entidades.Catalogo.UnidadMedida.Metro, ProveedorId = 1, Moneda = "UYU" },
        new AgregarProveedorMaterialViewModel { MaterialId = 1, ProveedorId = 1, Moneda = "UYU" },
        new EditarReferenciaPrecioViewModel { RelacionId = 1, Moneda = "UYU" }
    };

    private static void AsignarPrecio(object formulario, decimal precio) =>
        formulario.GetType().GetProperty("PrecioUnitario")!.SetValue(formulario, precio);

    private static List<string> ErroresDelPrecio(object formulario)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(formulario, new ValidationContext(formulario), resultados, true);

        return resultados.Where(r => r.MemberNames.Contains("PrecioUnitario")).Select(r => r.ErrorMessage!).ToList();
    }

    [Theory]
    [MemberData(nameof(FormulariosConPrecio))]
    public void Formulario_AceptaElMaximoDeLaColumna(object formulario)
    {
        AsignarPrecio(formulario, ValidadorPrecioUnitario.ValorMaximo);

        Assert.Empty(ErroresDelPrecio(formulario));
    }

    [Theory]
    [MemberData(nameof(FormulariosConPrecio))]
    public void Formulario_RechazaPrecioMasGrandeQueLaColumna(object formulario)
    {
        AsignarPrecio(formulario, decimal.MaxValue);

        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadoGrande], ErroresDelPrecio(formulario));
    }

    [Theory]
    [MemberData(nameof(FormulariosConPrecio))]
    public void Formulario_RechazaPrecioConMasDe10Decimales(object formulario)
    {
        AsignarPrecio(formulario, 1.00000000001m);

        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadosDecimales], ErroresDelPrecio(formulario));
    }

    [Theory]
    [MemberData(nameof(FormulariosConPrecio))]
    public void Formulario_RechazaPrecioCeroONegativo(object formulario)
    {
        AsignarPrecio(formulario, -1m);

        Assert.Equal([ValidadorPrecioUnitario.MensajeNoPositivo], ErroresDelPrecio(formulario));
    }
}
