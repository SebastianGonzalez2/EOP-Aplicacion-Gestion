
using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Usuarios
{
    public class ConsultarUsuarios
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para poder consultar la base de datos.
        public ConsultarUsuarios(EopDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los usuarios registrados, sin aplicar filtros.
        public async Task<List<UsuarioListadoDto>> EjecutarAsync(CancellationToken cancellationToken = default)
        {
            // Consultamos los usuarios sin modificar sus registros.
            var consulta = _context.Usuarios.AsNoTracking();

            // Ordenamos por nombre y seleccionamos los datos del listado.
            return await consulta.OrderBy(usuario => usuario.NombreCompleto).ThenBy(usuario => usuario.CorreoCorporativo).Select(usuario => new UsuarioListadoDto
                {
                    Id = usuario.Id,
                    NombreCompleto = usuario.NombreCompleto,
                    CorreoCorporativo = usuario.CorreoCorporativo,
                    Rol = usuario.Rol,
                    Activo = usuario.Activo
                })
                .ToListAsync(cancellationToken);
        }
    }
}
