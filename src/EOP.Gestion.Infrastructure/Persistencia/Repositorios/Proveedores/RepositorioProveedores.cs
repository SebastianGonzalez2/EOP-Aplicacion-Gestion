
using EOP.Gestion.Core.Interfaces.Proveedores;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores
{
    public class RepositorioProveedores : IRepositorioProveedores
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para poder consultar los
        // proveedores registrados en la base de datos.
        public RepositorioProveedores(EopDbContext context)
        {
            _context = context;
        }

        // Comprueba si existe un proveedor con el Id recibido.
        public async Task<bool> ExistePorIdAsync(int proveedorId,CancellationToken cancellationToken = default)
        {
            return await _context.Proveedores.AnyAsync(proveedor => proveedor.Id == proveedorId,cancellationToken);
        }
    }
}
