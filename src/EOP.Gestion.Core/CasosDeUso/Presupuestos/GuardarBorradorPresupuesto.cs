using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// US-003 (RF-05): persiste definitivamente en la base de datos todos los cambios pendientes del
// presupuesto cuando el usuario presiona explícitamente «Guardar borrador».
//
// Lo que garantiza este caso de uso:
// - Sincroniza el ciclo completo (cargar sesión -> validar -> persistir -> limpiar y confirmar sesión)
//   para evitar choques entre pestañas o con ediciones simultáneas del mismo usuario.
// - Comprueba permisos y estado editable en el servidor, incluso si no hay cambios pendientes.
// - Si la pantalla esperaba cambios pendientes pero ya no están disponibles, informa claramente
//   sin confirmar éxito en silencio.
// - Diferencia conflictos entre pestañas del mismo usuario de conflictos por cambios de otro usuario.
// - Solo limpia los cambios pendientes cuando SQL Server confirmó el guardado; si ocurre un error,
//   el borrador pendiente se conserva para que el usuario pueda reintentar.

/*El caso de uso GuardarBorradorPresupuesto recupera los cambios pendientes que tiene el usuario en su sesión, 
 * comprueba que sean válidos y le solicita al repositorio que los guarde definitivamente en la base de datos. 
 * Si el guardado es exitoso, elimina el borrador temporal. Si ocurre un error, conserva los cambios pendientes.*/
public class GuardarBorradorPresupuesto
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;
    private readonly ISincronizadorBorradorPresupuesto _sincronizador;

    public GuardarBorradorPresupuesto(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ISincronizadorBorradorPresupuesto sincronizador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
        _sincronizador = sincronizador;
    }

    public Task<ResultadoAccionGuardarBorrador> EjecutarAsync(
        int presupuestoId,
        ContextoEdicionBorrador contextoEdicion,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        return _sincronizador.EjecutarConBloqueoAsync(
            usuario.Id,
            () => EjecutarDentroDelBloqueoAsync(presupuestoId, contextoEdicion, usuario, cancellationToken),
            cancellationToken);
    }

    private async Task<ResultadoAccionGuardarBorrador> EjecutarDentroDelBloqueoAsync(
        int presupuestoId,
        ContextoEdicionBorrador contextoEdicion,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        await _almacenBorrador.CargarAsync(cancellationToken);

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

            string? huellaPersistida = await _repositorio.ObtenerHuellaPersistidaAsync(
                presupuestoId, cancellationToken);

            bool fueGuardadoEnOtraPestana = !string.IsNullOrWhiteSpace(huellaPersistida)
                && _almacenBorrador.CoincideConUltimoGuardadoEnSesion(presupuestoId, usuario.Id, huellaPersistida);

            // Si la pantalla esperaba cambios pendientes (o una versión/identificador previo) y el
            // borrador ya no está en la sesión, distinguimos si lo guardó otra pestaña o si se perdió.
            if (contextoEdicion.EsperabaCambiosPendientes
                || contextoEdicion.VersionBorradorEsperada > 0
                || !string.IsNullOrWhiteSpace(contextoEdicion.BorradorIdEsperado))
            {
                if (fueGuardadoEnOtraPestana)
                {
                    return ResultadoAccionGuardarBorrador.Error(
                        ResultadoGuardarBorrador.BorradorDesactualizado,
                        MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana);
                }

                // S3-T15: el borrador no se perdió, el usuario lo descartó desde otra pestaña.
                if (ControladorEstadoBorrador.FueDescartadoDesdeOtraPestana(
                    presupuestoId, usuario.Id, contextoEdicion, _almacenBorrador))
                {
                    return ResultadoAccionGuardarBorrador.Error(
                        ResultadoGuardarBorrador.BorradorDesactualizado,
                        MensajesBorradorPresupuesto.CambiosDescartadosEnOtraPestana);
                }

                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.CambiosPendientesPerdidos,
                    MensajesBorradorPresupuesto.CambiosPendientesPerdidos);
            }

            if (!string.IsNullOrWhiteSpace(contextoEdicion.HuellaEstadoBaseEsperada)
                && !string.Equals(contextoEdicion.HuellaEstadoBaseEsperada, huellaPersistida, StringComparison.Ordinal))
            {
                if (fueGuardadoEnOtraPestana)
                {
                    return ResultadoAccionGuardarBorrador.Error(
                        ResultadoGuardarBorrador.BorradorDesactualizado,
                        MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana);
                }

                return ResultadoAccionGuardarBorrador.Error(
                    ResultadoGuardarBorrador.ConflictoConcurrencia,
                    MensajesBorradorPresupuesto.PresupuestoModificadoSinPendientes);
            }

            if (borrador != null)
            {
                _almacenBorrador.Eliminar(presupuestoId, usuario.Id);
                await _almacenBorrador.ConfirmarAsync(cancellationToken);
            }

            return ResultadoAccionGuardarBorrador.Exito(huboCambiosGuardados: false);
        }

        // Si la pantalla intenta guardar desde otro borrador o desde una versión anterior a la que
        // tiene la sesión, rechazamos la solicitud para no persistir cambios que esa pestaña no vio.
        bool coincideIdentificador = !string.IsNullOrWhiteSpace(borrador.BorradorId)
            && string.Equals(contextoEdicion.BorradorIdEsperado, borrador.BorradorId, StringComparison.Ordinal);

        if (contextoEdicion.VersionBorradorEsperada != borrador.Version || !coincideIdentificador)
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
                ResultadoGuardarBorrador.BorradorDesactualizado,
                MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana);
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

        ResultadoPersistenciaBorrador persistencia = await _repositorio.GuardarBorradorSiPuedeEditarAsync(
            borrador, usuario, cancellationToken);

        switch (persistencia.Estado)
        {
            case ResultadoGuardarBorrador.Guardado:
                // Recién después del Commit exitoso limpiamos los cambios pendientes y registramos
                // la huella guardada para distinguir guardados desde otra pestaña de pérdidas.
                _almacenBorrador.Eliminar(presupuestoId, usuario.Id);
                if (!string.IsNullOrWhiteSpace(persistencia.HuellaPersistida))
                {
                    _almacenBorrador.RegistrarHuellaGuardada(presupuestoId, usuario.Id, persistencia.HuellaPersistida);
                }

                await _almacenBorrador.ConfirmarAsync(cancellationToken);
                return ResultadoAccionGuardarBorrador.Exito(huboCambiosGuardados: true);

            case ResultadoGuardarBorrador.SinCambios:
                _almacenBorrador.Eliminar(presupuestoId, usuario.Id);
                await _almacenBorrador.ConfirmarAsync(cancellationToken);
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
                    MensajesBorradorPresupuesto.PresupuestoModificadoConPendientesConservados);

            default:
                throw new InvalidOperationException("El repositorio devolvió un resultado desconocido.");
        }
    }
}
