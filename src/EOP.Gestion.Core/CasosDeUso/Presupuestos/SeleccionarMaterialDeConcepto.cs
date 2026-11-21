using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// US-007 (RF-12, RF-13): el usuario elige a mano un material del catálogo para una fila de
// tipo Material que ya existe en el presupuesto. Sirve tanto para vincular una fila que todavía
// no tiene material (las que va a generar el análisis con IA de US-006) como para sustituir
// el material que la fila ya tenía.
//
// Lo que NO hace, a propósito:
// - No busca ni propone materiales: la persona elige expresamente una referencia.
// - No crea ni modifica nada del catálogo: solo lo lee para copiar los datos a la fila.
// - No toca ConceptoDetectado ni la cantidad de la fila.
public class SeleccionarMaterialDeConcepto
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;

    public SeleccionarMaterialDeConcepto(IRepositorioConceptosPresupuesto repositorio)
    {
        _repositorio = repositorio;
    }

    // Devuelve null si se guardó, o el mensaje que hay que mostrarle al usuario.
    public async Task<string?> EjecutarAsync(
        int presupuestoId, int conceptoId, int? referenciaId, Usuario usuario,
        CancellationToken cancellationToken)
    {
        // La validación de la vista no alcanza (RNF-10): el servidor revisa que haya una selección
        // expresa. Nunca se elige una referencia por defecto, ni siquiera si hay una sola.
        if (referenciaId is null or <= 0)
        {
            return "Seleccioná una referencia de material y proveedor.";
        }

        ResultadoSeleccionarMaterial resultado = await _repositorio.SeleccionarMaterialSiPuedeEditarAsync(
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
                return "Solo se puede seleccionar un material en las filas de tipo Material.";
            case ResultadoSeleccionarMaterial.ReferenciaInexistente:
                return "La referencia seleccionada no existe. Buscá otra referencia del catálogo.";
            case ResultadoSeleccionarMaterial.ReferenciaDuplicada:
                return "Ese material y proveedor ya están en otra fila del presupuesto. Conservá la fila existente o seleccioná otra referencia.";
            case ResultadoSeleccionarMaterial.SinCambios:
                return "Esa referencia ya es la seleccionada en la fila. Elegí otra si querés sustituirla.";
            default:
                throw new InvalidOperationException("El repositorio devolvió un resultado desconocido.");
        }
    }
}
