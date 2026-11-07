
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
        //
        // Devuelve true si se guardó. Si otro material con el mismo nombre normalizado se
        // guardó antes (por ejemplo, otra persona lo registró al mismo tiempo), no guarda nada
        // y devuelve false. Cualquier otro problema de la base sí es una excepción.
        Task<bool> AgregarSiNombreDisponibleAsync(Material material,MaterialProveedor primeraRelacion,CancellationToken cancellationToken = default);
    }
}
