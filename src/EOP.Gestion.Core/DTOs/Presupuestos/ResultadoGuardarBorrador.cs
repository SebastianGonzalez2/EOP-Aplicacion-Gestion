namespace EOP.Gestion.Core.DTOs.Presupuestos;

// Resultados posibles al persistir definitivamente un borrador en la base de datos (US-003, RF-05).
public enum ResultadoGuardarBorrador
{
    Guardado,
    SinCambios,
    EdicionNoPermitida,
    CambiosPendientesPerdidos,
    DatosInvalidos,
    ReferenciaInexistente,
    ReferenciaDuplicada,
    MargenExistente,
    TiposBaseMargenNoDisponibles,
    ConflictoConcurrencia
}

// Resultado que devuelve el caso de uso GuardarBorradorPresupuesto al Controller.
// Distingue claramente un guardado efectivo, un guardado sin cambios pendientes, una falta de
// permisos (que debe responder Forbid) y un error funcional o de concurrencia.
public class ResultadoAccionGuardarBorrador
{
    public ResultadoGuardarBorrador Estado { get; init; }

    public string? MensajeError { get; init; }

    public bool GuardadoCorrectamente
    {
        get
        {
            return Estado == ResultadoGuardarBorrador.Guardado
                || Estado == ResultadoGuardarBorrador.SinCambios;
        }
    }

    public bool HuboCambiosGuardados
    {
        get
        {
            return Estado == ResultadoGuardarBorrador.Guardado;
        }
    }

    public static ResultadoAccionGuardarBorrador Exito(bool huboCambiosGuardados)
    {
        return new ResultadoAccionGuardarBorrador
        {
            Estado = huboCambiosGuardados
                ? ResultadoGuardarBorrador.Guardado
                : ResultadoGuardarBorrador.SinCambios
        };
    }

    public static ResultadoAccionGuardarBorrador Error(ResultadoGuardarBorrador estado, string mensaje)
    {
        return new ResultadoAccionGuardarBorrador
        {
            Estado = estado,
            MensajeError = mensaje
        };
    }
}
