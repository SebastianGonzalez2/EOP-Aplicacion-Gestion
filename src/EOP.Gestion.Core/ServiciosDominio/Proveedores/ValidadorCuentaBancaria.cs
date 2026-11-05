using EOP.Gestion.Core.Entidades.Proveedores;

namespace EOP.Gestion.Core.ServiciosDominio.Proveedores;

// Las reglas de UNA cuenta bancaria (RF-23): tiene que tener Banco, Número, Titular y Moneda.
// Es un servicio de dominio porque la regla vale siempre, no solo en el alta del Proveedor:
// cuando llegue "Agregar cuenta bancaria" (RF-102) va a pedir exactamente lo mismo.
public static class ValidadorCuentaBancaria
{
    // Devuelve todos los problemas de la cuenta juntos, así el usuario los corrige de una vez
    // y no de a uno. Si la cuenta está bien, la lista vuelve vacía.
    public static List<string> ObtenerErrores(CuentaBancaria cuenta)
    {
        List<string> errores = new();

        // IsDefined mira si el valor es uno de los que tiene el enum. Hace falta porque un enum
        // en C# es un número por dentro y acepta cualquiera: alguien que manipule el formulario
        // puede mandar el banco 99, aunque en el selector no exista. Además, una cuenta a la que
        // no le pusieron banco vale 0, y como las listas arrancan en 1 también cae acá.
        if (!Enum.IsDefined(cuenta.Banco))
        {
            errores.Add("Seleccioná un banco de la lista.");
        }

        if (string.IsNullOrWhiteSpace(cuenta.NumeroCuenta))
        {
            errores.Add("Ingresá el número de cuenta.");
        }

        if (string.IsNullOrWhiteSpace(cuenta.Titular))
        {
            errores.Add("Ingresá el titular de la cuenta.");
        }

        // Lo mismo que con el banco: las únicas monedas posibles son UYU y USD.
        if (!Enum.IsDefined(cuenta.Moneda))
        {
            errores.Add("Seleccioná una moneda válida: UYU o USD.");
        }

        return errores;
    }
}
