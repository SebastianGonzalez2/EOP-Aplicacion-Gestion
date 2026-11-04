
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Proveedores
{
    public class ConsultarProveedoresParaSeleccion
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para consultar la base de datos.
        public ConsultarProveedoresParaSeleccion(EopDbContext context)
        {
            _context = context;
        }

        // Obtenemos los proveedores para mostrarlos en el formulario.
        public async Task<List<ProveedorOpcionDto>> EjecutarAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Proveedores.AsNoTracking().OrderBy(proveedor => proveedor.Nombre).Select(proveedor => new ProveedorOpcionDto{Id = proveedor.Id,Nombre = proveedor.Nombre}).ToListAsync(cancellationToken);
        }
    }
}
