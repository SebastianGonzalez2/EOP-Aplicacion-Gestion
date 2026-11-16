
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Presupuestos
{
    public class ConsultarPresupuestoDetalle
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para poder consultar la base de datos.
        public ConsultarPresupuestoDetalle(EopDbContext context)
        {
            _context = context;
        }

        // Buscamos un presupuesto por su ID y obtenemos
        // la información necesaria para mostrar su detalle.
        public async Task<PresupuestoDetalleDto?> EjecutarAsync(int presupuestoId,CancellationToken cancellationToken = default)
        {
            // Relacionamos el presupuesto con su cliente,
            // tipo de proyecto y usuario responsable.
            var consulta =from presupuesto in _context.Presupuestos.AsNoTracking()

                join cliente in _context.Clientes.AsNoTracking()
                    on presupuesto.ClienteId equals cliente.Id

                join tipoProyecto in _context.TiposProyecto.AsNoTracking()
                    on presupuesto.TipoProyectoId equals tipoProyecto.Id

                join responsable in _context.Usuarios.AsNoTracking()
                    on presupuesto.ResponsableId equals responsable.Id

                where presupuesto.Id == presupuestoId

                select new PresupuestoDetalleDto
                {
                    Id = presupuesto.Id,
                    Codigo = presupuesto.Codigo,
                    NombreProyecto = presupuesto.NombreProyecto,
                    NombreCliente = cliente.Nombre,
                    NombreTipoProyecto = tipoProyecto.Nombre,
                    NombreResponsable = responsable.NombreCompleto,
                    FechaCreacion = presupuesto.FechaCreacion,
                    Estado = presupuesto.Estado,
                    PlazoVigenciaDias = presupuesto.PlazoVigenciaDias
                };

            // Devolvemos el presupuesto encontrado.
            // Si no existe, devolvemos null.
            return await consulta.FirstOrDefaultAsync(cancellationToken);
        }
    }
}
