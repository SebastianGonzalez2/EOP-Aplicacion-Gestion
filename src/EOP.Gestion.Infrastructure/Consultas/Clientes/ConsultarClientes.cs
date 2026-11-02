
using EOP.Gestion.Core.DTOs.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Clientes
{
    public class ConsultarClientes
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto de la base de datos.
        public ConsultarClientes(EopDbContext context)
        {
            _context = context;
        }

        // Obtiene los clientes aplicando búsqueda y filtro por estado.
        public async Task<List<ClienteListadoDto>> EjecutarAsync(string? busqueda,bool? activo,CancellationToken cancellationToken = default)
        {
            // Preparamos la consulta sin modificar los registros.
            var consulta = _context.Clientes.AsNoTracking();

            // Si se ingresó un texto, buscamos por nombre o RUT.
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string texto = busqueda.Trim();

                consulta = consulta.Where(c =>c.Nombre.Contains(texto) ||c.Rut.Contains(texto));
            }

            // Si se seleccionó un estado, aplicamos el filtro.
            if (activo.HasValue)
            {
                bool estado = activo.Value;

                consulta = consulta.Where(c => c.Activo == estado);
            }

            // Ordenamos y seleccionamos los datos del listado.
            return await consulta.OrderBy(c => c.Nombre).ThenBy(c => c.Rut).Select(c => new ClienteListadoDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Rut = c.Rut,
                    CorreoContacto = c.CorreoContacto,
                    CorreoFacturacion = c.CorreoFacturacion,
                    NumeroWhatsApp = c.NumeroWhatsApp,
                    Activo = c.Activo
                })
                .ToListAsync(cancellationToken);
        }
    }
}
