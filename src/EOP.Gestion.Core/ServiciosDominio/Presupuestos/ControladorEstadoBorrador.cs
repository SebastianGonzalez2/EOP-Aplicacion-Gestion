using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

// Mensajes unificados para la gestión de cambios pendientes, conflictos entre pestañas y
// conflictos por modificaciones concurrentes de otros usuarios (US-003, RF-05).
// Evitan jerga técnica (no mencionan reinicios, memoria RAM ni expiración de sesiones).
public static class MensajesBorradorPresupuesto
{
    public const string CambiosPendientesPerdidos =
        "No se pudieron recuperar los cambios pendientes. El presupuesto conserva la información del último guardado.";

    public const string BorradorModificadoEnOtraPestana =
        "El borrador fue modificado desde otra pestaña. Actualizá la información antes de continuar.";

    public const string PresupuestoModificadoConPendientesConservados =
        "El presupuesto fue modificado mientras estabas editándolo. Tus cambios pendientes se conservaron. Revisá la información antes de continuar.";

    public const string PresupuestoModificadoSinPendientes =
        "El presupuesto fue modificado mientras estabas editándolo. Revisá la información antes de continuar.";
}

// Resultado de comprobar si el borrador temporal de una solicitud coincide con el estado vigente.
public record ResultadoVerificacionBorrador(
    BorradorPresupuestoDto? BorradorParaEditar,
    bool EsNuevoBorrador,
    ResultadoGuardarBorrador? ErrorEstado,
    string? MensajeError);

// Centraliza la detección de pérdida del borrador temporal y de solicitudes basadas en pantallas
// desactualizadas (otra pestaña u otro usuario) para que todas las operaciones del presupuesto
// apliquen exactamente las mismas reglas.
public static class ControladorEstadoBorrador
{
    // Verifica el estado del borrador antes de aplicar una modificación (Agregar, AsignarMaterial,
    // CambiarProveedor, ActualizarReferencia, RegistrarMargen).
    // Debe ejecutarse dentro del bloqueo del sincronizador y después de CargarAsync().
    public static async Task<ResultadoVerificacionBorrador> PrepararParaEdicionAsync(
        int presupuestoId,
        int usuarioId,
        ContextoEdicionBorrador contexto,
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        CancellationToken cancellationToken)
    {
        BorradorPresupuestoDto? borradorActual = almacenBorrador.Obtener(presupuestoId, usuarioId);

        if (borradorActual != null && borradorActual.TieneCambiosPendientes)
        {
            // Ya existe un borrador pendiente en la sesión: la pantalla debe coincidir tanto en
            // el identificador único del borrador como en su versión actual. Así evitamos que una
            // pestaña vieja coincida accidentalmente con la versión 1 de un borrador nuevo.
            bool coincideIdentificador = !string.IsNullOrWhiteSpace(borradorActual.BorradorId)
                && string.Equals(contexto.BorradorIdEsperado, borradorActual.BorradorId, StringComparison.Ordinal);

            if (contexto.VersionBorradorEsperada != borradorActual.Version || !coincideIdentificador)
            {
                return new ResultadoVerificacionBorrador(
                    null,
                    false,
                    ResultadoGuardarBorrador.BorradorDesactualizado,
                    MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana);
            }

            return new ResultadoVerificacionBorrador(
                borradorActual.Clonar(),
                false,
                null,
                null);
        }

        // No hay un borrador con cambios pendientes en la sesión. Cargamos el estado guardado en SQL Server.
        BorradorPresupuestoDto? borradorBase = await repositorio.ObtenerBorradorBaseAsync(
            presupuestoId, cancellationToken);

        if (borradorBase == null)
        {
            return new ResultadoVerificacionBorrador(
                null,
                false,
                ResultadoGuardarBorrador.EdicionNoPermitida,
                "El presupuesto ya no está disponible para edición.");
        }

        bool fueGuardadoDesdeOtraPestana = almacenBorrador.CoincideConUltimoGuardadoEnSesion(
            presupuestoId, usuarioId, borradorBase.HuellaEstadoBase);

        // Si la pantalla esperaba cambios pendientes (o una versión/identificador previo) pero ya
        // no están en sesión, rechazamos la operación en lugar de iniciar silenciosamente un borrador nuevo.
        if (contexto.EsperabaCambiosPendientes
            || contexto.VersionBorradorEsperada > 0
            || !string.IsNullOrWhiteSpace(contexto.BorradorIdEsperado))
        {
            if (fueGuardadoDesdeOtraPestana)
            {
                return new ResultadoVerificacionBorrador(
                    null,
                    false,
                    ResultadoGuardarBorrador.BorradorDesactualizado,
                    MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana);
            }

            return new ResultadoVerificacionBorrador(
                null,
                false,
                ResultadoGuardarBorrador.CambiosPendientesPerdidos,
                MensajesBorradorPresupuesto.CambiosPendientesPerdidos);
        }

        // Si la pantalla informó la huella del estado guardado cuando se cargó y la base cambió
        // mientras tanto (por otra pestaña que guardó o por otro usuario), rechazamos el cambio.
        if (!string.IsNullOrWhiteSpace(contexto.HuellaEstadoBaseEsperada)
            && !string.Equals(contexto.HuellaEstadoBaseEsperada, borradorBase.HuellaEstadoBase, StringComparison.Ordinal))
        {
            if (fueGuardadoDesdeOtraPestana)
            {
                return new ResultadoVerificacionBorrador(
                    null,
                    false,
                    ResultadoGuardarBorrador.BorradorDesactualizado,
                    MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana);
            }

            return new ResultadoVerificacionBorrador(
                null,
                false,
                ResultadoGuardarBorrador.ConflictoConcurrencia,
                MensajesBorradorPresupuesto.PresupuestoModificadoSinPendientes);
        }

        // Es una edición nueva limpia (sin cambios pendientes previos): asignamos un identificador
        // único nuevo y arrancamos en versión 1.
        borradorBase.BorradorId = Guid.NewGuid().ToString("N");
        borradorBase.Version = 1;
        return new ResultadoVerificacionBorrador(borradorBase, true, null, null);
    }

    // Verifica el estado cuando una recarga o navegación indicaba que había cambios pendientes.
    // Devuelve null si los cambios siguen disponibles, o el mensaje que corresponde mostrar.
    public static async Task<string?> VerificarNavegacionConCambiosPendientesAsync(
        int presupuestoId,
        int usuarioId,
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        CancellationToken cancellationToken)
    {
        BorradorPresupuestoDto? borradorActual = almacenBorrador.Obtener(presupuestoId, usuarioId);
        if (borradorActual != null && borradorActual.TieneCambiosPendientes)
        {
            return null;
        }

        string? huellaPersistida = await repositorio.ObtenerHuellaPersistidaAsync(
            presupuestoId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(huellaPersistida)
            && almacenBorrador.CoincideConUltimoGuardadoEnSesion(presupuestoId, usuarioId, huellaPersistida))
        {
            return MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana;
        }

        return MensajesBorradorPresupuesto.CambiosPendientesPerdidos;
    }
}
