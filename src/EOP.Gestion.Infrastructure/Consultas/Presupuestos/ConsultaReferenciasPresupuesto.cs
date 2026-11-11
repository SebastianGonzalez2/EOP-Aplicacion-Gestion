
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Presupuestos
{
    public class ConsultaReferenciasPresupuesto : IConsultaReferenciasPresupuesto
    {
        private readonly EopDbContext _context;

        // Recibimos el contexto para consultar la base de datos.
        public ConsultaReferenciasPresupuesto(EopDbContext context)
        {
            _context = context;
        }

        // Verificamos que el cliente exista y esté activo.
        public async Task<bool> ClienteEstaActivoAsync(int clienteId,CancellationToken cancellationToken = default)
        {
            return await _context.Clientes.AsNoTracking().AnyAsync(cliente => cliente.Id == clienteId && cliente.Activo,cancellationToken);
        }

        // Verificamos que el tipo de proyecto exista.
        public async Task<bool> TipoProyectoExisteAsync(int tipoProyectoId,CancellationToken cancellationToken = default)
        {
            return await _context.TiposProyecto.AsNoTracking().AnyAsync(tipo => tipo.Id == tipoProyectoId,cancellationToken);
        }
    }
}
