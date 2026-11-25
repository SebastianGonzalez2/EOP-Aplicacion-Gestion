namespace EOP.Gestion.Core.DTOs.Presupuestos;

// S3-T15 (US-003, RF-05): datos que necesitamos para gestionar los cambios sin guardar fuera de
// la pantalla de elaboración (descarte voluntario y cierre de sesión).

// Un presupuesto en el que el usuario tiene cambios sin guardar, tal como lo mostramos en la
// advertencia de cierre de sesión. BorradorId y Version son la "foto" del borrador que vio el
// usuario: si cambian antes de que decida, sabemos que otra pestaña lo tocó.
public record CambioPendienteDto(
    int PresupuestoId,
    string Codigo,
    string NombreProyecto,
    string BorradorId,
    int Version);

// Lo que la advertencia de cierre de sesión le mostró al usuario de cada borrador. El servidor lo
// compara con lo que hay en la sesión en el momento de guardar o descartar.
public record BorradorEsperadoDto(
    int PresupuestoId,
    string? BorradorId,
    int Version);

public enum ResultadoDescartarCambios
{
    // Se eliminaron los cambios temporales de ese usuario y presupuesto.
    Descartado,

    // No había nada para descartar (ya se guardó, ya se descartó o se perdió la sesión).
    SinCambiosPendientes,

    // La pantalla mostraba una versión vieja del borrador: no se descarta nada que el usuario no vio.
    BorradorDesactualizado
}

public record ResultadoAccionDescartarCambios(
    ResultadoDescartarCambios Estado,
    string Mensaje);

// Cómo terminó cada presupuesto cuando el usuario eligió Guardar o Descartar al cerrar sesión.
public record ResultadoPresupuestoAlCerrarSesion(
    int PresupuestoId,
    string Codigo,
    bool Correcto,
    string Mensaje);

// Resultado completo de intentar cerrar sesión con cambios pendientes.
// La sesión solo se cierra si PuedeCerrarSesion es true: es decir, cuando todo se guardó o se
// descartó bien y no quedó ningún cambio pendiente en la sesión.
public class ResultadoCierreSesionConPendientes
{
    public bool PuedeCerrarSesion { get; init; }

    // Mensaje general cuando no se llegó a procesar ningún presupuesto (por ejemplo, porque la
    // lista de la advertencia ya no coincide con la sesión).
    public string? MensajeGeneral { get; init; }

    public List<ResultadoPresupuestoAlCerrarSesion> Resultados { get; init; } = [];
}
