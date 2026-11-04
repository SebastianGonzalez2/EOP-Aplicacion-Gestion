using EOP.Gestion.Core.Interfaces.Proveedores;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;

public class RepositorioProveedores : IRepositorioProveedores
{
    private readonly EopDbContext _context;

    public RepositorioProveedores(EopDbContext context)
    {
        _context = context;
    }

    // AnyAsync le pregunta a la base "¿hay al menos uno?" y corta en cuanto encuentra el primero.
    // No trae ningún proveedor a memoria, así que tampoco hay nada que se pueda modificar sin querer.
    public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default)
    {
        return _context.Proveedores.AnyAsync(proveedor => proveedor.NombreNormalizado == nombreNormalizado, cancellationToken);
    }

    public Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default)
    {
        return _context.Proveedores.AnyAsync(proveedor => proveedor.Rut == rutNormalizado, cancellationToken);
    }
}
