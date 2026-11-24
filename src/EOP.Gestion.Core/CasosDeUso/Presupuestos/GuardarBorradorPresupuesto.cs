using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// US-003 (RF-05): persiste definitivamente en la base de datos todos los cambios pendientes del
// presupuesto cuando el usuario presiona explícitamente «Guardar borrador».
//
// Lo que garantiza este caso de uso:
// - Comprueba permisos y estado editable en el servidor, incluso si no hay cambios pendientes.
// - Si la pantalla esperaba cambios pendientes pero la sesión se perdió, informa el error en vez
//   de responder éxito en silencio.
// - Valida el borrador completo antes de persistir y delega en el repositorio una única
//   transacción atómica con control de concurrencia.
// - Solo limpia los cambios pendientes cuando SQL Server confirmó el guardado; si ocurre un error,
//   el borrador pendiente se conserva para que el usuario pueda reintentar.
public class GuardarBorradorPresupuesto
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;

    public GuardarBorradorPresupuesto(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
    }

    public async Task<ResultadoAccionGuardarBorrador> EjecutarAsync(
        int presupuestoId,
        bool esperabaCambiosPendientes,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        BorradorPresupuestoDto? borrador = _almacenBorrador.Obtener(presupuestoId, usuario.Id);

        if (borrador == null || !borrador.TieneCambiosPendientes)
        {
            // Aunque no haya cambios por escribir, un usuario sin permiso o un presupuesto no
            // editable siempre deben ser rechazados en el servidor (RNF-4, RF-73, RF-02).
            bool puedeEditar = await _repositorio.PuedeEditarEnBaseAsync(
                presupuestoId, usuario, cancellationToken);

            if (!puedeEditar)
            {
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.EdicionNoPermitida,
                    "El presupuesto ya no está disponible para edición.");
            }

            // Si la pantalla mostraba cambios pendientes pero el almacén temporal ya no los tiene
            // (por ejemplo, venció la sesión), avisamos explícitamente para evitar pérdidas silenciosas.
            if (esperabaCambiosPendientes)
            {
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.CambiosPendientesPerdidos,
                    "No se encontraron los cambios pendientes de esta sesión. Revisá el presupuesto antes de volver a guardar.");
            }

            if (borrador != null)
            {
                _almacenBorrador.Eliminar(presupuestoId, usuario.Id);
            }

            return ResultadoAccionGuardarBorrador.Exito(huboCambiosGuardados: false);
        }

        // Validamos todas las reglas de dominio del borrador antes de abrir la transacción.
        // Antes de devolver un error de validación, nos aseguramos de que el usuario realmente
        // tenga permiso sobre el presupuesto para no filtrar información a usuarios no autorizados.
        string? errorValidacion = ValidadorBorradorPresupuesto.ValidarBorrador(borrador);
        if (errorValidacion != null)
        {
            bool puedeEditar = await _repositorio.PuedeEditarEnBaseAsync(
                presupuestoId, usuario, cancellationToken);

            if (!puedeEditar)
            {
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.EdicionNoPermitida,
                    "El presupuesto ya no está disponible para edición.");
            }

            return ResultadoAccionGuardarBorrador.Error(
                ResultadoGuardarBorrador.DatosInvalidos,
                errorValidacion);
        }

        ResultadoGuardarBorrador resultado = await _repositorio.GuardarBorradorSiPuedeEditarAsync(
            borrador, usuario, cancellationToken);

        switch (resultado)
        {
            case ResultadoGuardarBorrador.Guardado:
                // Recién después del Commit exitoso limpiamos los cambios pendientes.
                _almacenBorrador.Eliminar(presupuestoId, usuario.Id);
                return ResultadoAccionGuardarBorrador.Exito(huboCambiosGuardados: true);

            case ResultadoGuardarBorrador.SinCambios:
                _almacenBorrador.Eliminar(presupuestoId, usuario.Id);
                return ResultadoAccionGuardarBorrador.Exito(huboCambiosGuardados: false);

            case ResultadoGuardarBorrador.EdicionNoPermitida:
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.EdicionNoPermitida,
                    "El presupuesto ya no está disponible para edición.");

            case ResultadoGuardarBorrador.DatosInvalidos:
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.DatosInvalidos,
                    "Alguno de los conceptos pendientes contiene datos inválidos. Revisá la tabla antes de guardar.");

            case ResultadoGuardarBorrador.ReferenciaInexistente:
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.ReferenciaInexistente,
                    "Una de las referencias de material seleccionadas ya no existe en el catálogo. Revisá las filas antes de guardar.");

            case ResultadoGuardarBorrador.ReferenciaDuplicada:
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.ReferenciaDuplicada,
                    "Ese material y proveedor ya están en el presupuesto. Conservá la fila existente o seleccioná otra referencia.");

            case ResultadoGuardarBorrador.MargenExistente:
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.MargenExistente,
                    "Este presupuesto ya tiene un margen registrado.");

            case ResultadoGuardarBorrador.TiposBaseMargenNoDisponibles:
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.TiposBaseMargenNoDisponibles,
                    "Seleccioná tipos de concepto que estén presentes en el presupuesto.");

            case ResultadoGuardarBorrador.ConflictoConcurrencia:
                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.ConflictoConcurrencia,
                    "No se pudo guardar el borrador porque el presupuesto fue modificado desde otra operación. Revisá los cambios antes de volver a intentarlo.");

            default:
                throw new InvalidOperationException("El repositorio devolvió un resultado desconocido.");
        }
    }
}
