using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia;

// S1-T34: todos los repositorios reconocen de la misma forma un valor repetido al guardar.
//
// El problema que resuelve: dos personas registran el mismo dato casi al mismo tiempo. Las dos
// preguntan "¿ya existe?", a las dos les dice que no y las dos intentan guardar. Esa consulta
// previa no alcanza para frenarlas: solo el índice único de la base garantiza que entre una sola.
// La otra recibe un error de SQL Server, y el repositorio lo convierte en el mismo mensaje de
// "ya está registrado" que el usuario vería en el caso normal, en vez de la página de error.
//
// Para reconocer ese error miramos dos cosas, y las dos tienen que coincidir:
//   1. El código: SQL Server avisa que se repitió un valor único.
//   2. El nombre del índice que se repitió.
// Así nunca informamos otro problema (la base caída, una restricción CHECK, un proveedor que no
// existe) como si fuera un duplicado. Y cuando una tabla tiene más de un campo único, como
// Proveedores con el nombre y el RUT, sabemos cuál de los dos se repitió.
//
// Los nombres de los índices están todos acá. EF Core los arma a partir de la configuración de
// cada entidad: si alguien cambia esa configuración y el nombre deja de coincidir,
// IndicesUnicosTests falla y avisa.
public static class IndicesUnicos
{
    // Los códigos con los que SQL Server avisa que se repitió un valor único: 2601 si lo que frenó
    // el dato es un índice único y 2627 si es una restricción UNIQUE. Hoy tenemos índices, pero
    // miramos los dos porque para nosotros significan exactamente lo mismo.
    private const int ErrorIndiceUnicoRepetido = 2601;
    private const int ErrorRestriccionUnicaRepetida = 2627;

    public const string ClientesRut = "IX_Clientes_Rut";

    public const string ProveedoresNombreNormalizado = "IX_Proveedores_NombreNormalizado";

    public const string ProveedoresRut = "IX_Proveedores_Rut";

    public const string MaterialesNombreNormalizado = "IX_Materiales_NombreNormalizado";

    // S2-T19: un material no puede tener dos relaciones con el mismo proveedor.
    public const string MaterialProveedoresMaterialIdProveedorId = "IX_MaterialProveedores_MaterialId_ProveedorId";

    public const string UsuariosCorreoCorporativo = "IX_Usuarios_CorreoCorporativo";

    // Devuelve true solo si el guardado falló porque se repitió un valor en ese índice.
    //
    // El nombre del índice solo se puede sacar del texto del mensaje, porque SQL Server no lo da
    // aparte. Lo usamos únicamente para esta comparación: ese mensaje trae el valor repetido, así
    // que no se muestra ni se registra en ningún lado.
    //
    // SQL Server escribe el nombre entre comillas simples ('IX_Clientes_Rut'). Buscamos las
    // comillas también, para que un índice no se confunda con otro cuyo nombre empiece igual.
    public static bool RepitioElIndice(DbUpdateException excepcion, string nombreDelIndice)
    {
        if (excepcion.InnerException is not SqlException errorDeSqlServer)
        {
            return false;
        }

        if (errorDeSqlServer.Number != ErrorIndiceUnicoRepetido &&
            errorDeSqlServer.Number != ErrorRestriccionUnicaRepetida)
        {
            return false;
        }

        string nombreEntreComillas = "'" + nombreDelIndice + "'";

        return errorDeSqlServer.Message.Contains(nombreEntreComillas, StringComparison.Ordinal);
    }
}
