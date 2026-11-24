using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// S3-T12 (US-010A, RF-020) + S3-T14 (RF-05): cuando el catálogo cambió el nombre, la unidad,
// el precio o la moneda de la referencia que la fila ya tiene, el presupuesto NO se actualiza solo
// (DT-11). Si el usuario quiere incorporar los valores nuevos, tiene que pedirlo y confirmarlo
// expresamente. Este caso de uso aplica esa actualización en el borrador pendiente, que se guarda
// definitivamente recién al presionar «Guardar borrador».
//
// Lo que NO hace, a propósito:
// - No actualiza nada sin confirmación explícita.
// - No cambia el material ni el proveedor de la fila (para eso están US-007 y el cambio de proveedor).
// - No toca ConceptoDetectado ni la cantidad.
// - No modifica el catálogo: solo lo lee para copiar los datos a la fila.
public class ActualizarReferenciaDeConcepto
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;
    private readonly ISincronizadorBorradorPresupuesto _sincronizador;

    public ActualizarReferenciaDeConcepto(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ISincronizadorBorradorPresupuesto sincronizador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
        _sincronizador = sincronizador;
    }

    // Devuelve null si se actualizó en el borrador, o el mensaje que hay que mostrarle al usuario.
    public async Task<string?> EjecutarAsync(
        int presupuestoId, int conceptoId, bool confirmado, Usuario usuario,
        CancellationToken cancellationToken, ContextoEdicionBorrador? contextoEdicion = null)
    {
        // La confirmación se exige en el servidor (RNF-10): el diálogo de la pantalla es una
        // comodidad, pero una solicitud sin confirmar nunca debe pisar los valores históricos.
        if (!confirmado)
        {
            return "Confirmá la actualización para reemplazar los valores de la fila por los del catálogo.";
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
                    return "Solo se pueden actualizar los valores de las filas de tipo Material.";
                }

                if (fila.MaterialId == null || fila.MaterialProveedorId == null)
                {
                    return "La fila todavía no tiene un material. Buscá y seleccioná primero el material.";
                }

                ReferenciaCatalogoPresupuestoDto? referenciaActual = await _repositorio.ObtenerReferenciaCatalogoAsync(
                    fila.MaterialProveedorId.Value, cancellationToken);

                if (referenciaActual == null)
                {
                    return "La referencia de la fila ya no existe en el catálogo. Elegí otra referencia del material.";
                }

                // Si los cuatro valores de la fila ya son los del catálogo no hay nada que actualizar.
                bool yaCoinciden = fila.Concepto == referenciaActual.NombreMaterial
                    && fila.Unidad == referenciaActual.Unidad
                    && fila.PrecioUnitario == referenciaActual.PrecioUnitario
                    && fila.Moneda == referenciaActual.Moneda;

                if (yaCoinciden)
                {
                    return "Los valores de la fila ya coinciden con los del catálogo. No hay nada que actualizar.";
                }

                fila.CopiarReferencia(referenciaActual);
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
