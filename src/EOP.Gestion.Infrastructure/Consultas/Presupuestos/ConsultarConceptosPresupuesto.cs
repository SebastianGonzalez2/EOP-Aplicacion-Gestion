using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
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
        var conceptosDelPresupuesto = _contexto.ConceptosPresupuesto.AsNoTracking()
            .Where(concepto => concepto.PresupuestoId == presupuestoId);

        return ArmarFilas(conceptosDelPresupuesto).ToListAsync(cancellationToken);
    }

    // US-007: la pantalla de selección de material necesita mostrar una sola fila (su concepto
    // detectado y el material que tiene hoy). Se filtra también por presupuesto para que una
    // fila de otro presupuesto nunca aparezca bajo la URL de este.
    public Task<FilaConceptoDto?> ObtenerFilaAsync(
        int presupuestoId, int conceptoId, CancellationToken cancellationToken)
    {
        var conceptoBuscado = _contexto.ConceptosPresupuesto.AsNoTracking()
            .Where(concepto => concepto.PresupuestoId == presupuestoId && concepto.Id == conceptoId);

        return ArmarFilas(conceptoBuscado).FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<FilaConceptoDto> ArmarFilas(IQueryable<ConceptoPresupuesto> conceptos)
    {
        // Los datos económicos y el nombre del material se leen de la fila histórica.
        // La referencia sirve para identificar al proveedor, que no se copia al concepto.
        return
            from concepto in conceptos
            join tipo in _contexto.TiposConcepto on concepto.TipoConceptoId equals tipo.Id
            orderby concepto.Id
            select new FilaConceptoDto(
                concepto.Id, tipo.Id, tipo.Nombre, concepto.ConceptoDetectado, concepto.Concepto,
                concepto.MaterialProveedorId,
                (from referencia in _contexto.MaterialProveedores
                 join proveedor in _contexto.Proveedores on referencia.ProveedorId equals proveedor.Id
                 where referencia.Id == concepto.MaterialProveedorId
                 select proveedor.Nombre).FirstOrDefault(),
                concepto.Cantidad, concepto.Unidad, concepto.PrecioUnitario, concepto.Moneda);
    }

    public Task<List<TipoConceptoOpcionDto>> ObtenerTiposAsync(CancellationToken cancellationToken)
    {
        return _contexto.TiposConcepto.AsNoTracking()
            .OrderBy(tipo => tipo.Id)
            .Select(tipo => new TipoConceptoOpcionDto(tipo.Id, tipo.Nombre))
            .ToListAsync(cancellationToken);
    }
}
