namespace EOP.Gestion.Core.Comun;

/// <summary>
/// No se pudo averiguar la cotización porque falló la comunicación con el proveedor
/// (datosUruguay): no hubo conexión, tardó demasiado, respondió con un error o mandó
/// algo que no se puede interpretar.
/// </summary>
/// <remarks>
/// Es importante no confundir esto con "no hay publicación". Si el proveedor responde bien y
/// simplemente no existe ninguna cotización válida en o antes de la fecha pedida, el servicio
/// devuelve <c>null</c>. Esta excepción, en cambio, significa "no sabemos": RF-29 y RF-103 dicen
/// que una falla temporal no se puede interpretar como ausencia de publicación, y tampoco
/// habilita a usar la cotización de otra fecha.
///
/// Sigue la misma idea que <see cref="AlmacenamientoNoDisponibleException"/>: Core no conoce
/// HttpClient, así que los casos de uso atrapan esta excepción para informar el problema al
/// usuario (RNF-13) y decidir qué hacer según su propia regla de negocio. El mensaje es genérico
/// y se puede mostrar; el detalle técnico queda en los logs y en <see cref="Exception.InnerException"/>.
/// </remarks>
public class CotizacionNoDisponibleException : Exception
{
    public CotizacionNoDisponibleException(MotivoCotizacionNoDisponible motivo, Exception? causa)
        : base(ArmarMensaje(motivo), causa)
    {
        Motivo = motivo;
    }

    // Qué fue lo que falló. Les sirve a los casos de uso si quieren dar un mensaje más preciso
    // (por ejemplo "esperá unos minutos" cuando se pasó el límite de solicitudes).
    public MotivoCotizacionNoDisponible Motivo { get; }

    private static string ArmarMensaje(MotivoCotizacionNoDisponible motivo)
    {
        string mensajeComun = "No se pudo obtener la cotización del dólar desde datosUruguay. ";

        switch (motivo)
        {
            case MotivoCotizacionNoDisponible.LimiteDeSolicitudesExcedido:
                return mensajeComun + "Se hicieron demasiadas consultas seguidas; intentá de nuevo en unos minutos.";
            case MotivoCotizacionNoDisponible.TiempoDeEsperaAgotado:
                return mensajeComun + "El servicio tardó demasiado en responder; intentá de nuevo más tarde.";
            default:
                return mensajeComun + "Intentá de nuevo más tarde.";
        }
    }
}

/// <summary>
/// Los motivos por los que no se pudo obtener una cotización. Todos son fallas técnicas:
/// ninguno significa que no haya publicación.
/// </summary>
public enum MotivoCotizacionNoDisponible
{
    // No se pudo conectar con el proveedor (sin internet, DNS, el servidor no atiende...).
    FallaDeConexion = 1,

    // El proveedor no respondió a tiempo.
    TiempoDeEsperaAgotado = 2,

    // El proveedor respondió HTTP 429: se superó su límite de consultas por IP.
    LimiteDeSolicitudesExcedido = 3,

    // El proveedor respondió con otro código de error HTTP (por ejemplo 500 o 503).
    ErrorDelProveedor = 4,

    // La respuesta llegó, pero no es un JSON con la forma esperada o tiene datos incompletos.
    RespuestaInvalida = 5,

    // Se recorrieron muchas páginas hacia atrás sin encontrar una cotización válida y quedaban
    // más páginas. No sabemos si más atrás hay una, así que no podemos afirmar que no exista.
    BusquedaIncompleta = 6
}
