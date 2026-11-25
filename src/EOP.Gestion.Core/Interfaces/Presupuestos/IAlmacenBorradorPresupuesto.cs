using EOP.Gestion.Core.DTOs.Presupuestos;

namespace EOP.Gestion.Core.Interfaces.Presupuestos;

// Almacén temporal de cambios pendientes de un presupuesto durante la elaboración (US-003, RF-05).
//
// Los cambios viven separados de SQL Server y aislados por usuario y presupuesto hasta que el
// usuario presiona explícitamente «Guardar borrador» o los descarta (preparado para S3-T15).
public interface IAlmacenBorradorPresupuesto
{
    // Carga de forma explícita el almacenamiento de la sesión dentro del bloqueo de sincronización,
    // garantizando que se lea el estado más reciente confirmado por solicitudes anteriores.
    Task CargarAsync(CancellationToken cancellationToken);

    // Confirma de forma explícita los cambios realizados en la sesión antes de liberar el bloqueo,
    // evitando que dos solicitudes concurrentes de la misma sesión sobrescriban sus claves.
    Task ConfirmarAsync(CancellationToken cancellationToken);

    BorradorPresupuestoDto? Obtener(int presupuestoId, int usuarioId);

    void Guardar(int presupuestoId, int usuarioId, BorradorPresupuestoDto borrador);

    void Eliminar(int presupuestoId, int usuarioId);

    bool TieneCambiosPendientes(int presupuestoId, int usuarioId);

    // Registra en la sesión la última huella persistida en SQL Server para ese presupuesto.
    // Permite diferenciar si un borrador desapareció porque otra pestaña de la misma sesión lo
    // guardó, o porque se perdió el almacenamiento temporal.
    void RegistrarHuellaGuardada(int presupuestoId, int usuarioId, string huellaGuardada);

    bool CoincideConUltimoGuardadoEnSesion(int presupuestoId, int usuarioId, string huellaActual);

    // S3-T15: devuelve todos los borradores con cambios pendientes de ESE usuario, de cualquier
    // presupuesto. Lo usamos para saber, desde cualquier pantalla, si al cerrar sesión hay algo
    // sin guardar. Nunca devuelve borradores de otro usuario.
    List<BorradorPresupuestoDto> ObtenerPendientesDelUsuario(int usuarioId);

    // S3-T15: anota qué borrador se descartó a propósito. Así, si una pestaña vieja intenta seguir
    // trabajando con ese borrador, le explicamos que se descartó desde otra pestaña en lugar de
    // decirle que "se perdió".
    void RegistrarDescarte(int presupuestoId, int usuarioId, string borradorId);

    bool FueDescartadoEnSesion(int presupuestoId, int usuarioId, string borradorId);

    // S3-T15: borra TODO lo que guarda el almacén temporal en esta sesión. Se usa solo al cerrar
    // sesión, para que no quede nada guardado para la próxima persona que use el mismo navegador.
    void VaciarDatosTemporales();
}
