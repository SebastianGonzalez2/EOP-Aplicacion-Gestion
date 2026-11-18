using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;

namespace EOP.Gestion.Core.Entidades.Presupuestos
{
    // Una fila de la tabla económica del presupuesto (RF-16, RF-20, RF-27).
    //
    // Cuando el concepto es de tipo Material, guarda además la referencia de
    // material-proveedor elegida y copia sus cuatro datos históricos: nombre
    // (Concepto), unidad, precio unitario y moneda. Esos valores son del
    // presupuesto y no se vuelven a leer del catálogo, así que un cambio posterior
    // en Material o MaterialProveedor no modifica presupuestos anteriores (DT-11).
    //
    // Para los conceptos con importe manual, PrecioUnitario guarda el importe
    // ingresado y Moneda la moneda elegida. El subtotal de esas filas es ese
    // importe, sin cotización histórica individual (RF-16).
    //
    // El concepto Margen es el único que usa PorcentajeMargen. Los tipos que
    // forman su base de cálculo se guardan en MargenBaseTipo (RF-27).
    public class ConceptoPresupuesto
    {
        public int Id { get; set; }

        // Todo concepto pertenece a un presupuesto y tiene un tipo.
        public int PresupuestoId { get; set; }

        public int TipoConceptoId { get; set; }

        // Denominación que se muestra en la tabla: el nombre del material copiado
        // o la descripción ingresada para un concepto manual.
        public string? Concepto { get; set; }

        // Solo lo completa el análisis del relevamiento (US-006). En las filas
        // agregadas manualmente no aplica y queda en NULL (US-011).
        public string? ConceptoDetectado { get; set; }

        // Referencia del catálogo elegida de forma expresa. Las dos columnas van
        // juntas: o no hay material, o hay material y referencia.
        public int? MaterialId { get; set; }

        public int? MaterialProveedorId { get; set; }

        // Cantidad y unidad no son obligatorias para todos los tipos (RF-16, RF-24).
        public decimal? Cantidad { get; set; }

        public UnidadMedida? Unidad { get; set; }

        // Precio unitario del material o importe manual. Se guarda con la misma
        // precisión que el precio del catálogo para no perder decimales (DT-11).
        public decimal? PrecioUnitario { get; set; }

        public Moneda? Moneda { get; set; }

        // Porcentaje del concepto Margen. Los demás tipos lo dejan en NULL.
        public decimal? PorcentajeMargen { get; set; }
    }
}
