
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

        // Guarda el material y su primera relación con un proveedor.
        // Devuelve false si el nombre ya lo tenía otro material.
        //
        // S1-T34: el problema que resuelve el try/catch es el mismo que en Clientes, Proveedores y
        // Usuarios. Dos personas registran el mismo material casi al mismo tiempo: las dos pasan la
        // consulta ExistePorNombreNormalizadoAsync y las dos intentan guardar. El índice único deja
        // entrar solo a la primera. Antes, la segunda veía la página de error genérica.
        //
        // El "when" deja atrapar el error solo si SQL Server avisa que se repitió el índice del
        // nombre (ver IndicesUnicos). Cualquier otro problema, por ejemplo un proveedor que ya no
        // existe, sigue de largo hasta el manejo de errores de la aplicación (RNF-12).
        public async Task<bool> AgregarSiNombreDisponibleAsync(Material material,MaterialProveedor primeraRelacion,CancellationToken cancellationToken = default)
        {
            try
            {
                await GuardarMaterialConSuPrimeraRelacionAsync(material,primeraRelacion,cancellationToken);

                return true;
            }
            catch (DbUpdateException excepcion) when (IndicesUnicos.RepitioElIndice(excepcion, IndicesUnicos.MaterialesNombreNormalizado))
            {
                // Para este momento la transacción ya se deshizo, así que en la base no quedó nada.
                // Pero EF sigue teniendo anotado el material como "pendiente de guardar". El contexto
                // dura todo el pedido, así que lo borramos de la lista para que otro SaveChanges no
                // intente insertarlo de nuevo.
                _context.ChangeTracker.Clear();

                return false;
            }
        }

        // Guarda el material y su primera relación
        // con un proveedor en una única transacción.
        private async Task GuardarMaterialConSuPrimeraRelacionAsync(Material material,MaterialProveedor primeraRelacion,CancellationToken cancellationToken)
        {
            // Iniciamos una transacción para evitar que se
            // guarde un material sin su primera relación.
            //
            // Si algo falla antes del CommitAsync, el "await using" libera la transacción sin
            // confirmarla y SQL Server deshace todo lo que se había guardado.
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
