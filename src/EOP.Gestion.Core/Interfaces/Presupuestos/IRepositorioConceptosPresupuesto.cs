using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Core.Interfaces.Presupuestos;

public interface IRepositorioConceptosPresupuesto
{
    Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken);

    Task<ResultadoGuardarConcepto> AgregarSiPuedeEditarAsync(
        ConceptoPresupuesto concepto, Usuario usuario, CancellationToken cancellationToken);
}
