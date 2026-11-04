using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.ServiciosDominio.Proveedores;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

public class ValidadorCuentaBancariaTests
{
    [Fact]
    public void ObtenerErrores_ConCuentaCompleta_NoDebeDevolverErrores()
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();

        Assert.Empty(ValidadorCuentaBancaria.ObtenerErrores(cuenta));
    }

    // Recorremos los enums enteros para que, si algún día se suma un banco, siga siendo aceptado.
    [Fact]
    public void ObtenerErrores_DebeAceptarTodosLosBancosYMonedasDeLasListas()
    {
        foreach (Banco banco in Enum.GetValues<Banco>())
        {
            foreach (Moneda moneda in Enum.GetValues<Moneda>())
            {
                CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
                cuenta.Banco = banco;
                cuenta.Moneda = moneda;

                Assert.Empty(ValidadorCuentaBancaria.ObtenerErrores(cuenta));
            }
        }
    }

    // El 0 es una cuenta sin banco. Los demás son lo que llegaría si alguien manipula el formulario.
    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(99)]
    [InlineData(-1)]
    public void ObtenerErrores_ConBancoFueraDeLaLista_DebeRechazarlo(int numeroBanco)
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.Banco = (Banco)numeroBanco;

        Assert.Equal(["Seleccioná un banco de la lista."], ValidadorCuentaBancaria.ObtenerErrores(cuenta));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public void ObtenerErrores_ConMonedaFueraDeLaLista_DebeRechazarla(int numeroMoneda)
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.Moneda = (Moneda)numeroMoneda;

        Assert.Equal(["Seleccioná una moneda válida: UYU o USD."], ValidadorCuentaBancaria.ObtenerErrores(cuenta));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ObtenerErrores_SinNumeroDeCuenta_DebeRechazarla(string? numeroCuenta)
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.NumeroCuenta = numeroCuenta!;

        Assert.Equal(["Ingresá el número de cuenta."], ValidadorCuentaBancaria.ObtenerErrores(cuenta));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ObtenerErrores_SinTitular_DebeRechazarla(string? titular)
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.Titular = titular!;

        Assert.Equal(["Ingresá el titular de la cuenta."], ValidadorCuentaBancaria.ObtenerErrores(cuenta));
    }

    [Fact]
    public void ObtenerErrores_ConCuentaVacia_DebeInformarLosCuatroDatos()
    {
        List<string> errores = ValidadorCuentaBancaria.ObtenerErrores(new CuentaBancaria());

        Assert.Equal(4, errores.Count);
    }
}
