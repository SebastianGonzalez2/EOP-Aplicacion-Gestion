using System.ComponentModel.DataAnnotations;
using System.Reflection;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Proveedores;

namespace EOP.Gestion.Tests.Unitarias;

// Las listas de RF-23 son cerradas. Estas pruebas fallan si alguien agrega, saca o renombra
// una opción, o si le cambia el número (que es lo que queda guardado en la base).
public class BancoYMonedaTests
{
    [Theory]
    [InlineData(Banco.BancoRepublicaOrientalDelUruguay, 1, "Banco de la República Oriental del Uruguay (BROU)")]
    [InlineData(Banco.BancoHipotecarioDelUruguay, 2, "Banco Hipotecario del Uruguay (BHU)")]
    [InlineData(Banco.BancoBandesUruguay, 3, "Banco Bandes Uruguay S.A.")]
    [InlineData(Banco.BancoBilbaoVizcayaArgentariaUruguay, 4, "Banco Bilbao Vizcaya Argentaria Uruguay S.A. (BBVA)")]
    [InlineData(Banco.BancoBtgPactualUruguay, 5, "Banco BTG Pactual Uruguay S.A.")]
    [InlineData(Banco.BancoDeLaNacionArgentina, 6, "Banco de la Nación Argentina — Sucursal Uruguay")]
    [InlineData(Banco.BancoItauUruguay, 7, "Banco Itaú Uruguay S.A.")]
    [InlineData(Banco.BancoSantander, 8, "Banco Santander S.A.")]
    [InlineData(Banco.BanqueHeritageUruguay, 9, "Banque Heritage (Uruguay) S.A.")]
    [InlineData(Banco.CitibankSucursalUruguay, 10, "Citibank N.A. — Sucursal Uruguay")]
    [InlineData(Banco.ScotiabankUruguay, 11, "Scotiabank Uruguay S.A.")]
    public void Banco_DebeTenerElNumeroYElNombreDelRequerimiento(Banco banco, int numeroEsperado, string nombreEsperado)
    {
        Assert.Equal(numeroEsperado, (int)banco);
        Assert.Equal(nombreEsperado, ObtenerNombreVisible(banco));
    }

    [Theory]
    [InlineData(Moneda.UYU, 1, "UYU — Pesos uruguayos")]
    [InlineData(Moneda.USD, 2, "USD — Dólares estadounidenses")]
    public void Moneda_DebeTenerElNumeroYElNombreDelRequerimiento(Moneda moneda, int numeroEsperado, string nombreEsperado)
    {
        Assert.Equal(numeroEsperado, (int)moneda);
        Assert.Equal(nombreEsperado, ObtenerNombreVisible(moneda));
    }

    [Fact]
    public void Banco_DebeTenerSoloLasOnceOpcionesDelRequerimiento()
    {
        Assert.Equal(11, Enum.GetValues<Banco>().Length);
    }

    [Fact]
    public void Moneda_DebeTenerSoloLasDosOpcionesDelRequerimiento()
    {
        Assert.Equal(2, Enum.GetValues<Moneda>().Length);
    }

    // Que ningún valor sea 0 es lo que permite detectar un banco o una moneda sin asignar.
    [Fact]
    public void ValorSinAsignar_NoDebeCorresponderANingunaOpcion()
    {
        Assert.False(Enum.IsDefined(default(Banco)));
        Assert.False(Enum.IsDefined(default(Moneda)));
    }

    private static string? ObtenerNombreVisible(Enum opcion)
    {
        FieldInfo? campo = opcion.GetType().GetField(opcion.ToString());
        Assert.NotNull(campo);

        DisplayAttribute? atributo = campo.GetCustomAttribute<DisplayAttribute>();
        Assert.NotNull(atributo);

        return atributo.Name;
    }
}
