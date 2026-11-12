
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

        // S2-T19: comprueba si el material ya está registrado en el catálogo.
        Task<bool> ExistePorIdAsync(int materialId,CancellationToken cancellationToken = default);

        // S2-T19: comprueba si el material ya tiene una relación con ese proveedor.
        Task<bool> ExisteRelacionConProveedorAsync(int materialId,int proveedorId,CancellationToken cancellationToken = default);

        // S2-T19: guarda una nueva relación para un material que ya existe.
        //
        // Devuelve true si se guardó. Si otra persona creó la misma relación material-proveedor
        // al mismo tiempo, no guarda nada y devuelve false. Cualquier otro problema de la base
        // sí es una excepción.
        Task<bool> AgregarRelacionSiNoExisteAsync(MaterialProveedor relacion,CancellationToken cancellationToken = default);

        // S2-T20 (US-071): comprueba si OTRO material (distinto al que se está editando)
        // ya usa ese nombre normalizado. El propio material no cuenta como duplicado.
        Task<bool> ExisteOtroConNombreNormalizadoAsync(string nombreNormalizado,int materialId,CancellationToken cancellationToken = default);

        // S2-T20: devuelve el material con seguimiento de cambios de EF Core, o null si no existe.
        // Lo que el caso de uso le modifique se guarda con GuardarEdicionSiNombreDisponibleAsync.
        Task<Material?> ObtenerParaEdicionAsync(int materialId,CancellationToken cancellationToken = default);

        // S2-T20: devuelve las relaciones del material con seguimiento de cambios, para
        // poder actualizarles el precio y la moneda sin recrearlas.
        Task<List<MaterialProveedor>> ObtenerRelacionesParaEdicionAsync(int materialId,CancellationToken cancellationToken = default);

        // S2-T20: guarda de una sola vez los cambios hechos sobre el material y sus relaciones
        // (una única transacción: o se guarda todo o no se guarda nada).
        //
        // Devuelve true si se guardó. Si otro material tomó ese nombre justo antes, no guarda
        // nada y devuelve false. Cualquier otro problema de la base sí es una excepción.
        Task<bool> GuardarEdicionSiNombreDisponibleAsync(CancellationToken cancellationToken = default);
    }
}
