
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Presupuestos
{
    public class ConsultarTiposProyectoParaSeleccion
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para consultar la base de datos.
        public ConsultarTiposProyectoParaSeleccion(EopDbContext context)
        {
            _context = context;
        }

        // Obtiene los tipos de proyecto disponibles para el formulario.
        public async Task<List<TipoProyectoOpcionDto>> EjecutarAsync(CancellationToken cancellationToken = default)
        {
            return await _context.TiposProyecto.AsNoTracking().OrderBy(tipo => tipo.Nombre).Select(tipo => new TipoProyectoOpcionDto
                {
                    Id = tipo.Id,
                    Nombre = tipo.Nombre
                })
                .ToListAsync(cancellationToken);
        }
    }
}
