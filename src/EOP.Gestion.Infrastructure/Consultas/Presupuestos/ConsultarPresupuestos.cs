
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Presupuestos
{
    public class ConsultarPresupuestos
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para consultar la base de datos.
        public ConsultarPresupuestos(EopDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los presupuestos con los datos
        // del cliente y del responsable correspondiente.
        public async Task<List<PresupuestoListadoDto>> EjecutarAsync(CancellationToken cancellationToken = default)
        {
            // Relacionamos los presupuestos con sus clientes
            // y responsables utilizando los identificadores guardados.
            var consulta =from presupuesto in _context.Presupuestos.AsNoTracking()

                join cliente in _context.Clientes.AsNoTracking()
                    on presupuesto.ClienteId equals cliente.Id

                join responsable in _context.Usuarios.AsNoTracking()
                    on presupuesto.ResponsableId equals responsable.Id

                // Mostramos primero los presupuestos más recientes.
                orderby presupuesto.FechaCreacion descending,
                        presupuesto.Id descending

                // Seleccionamos solamente los datos necesarios
                // para mostrar en el listado.
                select new PresupuestoListadoDto
                {
                    Id = presupuesto.Id,
                    Codigo = presupuesto.Codigo,
                    NombreProyecto = presupuesto.NombreProyecto,
                    NombreCliente = cliente.Nombre,
                    FechaCreacion = presupuesto.FechaCreacion,
                    Estado = presupuesto.Estado,
                    NombreResponsable = responsable.NombreCompleto
                };

            // Ejecutamos la consulta y devolvemos el listado.
            return await consulta.ToListAsync(cancellationToken);
        }
    }
}
