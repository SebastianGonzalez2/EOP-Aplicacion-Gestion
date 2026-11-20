using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Core.Interfaces.Presupuestos;

public interface IRepositorioConceptosPresupuesto
{
    Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken);

    Task<ResultadoGuardarConcepto> AgregarSiPuedeEditarAsync(
        ConceptoPresupuesto concepto, Usuario usuario, CancellationToken cancellationToken);

    // US-007: asocia o sustituye la referencia de material-proveedor de una fila de tipo Material
    // que ya existe. Copia los datos históricos de la nueva referencia (DT-11) y conserva
    // ConceptoDetectado y la cantidad. Verifica el acceso del usuario dentro de la misma transacción.
    Task<ResultadoSeleccionarMaterial> SeleccionarMaterialSiPuedeEditarAsync(
        int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
        CancellationToken cancellationToken);
}
