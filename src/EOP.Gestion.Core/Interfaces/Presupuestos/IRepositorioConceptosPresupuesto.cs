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

    // S3-T12 (US-010A): cambia solo el proveedor de una fila que ya tiene material. La referencia
    // nueva tiene que ser del mismo material que la fila; si no, se rechaza. Vuelve a copiar los
    // cuatro datos históricos (DT-11) y conserva ConceptoDetectado y la cantidad.
    Task<ResultadoSeleccionarMaterial> CambiarProveedorSiPuedeEditarAsync(
        int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
        CancellationToken cancellationToken);
}
