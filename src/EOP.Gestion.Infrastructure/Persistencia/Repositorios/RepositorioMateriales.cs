
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Interfaces.Materiales;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Materiales
{
    public class RepositorioMateriales : IRepositorioMateriales
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto de Entity Framework
        // para poder trabajar con la base de datos.
        public RepositorioMateriales(EopDbContext context)
        {
            _context = context;
        }

        // Comprueba si ya existe un material con el mismo
        // nombre normalizado.
        public async Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado,CancellationToken cancellationToken = default)
        {
            return await _context.Materiales.AnyAsync(material => material.NombreNormalizado == nombreNormalizado,cancellationToken);
        }

        // Guarda el material y su primera relación
        // con un proveedor en una única transacción.
        public async Task AgregarConPrimeraRelacionAsync(Material material,MaterialProveedor primeraRelacion,CancellationToken cancellationToken = default)
        {
            // Iniciamos una transacción para evitar que se
            // guarde un material sin su primera relación.
            await using var transaccion =await _context.Database.BeginTransactionAsync(cancellationToken);

            // Primero guardamos el material para que
            // SQL Server genere su Id.
            _context.Materiales.Add(material);
            await _context.SaveChangesAsync(cancellationToken);

            // Asociamos la relación al material recién creado.
            primeraRelacion.MaterialId = material.Id;

            // Guardamos la relación con el proveedor.
            _context.MaterialProveedores.Add(primeraRelacion);
            await _context.SaveChangesAsync(cancellationToken);

            // Confirmamos la transacción solamente cuando
            // las dos operaciones terminaron correctamente.
            await transaccion.CommitAsync(cancellationToken);
        }
    }
}
