namespace EOP.Gestion.Core.Entidades.Presupuestos
{
    // Los ocho estados de un presupuesto (RF-02). No se pueden crear estados nuevos desde el sistema.
    //
    // Los números son lo que queda guardado en la base, así que nunca se cambian ni se reutilizan.
    // Arrancan en 1 a propósito: un enum sin asignar vale 0, y como 0 no es ningún estado,
    // la base rechaza un presupuesto al que se olvidaron de ponerle estado.
    public enum EstadoPresupuesto
    {
        Borrador = 1,
        ParaRevisionInterna = 2,
        EnviadoPendienteDeRespuestaDelCliente = 3,
        Aprobado = 4,
        Rechazado = 5,
        SolicitudDeModificacionDelCliente = 6,
        Vencido = 7,
        Cancelado = 8
    }
}
