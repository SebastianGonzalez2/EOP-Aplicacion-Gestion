using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// S3-T12 (US-010A, RF-20) + S3-T14 (RF-05): el usuario ve las referencias de precio del material
// que la fila ya tiene y elige otra de ellas. La fila conserva el mismo material y pasa a usar el
// proveedor, el precio y la moneda de la referencia nueva dentro del borrador pendiente.
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
// - No persiste en SQL Server hasta que el usuario presiona explícitamente «Guardar borrador».
public class CambiarProveedorDeConcepto
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;
    private readonly ISincronizadorBorradorPresupuesto _sincronizador;

    public CambiarProveedorDeConcepto(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ISincronizadorBorradorPresupuesto sincronizador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
        _sincronizador = sincronizador;
    }

    // Devuelve null si se aplicó al borrador, o el mensaje que hay que mostrarle al usuario.
    public async Task<string?> EjecutarAsync(
        int presupuestoId, int conceptoId, int? referenciaId, Usuario usuario,
        CancellationToken cancellationToken, ContextoEdicionBorrador? contextoEdicion = null)
    {
        // La validación de la vista no alcanza (RNF-10): el servidor exige una selección expresa.
        if (referenciaId is null or <= 0)
        {
            return "Seleccioná la referencia de proveedor que querés usar.";
        }

        ContextoEdicionBorrador contexto = contextoEdicion ?? ContextoEdicionBorrador.Inicial;

        return await _sincronizador.EjecutarConBloqueoAsync(
            usuario.Id,
            async () =>
            {
                await _almacenBorrador.CargarAsync(cancellationToken);

                bool puedeEditar = await _repositorio.PuedeEditarEnBaseAsync(
                    presupuestoId, usuario, cancellationToken);

                if (!puedeEditar)
                {
                    return "El presupuesto ya no está disponible para edición.";
                }

                ResultadoVerificacionBorrador verificacion = await ControladorEstadoBorrador.PrepararParaEdicionAsync(
                    presupuestoId, usuario.Id, contexto, _repositorio, _almacenBorrador, cancellationToken);

                if (verificacion.MensajeError != null || verificacion.BorradorParaEditar == null)
                {
                    return verificacion.MensajeError;
                }

                BorradorPresupuestoDto borrador = verificacion.BorradorParaEditar;

                ConceptoBorradorDto? fila = BuscarFila(borrador, conceptoId);
                if (fila == null)
                {
                    return "La fila que querés modificar ya no existe en el presupuesto.";
                }

                if (fila.TipoConceptoId != TipoConcepto.MaterialId)
                {
                    return "Solo se puede cambiar el proveedor en las filas de tipo Material.";
                }

                // S3-T12: sin material en la fila no hay un "otro proveedor" que elegir.
                if (fila.MaterialId == null)
                {
                    return "La fila todavía no tiene un material. Buscá y seleccioná primero el material.";
                }

                if (fila.MaterialProveedorId == referenciaId.Value)
                {
                    return "Esa referencia ya es la seleccionada en la fila. Elegí otra si querés cambiar de proveedor.";
                }

                ReferenciaCatalogoPresupuestoDto? referencia = await _repositorio.ObtenerReferenciaCatalogoAsync(
                    referenciaId.Value, cancellationToken);

                if (referencia == null)
                {
                    return "La referencia seleccionada no existe. Elegí otra de la lista.";
                }

                // S3-T12: el cambio de proveedor no puede cambiar el material. Se compara contra el material
                // que la fila ya tiene, no contra lo que mande la pantalla (RNF-4, RNF-10).
                if (referencia.MaterialId != fila.MaterialId.Value)
                {
                    return "Esa referencia no es del material de la fila. Para cambiar de material usá la búsqueda.";
                }

                // RF-20: la misma combinación material-proveedor no puede estar en dos filas del presupuesto.
                foreach (ConceptoBorradorDto otraFila in borrador.Conceptos)
                {
                    if (otraFila.Id != fila.Id && otraFila.MaterialProveedorId == referencia.MaterialProveedorId)
                    {
                        return "Ese material y proveedor ya están en otra fila del presupuesto. Modificá la cantidad de esa fila o seleccioná otra referencia.";
                    }
                }

                fila.CopiarReferencia(referencia);
                if (!fila.EsNuevo)
                {
                    fila.FueModificado = true;
                }

                if (!verificacion.EsNuevoBorrador)
                {
                    borrador.IncrementarVersion();
                }

                _almacenBorrador.Guardar(presupuestoId, usuario.Id, borrador);
                await _almacenBorrador.ConfirmarAsync(cancellationToken);
                return null;
            },
            cancellationToken);
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
