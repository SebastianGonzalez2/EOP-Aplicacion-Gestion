using EOP.Gestion.Core.DTOs.Cotizaciones;

namespace EOP.Gestion.Core.Interfaces.Cotizaciones;

// Core dice QUÉ necesita (la cotización UYU/USD de un día) e Infrastructure resuelve CÓMO
// (hoy, consultando datosUruguay, que publica los datos del BCU). Es el único punto del sistema
// que consigue cotizaciones: lo van a usar los presupuestos (RF-27, RF-29, RF-31), las facturas
// de compra y de venta (RF-104, RF-077) y los costos adicionales (RF-103), así nadie repite la
// integración.
//
// Las tres respuestas posibles de los dos métodos:
//   - una CotizacionUsd: hay cotización válida (mayor que cero) publicada ese día o, si ese día
//     no hubo publicación, la última anterior. Nunca una posterior, un promedio ni una estimada.
//   - null: el proveedor respondió bien y no existe ninguna publicación válida en o antes de esa fecha.
//   - CotizacionNoDisponibleException: no se pudo saber, porque falló la comunicación con el proveedor.
//
// Este servicio NO decide qué hacer cuando no hay cotización (rechazar el guardado, permitir un
// borrador, impedir confirmar...). Eso lo decide cada caso de uso según su requerimiento.
// Tampoco guarda nada en la base: conservar la cotización aplicada es tarea de quien la usa.
public interface IServicioCotizaciones
{
    // La cotización para el día de hoy en Uruguay (o la última anterior si hoy todavía no se publicó).
    // Es la "cotización actual" que usa el presupuesto mientras se elabora (RF-29).
    Task<CotizacionUsd?> ObtenerCotizacionVigenteAsync(CancellationToken cancellationToken = default);

    // La cotización para un día determinado (o la última anterior si ese día no hubo publicación),
    // por ejemplo la fecha de emisión de una factura (RF-077, RF-104) o la fecha de un costo (RF-103).
    // La fecha no puede ser posterior a hoy en Uruguay: lanza ArgumentOutOfRangeException.
    Task<CotizacionUsd?> ObtenerCotizacionHistoricaAsync(DateOnly fecha, CancellationToken cancellationToken = default);
}
