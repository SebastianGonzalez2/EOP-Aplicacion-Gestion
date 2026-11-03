using EOP.Gestion.Core.Comun;

namespace EOP.Gestion.Core.Entidades.Proveedores;

public class CuentaBancaria
{
    public int Id { get; set; }

    // La clave foránea: guarda el Id del Proveedor dueño de esta cuenta. Es int y no int?,
    // así que una cuenta no puede existir sin su Proveedor.
    public int ProveedorId { get; set; }

    public Banco Banco { get; set; }

    // Es texto y no número porque un número de cuenta no se usa para hacer cuentas, puede
    // empezar con ceros y puede traer guiones.
    public string NumeroCuenta { get; set; } = string.Empty;

    public string Titular { get; set; } = string.Empty;

    public Moneda Moneda { get; set; }
}
