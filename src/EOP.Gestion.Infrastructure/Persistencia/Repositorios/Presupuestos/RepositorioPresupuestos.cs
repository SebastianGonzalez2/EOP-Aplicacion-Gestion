using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos
{
    public class RepositorioPresupuestos : IRepositorioPresupuestos
    {
        private readonly EopDbContext _context;

        public RepositorioPresupuestos(EopDbContext context)
        {
            _context = context;
        }

        public async Task AgregarConCodigoAsignadoAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default)
        {
            // Pedimos el correlativo a la base y no lo calculamos acá (por ejemplo, con el máximo
            // + 1), porque dos personas creando un presupuesto a la vez leerían el mismo máximo y
            // les tocaría el mismo número. La secuencia de SQL Server entrega cada número una sola
            // vez, aunque haya muchos pedidos simultáneos.
            int correlativo = await ObtenerSiguienteCorrelativoAsync(cancellationToken);

            presupuesto.NumeroCorrelativo = correlativo;
            // FechaCreacion está en UTC; el generador la pasa a hora de Uruguay para el AAAAMMDD.
            presupuesto.Codigo = GeneradorCodigoPresupuesto.GenerarDesdeFechaUtc(
                presupuesto.FechaCreacion,
                presupuesto.NombreProyecto,
                correlativo);

            _context.Presupuestos.Add(presupuesto);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Una secuencia no se deshace con una transacción: si el guardado falla después de pedir
        // el número (por ejemplo, el cliente no existe), ese número se pierde y el siguiente
        // presupuesto sale con el que sigue. Es el comportamiento normal de SQL Server y los
        // requerimientos no piden una numeración sin saltos. A cambio, nunca se repite un número.
        private async Task<int> ObtenerSiguienteCorrelativoAsync(CancellationToken cancellationToken)
        {
            // Usamos ToListAsync y no SingleAsync porque SingleAsync haría que EF envolviera la
            // consulta en otra, y SQL Server no permite NEXT VALUE FOR dentro de una subconsulta.
            List<int> resultado = await _context.Database
                .SqlQueryRaw<int>("SELECT NEXT VALUE FOR [" + EopDbContext.NombreSecuenciaPresupuestos + "] AS [Value]")
                .ToListAsync(cancellationToken);

            return resultado.Single();
        }
    }
}
