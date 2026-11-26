namespace EOP.Gestion.Core.DTOs.Cotizaciones;

/// <summary>
/// Una cotización del dólar publicada por el BCU, expresada en pesos uruguayos por dólar (UYU/USD).
/// </summary>
/// <remarks>
/// La cotización se pide para una fecha (<see cref="FechaSolicitada"/>), pero puede que ese día
/// no haya habido publicación: un sábado, un domingo o un feriado. En ese caso el servicio
/// devuelve la última publicación válida anterior (RF-29, RF-077, RF-103, RF-104), y por eso
/// guardamos las dos fechas por separado.
///
/// Cuando una tarea posterior conserve la cotización aplicada (por ejemplo
/// <c>FechaCotizacionConfirmada</c> en el presupuesto o <c>FechaCotizacionAplicada</c> en una
/// factura), la fecha que tiene que guardar es <see cref="FechaPublicacion"/>, que es la fecha
/// efectiva del valor usado, y no la fecha que se pidió.
/// </remarks>
/// <param name="Valor">
/// Cuántos pesos uruguayos vale un dólar. Siempre es mayor que cero. Es <c>decimal</c> y se
/// conserva tal cual lo publicó el proveedor, sin redondear (RF-29 pide no hacer redondeos intermedios).
/// </param>
/// <param name="FechaPublicacion">El día al que corresponde la publicación del BCU que se usó.</param>
/// <param name="FechaSolicitada">El día para el que se pidió la cotización.</param>
public sealed record CotizacionUsd(
    decimal Valor,
    DateOnly FechaPublicacion,
    DateOnly FechaSolicitada)
{
    // true si ese mismo día hubo publicación; false si se tuvo que usar una anterior.
    // Sirve, por ejemplo, para que una pantalla aclare "se usó la cotización del viernes".
    public bool EsDeLaFechaSolicitada
    {
        get { return FechaPublicacion == FechaSolicitada; }
    }
}
