
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
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

        // Obtiene los presupuestos aplicando los filtros seleccionados.
        // Si un filtro no se informa, no restringimos por ese dato.
        public async Task<List<PresupuestoListadoDto>> EjecutarAsync(CancellationToken cancellationToken = default,string? codigo = null,
            EstadoPresupuesto? estado = null,int? clienteId = null,DateTime? fecha = null, int? responsableId = null)
        {
            // Relacionamos los presupuestos con sus clientes
            // y responsables utilizando los identificadores guardados.
            var consulta =
                from presupuesto in _context.Presupuestos.AsNoTracking()

                join cliente in _context.Clientes.AsNoTracking()
                    on presupuesto.ClienteId equals cliente.Id

                join responsable in _context.Usuarios.AsNoTracking()
                    on presupuesto.ResponsableId equals responsable.Id

                select new
                {
                    Presupuesto = presupuesto,
                    Cliente = cliente,
                    Responsable = responsable
                };

            // Filtramos por código si el usuario escribió uno.
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                string codigoBuscado = codigo.Trim();

                consulta = consulta.Where(dato =>dato.Presupuesto.Codigo.Contains(codigoBuscado));
            }

            // Filtramos por el estado seleccionado.
            if (estado.HasValue)
            {
                consulta = consulta.Where(dato =>dato.Presupuesto.Estado == estado.Value);
            }

            // Filtramos por el cliente seleccionado.
            if (clienteId.HasValue)
            {
                consulta = consulta.Where(dato =>dato.Presupuesto.ClienteId == clienteId.Value);
            }

            // Comparamos el día completo, sin importar la hora.
            // El usuario elige el día según la hora de Uruguay, pero en la base las fechas están en UTC.
            // Por eso primero pasamos los límites de ese día a UTC: desde el inicio (inclusive)
            // hasta el inicio del día siguiente (exclusive).
            if (fecha.HasValue)
            {
                var intervalo = ZonaHorariaUruguay.ObtenerIntervaloUtcDelDia(fecha.Value);
                DateTime inicioDiaUtc = intervalo.InicioUtc;
                DateTime finDiaUtc = intervalo.FinUtcExclusivo;

                consulta = consulta.Where(dato => dato.Presupuesto.FechaCreacion >= inicioDiaUtc && dato.Presupuesto.FechaCreacion < finDiaUtc);
            }

            // Filtramos por el usuario responsable del presupuesto.
            if (responsableId.HasValue)
            {
                consulta = consulta.Where(dato =>dato.Presupuesto.ResponsableId == responsableId.Value);
            }

            // Ordenamos primero los presupuestos más recientes
            // y seleccionamos solamente los datos del listado.
            return await consulta.OrderByDescending(dato => dato.Presupuesto.FechaCreacion).ThenByDescending(dato => dato.Presupuesto.Id)
                .Select(dato => new PresupuestoListadoDto
                {
                    Id = dato.Presupuesto.Id,
                    Codigo = dato.Presupuesto.Codigo,
                    NombreProyecto = dato.Presupuesto.NombreProyecto,
                    NombreCliente = dato.Cliente.Nombre,
                    FechaCreacion = dato.Presupuesto.FechaCreacion,
                    Estado = dato.Presupuesto.Estado,
                    NombreResponsable = dato.Responsable.NombreCompleto
                })
                .ToListAsync(cancellationToken);
        }
    }
}
