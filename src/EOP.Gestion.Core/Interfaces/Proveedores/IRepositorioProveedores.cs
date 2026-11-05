
namespace EOP.Gestion.Core.Interfaces.Proveedores
{
    public interface IRepositorioProveedores
    {
        // Comprueba si existe un proveedor con el Id recibido.
        // Devuelve true si existe y false si no existe.
        Task<bool> ExistePorIdAsync(int proveedorId,CancellationToken cancellationToken = default);


    }
}
