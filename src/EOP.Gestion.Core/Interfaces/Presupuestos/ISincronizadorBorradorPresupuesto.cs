namespace EOP.Gestion.Core.Interfaces.Presupuestos;

// Coordina el acceso concurrente al almacenamiento temporal de borradores por usuario.
//
// ¿Por qué se sincroniza por usuario y no solo por presupuesto?
// Porque la sesión HTTP guarda todos los borradores del mismo usuario en un único registro de
// sesión. Si dos pestañas del mismo usuario modifican a la vez dos presupuestos distintos,
// serializar el ciclo completo (cargar sesión -> modificar -> confirmar sesión) evita que una
// pestaña sobrescriba las claves de la otra. Distintos usuarios nunca se bloquean entre sí.
//
// Limitación de diseño: esta sincronización usa memoria local de la instancia de la aplicación,
// por lo que está pensada para el despliegue en una única instancia.
public interface ISincronizadorBorradorPresupuesto
{
    Task<TResultado> EjecutarConBloqueoAsync<TResultado>(
        int usuarioId,
        Func<Task<TResultado>> operacion,
        CancellationToken cancellationToken);

    Task EjecutarConBloqueoAsync(
        int usuarioId,
        Func<Task> operacion,
        CancellationToken cancellationToken);

    int CantidadBloqueosActivos { get; }
}
