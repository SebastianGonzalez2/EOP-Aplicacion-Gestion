using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Proveedores;

namespace EOP.Gestion.Tests.Compartido;

// Arma proveedores y cuentas completos para las pruebas. Cada prueba cambia solamente el dato
// que quiere comprobar, así, si la base rechaza algo, es por ese dato y no por otro.
internal static class DatosDePruebaProveedores
{
    public static Proveedor CrearProveedor(string nombre = "Proveedor de prueba", string rut = "215521750017")
    {
        var proveedor = new Proveedor
        {
            Nombre = nombre,
            Rut = rut,
            Telefono = "+59899123456",
            CorreoContacto = "contacto@ejemplo.com"
        };

        proveedor.CuentasBancarias.Add(CrearCuenta());

        return proveedor;
    }

    public static CuentaBancaria CrearCuenta(string numeroCuenta = "001234567")
    {
        return new CuentaBancaria
        {
            Banco = Banco.BancoRepublicaOrientalDelUruguay,
            NumeroCuenta = numeroCuenta,
            Titular = "Titular de prueba",
            Moneda = Moneda.UYU
        };
    }
}
