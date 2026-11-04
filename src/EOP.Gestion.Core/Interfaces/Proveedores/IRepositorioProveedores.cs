namespace EOP.Gestion.Core.Interfaces.Proveedores;

// Core dice QUÉ necesita saber de los proveedores guardados, e Infrastructure resuelve CÓMO
// preguntárselo a la base. Así Core no se entera de que existe EF Core.
//
// Por ahora son solo consultas: guardar proveedores (AgregarAsync) llega con S2-T13.
public interface IRepositorioProveedores
{
    // Recibe el nombre ya normalizado, el que calcula Proveedor.NombreNormalizado.
    Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default);

    // Recibe el RUT ya normalizado: solo los dígitos, que es como queda guardado.
    Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default);
}
