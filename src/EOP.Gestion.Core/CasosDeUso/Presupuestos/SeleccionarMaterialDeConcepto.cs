using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// US-007 (RF-12, RF-13) + S3-T14 (RF-05): el usuario elige a mano un material del catálogo para
// una fila de tipo Material que ya existe en el presupuesto (persistida o pendiente en el borrador).
// Sirve tanto para vincular una fila que todavía no tiene material (las que va a generar el análisis
// con IA de US-006) como para sustituir el material que la fila ya tenía.
//
// Lo que NO hace, a propósito:
// - No busca ni propone materiales: la persona elige expresamente una referencia.
// - No crea ni modifica nada del catálogo: solo lo lee para copiar los datos a la fila.
// - No toca ConceptoDetectado ni la cantidad de la fila.
// - No persiste definitivamente en SQL Server hasta que el usuario presiona «Guardar borrador».
public class SeleccionarMaterialDeConcepto
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;

    public SeleccionarMaterialDeConcepto(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
    }

    // Devuelve null si se aplicó al borrador, o el mensaje que hay que mostrarle al usuario.
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

        // Primero el acceso: si no tiene permiso no revelamos ni siquiera si la fila existe.
        bool puedeEditar = await _repositorio.PuedeEditarEnBaseAsync(
            presupuestoId, usuario, cancellationToken);

        if (!puedeEditar)
        {
            return "El presupuesto ya no está disponible para edición.";
        }

        BorradorPresupuestoDto? borrador = await ObtenerOInicializarBorradorAsync(
            presupuestoId, usuario.Id, cancellationToken);

        if (borrador == null)
        {
            return "El presupuesto ya no está disponible para edición.";
        }

        ConceptoBorradorDto? fila = BuscarFila(borrador, conceptoId);
        if (fila == null)
        {
            return "La fila que querés modificar ya no existe en el presupuesto.";
        }

        if (fila.TipoConceptoId != TipoConcepto.MaterialId)
        {
            return "Solo se puede seleccionar un material en las filas de tipo Material.";
        }

        // Si la fila ya usa esta referencia no se copia nada de nuevo: volver a copiar pisaría
        // los valores históricos que la fila conserva con los actuales del catálogo (DT-11).
        if (fila.MaterialProveedorId == referenciaId.Value)
        {
            return "Esa referencia ya es la seleccionada en la fila. Elegí otra si querés sustituirla.";
        }

        ReferenciaCatalogoPresupuestoDto? referencia = await _repositorio.ObtenerReferenciaCatalogoAsync(
            referenciaId.Value, cancellationToken);

        if (referencia == null)
        {
            return "La referencia seleccionada no existe. Buscá otra referencia del catálogo.";
        }

        // RF-20: la misma combinación material-proveedor no puede estar en dos filas del presupuesto.
        foreach (ConceptoBorradorDto otraFila in borrador.Conceptos)
        {
            if (otraFila.Id != fila.Id && otraFila.MaterialProveedorId == referencia.MaterialProveedorId)
            {
                return "Ese material y proveedor ya están en otra fila del presupuesto. Conservá la fila existente o seleccioná otra referencia.";
            }
        }

        fila.CopiarReferencia(referencia);
        if (!fila.EsNuevo)
        {
            fila.FueModificado = true;
        }

        _almacenBorrador.Guardar(presupuestoId, usuario.Id, borrador);
        return null;
    }

    private async Task<BorradorPresupuestoDto?> ObtenerOInicializarBorradorAsync(
        int presupuestoId, int usuarioId, CancellationToken cancellationToken)
    {
        BorradorPresupuestoDto? borradorExistente = _almacenBorrador.Obtener(presupuestoId, usuarioId);
        if (borradorExistente != null)
        {
            return borradorExistente.Clonar();
        }

        return await _repositorio.ObtenerBorradorBaseAsync(presupuestoId, cancellationToken);
    }

    private static ConceptoBorradorDto? BuscarFila(BorradorPresupuestoDto borrador, int conceptoId)
    {
        foreach (ConceptoBorradorDto fila in borrador.Conceptos)
        {
            if (fila.Id == conceptoId)
            {
                return fila;
            }
        }

        return null;
    }
}
