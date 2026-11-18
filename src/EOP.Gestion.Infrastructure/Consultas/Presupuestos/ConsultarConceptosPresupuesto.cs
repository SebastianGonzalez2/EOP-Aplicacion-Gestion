using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Consultas.Presupuestos;

public class ConsultarConceptosPresupuesto
{
    private readonly EopDbContext _contexto;

    public ConsultarConceptosPresupuesto(EopDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<List<FilaConceptoDto>> ObtenerFilasAsync(int presupuestoId, CancellationToken cancellationToken)
    {
        // Los datos económicos y el nombre del material se leen de la fila histórica.
        // La referencia sirve para identificar al proveedor, que no se copia al concepto.
        return (
            from concepto in _contexto.ConceptosPresupuesto.AsNoTracking()
            join tipo in _contexto.TiposConcepto on concepto.TipoConceptoId equals tipo.Id
            where concepto.PresupuestoId == presupuestoId
            orderby concepto.Id
            select new FilaConceptoDto(
                tipo.Id, tipo.Nombre, concepto.ConceptoDetectado, concepto.Concepto,
                (from referencia in _contexto.MaterialProveedores
                 join proveedor in _contexto.Proveedores on referencia.ProveedorId equals proveedor.Id
                 where referencia.Id == concepto.MaterialProveedorId
                 select proveedor.Nombre).FirstOrDefault(),
                concepto.Cantidad, concepto.Unidad, concepto.PrecioUnitario, concepto.Moneda)
        ).ToListAsync(cancellationToken);
    }

    public Task<List<TipoConceptoOpcionDto>> ObtenerTiposAsync(CancellationToken cancellationToken)
    {
        return _contexto.TiposConcepto.AsNoTracking()
            .OrderBy(tipo => tipo.Id)
            .Select(tipo => new TipoConceptoOpcionDto(tipo.Id, tipo.Nombre))
            .ToListAsync(cancellationToken);
    }
}
