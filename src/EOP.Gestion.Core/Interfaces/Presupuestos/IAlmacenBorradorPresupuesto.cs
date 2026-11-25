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
}
