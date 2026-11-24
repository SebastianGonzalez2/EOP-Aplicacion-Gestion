using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// S3-T12 (US-010A, RF-020): cuando el catálogo cambió el nombre, la unidad, el precio o la moneda de
// la referencia que la fila ya tiene, el presupuesto NO se actualiza solo (DT-11). Si el usuario
// quiere incorporar los valores nuevos, tiene que pedirlo y confirmarlo expresamente. Este caso de
// uso es esa acción: vuelve a copiar a la fila los cuatro valores vigentes de la MISMA referencia.
//
// Lo que NO hace, a propósito:
// - No actualiza nada sin confirmación explícita.
// - No cambia el material ni el proveedor de la fila (para eso están US-007 y el cambio de proveedor).
// - No toca ConceptoDetectado ni la cantidad.
// - No modifica el catálogo: solo lo lee para copiar los datos a la fila.
public class ActualizarReferenciaDeConcepto
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;

    public ActualizarReferenciaDeConcepto(IRepositorioConceptosPresupuesto repositorio)
    {
        _repositorio = repositorio;
    }

    // Devuelve null si se actualizó, o el mensaje que hay que mostrarle al usuario.
    public async Task<string?> EjecutarAsync(
        int presupuestoId, int conceptoId, bool confirmado, Usuario usuario,
        CancellationToken cancellationToken)
    {
        // La confirmación se exige en el servidor (RNF-10): el diálogo de la pantalla es una
        // comodidad, pero una solicitud sin confirmar nunca debe pisar los valores históricos.
        if (!confirmado)
        {
            return "Confirmá la actualización para reemplazar los valores de la fila por los del catálogo.";
        }

        ResultadoSeleccionarMaterial resultado = await _repositorio.ActualizarReferenciaActualSiPuedeEditarAsync(
            presupuestoId, conceptoId, usuario, cancellationToken);

        switch (resultado)
        {
            case ResultadoSeleccionarMaterial.Seleccionado:
                return null;
            case ResultadoSeleccionarMaterial.EdicionNoPermitida:
                return "El presupuesto ya no está disponible para edición.";
            case ResultadoSeleccionarMaterial.ConceptoInexistente:
                return "La fila que querés modificar ya no existe en el presupuesto.";
            case ResultadoSeleccionarMaterial.ConceptoNoEsMaterial:
                return "Solo se pueden actualizar los valores de las filas de tipo Material.";
            case ResultadoSeleccionarMaterial.ConceptoSinMaterial:
                return "La fila todavía no tiene un material. Buscá y seleccioná primero el material.";
            case ResultadoSeleccionarMaterial.ReferenciaInexistente:
                return "La referencia de la fila ya no existe en el catálogo. Elegí otra referencia del material.";
            case ResultadoSeleccionarMaterial.SinCambios:
                return "Los valores de la fila ya coinciden con los del catálogo. No hay nada que actualizar.";
            default:
                throw new InvalidOperationException("El repositorio devolvió un resultado desconocido.");
        }
    }
}
