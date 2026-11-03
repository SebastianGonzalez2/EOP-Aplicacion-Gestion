using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

public class ProveedorTests
{
    [Theory]
    [InlineData("Fivisa", "FIVISA")]
    [InlineData("fivisa", "FIVISA")]
    [InlineData("   Fivisa   ", "FIVISA")]
    [InlineData("Fivisa    S.A.", "FIVISA S.A.")]
    [InlineData("Fivisa\tS.A.", "FIVISA S.A.")]
    [InlineData("  fIvIsA   s.a.  ", "FIVISA S.A.")]
    [InlineData("Construcciones Pérez", "CONSTRUCCIONES PEREZ")]
    [InlineData("áéíóú ÁÉÍÓÚ", "AEIOU AEIOU")]
    [InlineData("Pérez", "PEREZ")] // la letra y el tilde escritos como dos caracteres
    [InlineData("Peña", "PEÑA")]
    [InlineData("Peña", "PEÑA")] // la ñ escrita como una n y su marca
    [InlineData("Güemes", "GÜEMES")]
    public void Nombre_DebeCalcularElNombreNormalizado(string nombre, string nombreNormalizadoEsperado)
    {
        var proveedor = new Proveedor { Nombre = nombre };

        Assert.Equal(nombreNormalizadoEsperado, proveedor.NombreNormalizado);
    }

    [Fact]
    public void Nombre_DebeConservarseTalComoSeEscribio()
    {
        var proveedor = new Proveedor { Nombre = "  Fivisa   S.A. " };

        // Normalizar es para comparar: lo que escribió el usuario no se toca.
        Assert.Equal("  Fivisa   S.A. ", proveedor.Nombre);
    }

    [Fact]
    public void Nombre_AlCorregirlo_DebeRecalcularElNombreNormalizado()
    {
        var proveedor = new Proveedor { Nombre = "Fivisa" };

        proveedor.Nombre = "Fivisa Uruguay";

        Assert.Equal("FIVISA URUGUAY", proveedor.NombreNormalizado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void Nombre_VacioOSoloEspacios_DebeDejarElNormalizadoVacio(string nombre)
    {
        var proveedor = new Proveedor { Nombre = nombre };

        Assert.Equal(string.Empty, proveedor.NombreNormalizado);
    }

    [Theory]
    [InlineData("Construcciones Pérez", "Construcciones Perez")]
    [InlineData("CONSTRUCCIONES PÉREZ", "construcciones perez")]
    [InlineData("Construcciones Pérez", "Construcciones Pérez")]
    public void Nombre_QueSoloCambiaEnLosTildes_DebeNormalizarseIgual(string unNombre, string otroNombre)
    {
        var unProveedor = new Proveedor { Nombre = unNombre };
        var otroProveedor = new Proveedor { Nombre = otroNombre };

        Assert.Equal(unProveedor.NombreNormalizado, otroProveedor.NombreNormalizado);
    }

    // Se ignoran mayúsculas, espacios innecesarios y tildes, nada más. Estas pruebas cuidan que
    // la normalización no se pase de la raya y una nombres que son distintos.
    [Theory]
    [InlineData("Fi Visa", "Fivisa")]
    [InlineData("Peña", "Pena")]
    [InlineData("Güemes", "Guemes")]
    public void Nombre_DistintosParaLaRegla_NoDebenNormalizarseIgual(string unNombre, string otroNombre)
    {
        var unProveedor = new Proveedor { Nombre = unNombre };
        var otroProveedor = new Proveedor { Nombre = otroNombre };

        Assert.NotEqual(unProveedor.NombreNormalizado, otroProveedor.NombreNormalizado);
    }

    [Fact]
    public void ProveedorNuevo_DebeTenerLaListaDeCuentasVacia()
    {
        var proveedor = new Proveedor();

        Assert.Empty(proveedor.CuentasBancarias);
    }

    [Fact]
    public void CuentasBancarias_DebePermitirVariasCuentasEnElMismoProveedor()
    {
        var proveedor = new Proveedor();
        CuentaBancaria primeraCuenta = DatosDePruebaProveedores.CrearCuenta("111");
        CuentaBancaria segundaCuenta = DatosDePruebaProveedores.CrearCuenta("222");

        proveedor.CuentasBancarias.Add(primeraCuenta);
        proveedor.CuentasBancarias.Add(segundaCuenta);

        // La segunda cuenta se suma a la primera, no la reemplaza.
        Assert.Equal(2, proveedor.CuentasBancarias.Count);
        Assert.Contains(primeraCuenta, proveedor.CuentasBancarias);
        Assert.Contains(segundaCuenta, proveedor.CuentasBancarias);
    }
}
