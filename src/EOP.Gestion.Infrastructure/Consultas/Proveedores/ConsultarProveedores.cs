
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Proveedores
{
    public class ConsultarProveedores
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para consultar la base de datos.
        public ConsultarProveedores(EopDbContext context)
        {
            _context = context;
        }

        // Obtenemos todos los proveedores para mostrarlos en el listado.
        public async Task<List<ProveedorListadoDto>> EjecutarAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Proveedores.AsNoTracking().OrderBy(proveedor => proveedor.Nombre).Select(proveedor => new ProveedorListadoDto
                {
                    Id = proveedor.Id,
                    Nombre = proveedor.Nombre,
                    Rut = proveedor.Rut,
                    Telefono = proveedor.Telefono,
                    CorreoContacto = proveedor.CorreoContacto,
                    CorreoFacturacion = proveedor.CorreoFacturacion
                }).ToListAsync(cancellationToken);
        }
    }
}
