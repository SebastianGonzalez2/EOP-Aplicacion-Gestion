
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Core.Entidades.Proveedores;

namespace EOP.Gestion.Core.Interfaces.Proveedores;

// Core dice QUÉ necesita saber de los proveedores guardados, e Infrastructure resuelve CÓMO
// preguntárselo a la base. Así Core no se entera de que existe EF Core.
//
// No hay un repositorio aparte para las cuentas bancarias: el Proveedor es el agregado que las
// contiene, y por eso se guardan siempre junto con él.
public interface IRepositorioProveedores
{
    // Recibe el nombre ya normalizado, el que calcula Proveedor.NombreNormalizado.
    Task<bool> ExistePorNombreNormalizadoAsync(
        string nombreNormalizado,
        CancellationToken cancellationToken = default);

    // Recibe el RUT ya normalizado: solo los dígitos, que es como queda guardado.
    Task<bool> ExistePorRutAsync(
        string rutNormalizado,
        CancellationToken cancellationToken = default);

    // Guarda el Proveedor con todas sus cuentas, o no guarda nada. Si la base rechaza el nombre o
    // el RUT por repetidos lo avisa en el resultado; cualquier otra falla sigue siendo una excepción.
    Task<ResultadoAgregarProveedor> AgregarAsync(
        Proveedor proveedor,
        CancellationToken cancellationToken = default);

    // Comprueba si existe un proveedor con el Id recibido.
    // Devuelve true si existe y false si no existe.
    Task<bool> ExistePorIdAsync(
        int proveedorId,
        CancellationToken cancellationToken = default);
}
