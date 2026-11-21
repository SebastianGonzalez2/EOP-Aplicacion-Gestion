using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// S3-T12 (US-010A, RF-20): el usuario ve las referencias de precio del material que la fila ya
// tiene y elige otra de ellas. La fila conserva el mismo material y pasa a usar el proveedor,
// el precio y la moneda de la referencia nueva.
//
// Se parece a SeleccionarMaterialDeConcepto (US-007), pero con una diferencia importante: acá el
// material NO puede cambiar. Si alguien manda una referencia de otro material, se rechaza; para
// cambiar de material está la búsqueda de US-007.
//
// Lo que NO hace, a propósito:
// - No elige nada solo: la persona selecciona expresamente la referencia, aunque haya una sola.
// - No crea ni modifica nada del catálogo: solo lo lee para copiar los datos a la fila.
// - No permite editar a mano el nombre, la unidad, el precio ni la moneda: esos cuatro valores
//   históricos (DT-11) solo se copian de una referencia del catálogo.
// - No toca ConceptoDetectado ni la cantidad de la fila.
public class CambiarProveedorDeConcepto
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;

    public CambiarProveedorDeConcepto(IRepositorioConceptosPresupuesto repositorio)
    {
        _repositorio = repositorio;
    }

    // Devuelve null si se guardó, o el mensaje que hay que mostrarle al usuario.
    public async Task<string?> EjecutarAsync(
        int presupuestoId, int conceptoId, int? referenciaId, Usuario usuario,
        CancellationToken cancellationToken)
    {
        // La validación de la vista no alcanza (RNF-10): el servidor exige una selección expresa.
        if (referenciaId is null or <= 0)
        {
            return "Seleccioná la referencia de proveedor que querés usar.";
        }

        ResultadoSeleccionarMaterial resultado = await _repositorio.CambiarProveedorSiPuedeEditarAsync(
            presupuestoId, conceptoId, referenciaId.Value, usuario, cancellationToken);

        switch (resultado)
        {
            case ResultadoSeleccionarMaterial.Seleccionado:
                return null;
            case ResultadoSeleccionarMaterial.EdicionNoPermitida:
                return "El presupuesto ya no está disponible para edición.";
            case ResultadoSeleccionarMaterial.ConceptoInexistente:
                return "La fila que querés modificar ya no existe en el presupuesto.";
            case ResultadoSeleccionarMaterial.ConceptoNoEsMaterial:
                return "Solo se puede cambiar el proveedor en las filas de tipo Material.";
            case ResultadoSeleccionarMaterial.ConceptoSinMaterial:
                return "La fila todavía no tiene un material. Buscá y seleccioná primero el material.";
            case ResultadoSeleccionarMaterial.ReferenciaInexistente:
                return "La referencia seleccionada no existe. Elegí otra de la lista.";
            case ResultadoSeleccionarMaterial.ReferenciaDeOtroMaterial:
                return "Esa referencia no es del material de la fila. Para cambiar de material usá la búsqueda.";
            case ResultadoSeleccionarMaterial.ReferenciaDuplicada:
                return "Ese material y proveedor ya están en otra fila del presupuesto. Modificá la cantidad de esa fila o seleccioná otra referencia.";
            case ResultadoSeleccionarMaterial.SinCambios:
                return "Esa referencia ya es la seleccionada en la fila. Elegí otra si querés cambiar de proveedor.";
            default:
                throw new InvalidOperationException("El repositorio devolvió un resultado desconocido.");
        }
    }
}
