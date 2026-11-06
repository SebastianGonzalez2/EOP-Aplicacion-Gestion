
using EOP.Gestion.Core.Entidades.Catalogo;

namespace EOP.Gestion.Core.Interfaces.Materiales
{
    public interface IRepositorioMateriales
    {
        // Comprueba si ya existe un material con el mismo
        // nombre normalizado.
        Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado,CancellationToken cancellationToken = default);

        // Guarda el material y su primera relación con un
        // proveedor como una única operación.
        // Si falla alguna parte, no debe quedar nada guardado.
        Task AgregarConPrimeraRelacionAsync(Material material,MaterialProveedor primeraRelacion,CancellationToken cancellationToken = default);
    }
}
