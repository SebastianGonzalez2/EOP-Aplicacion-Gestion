using EOP.Gestion.Core.Comun;

namespace EOP.Gestion.Core.DTOs.Materiales
{
    // S2-T20 (US-071): lo que el usuario mandó para una referencia de precio ya existente.
    // Solo viajan el Id de la relación y los dos datos que se pueden cambiar: el proveedor de
    // una relación nunca se modifica, y las relaciones nuevas se registran aparte (US-010B).
    public class CambioReferenciaPrecio
    {
        // Identifica la fila de MaterialProveedores que se quiere modificar.
        public int RelacionId { get; set; }

        public decimal? PrecioUnitario { get; set; }

        public Moneda? Moneda { get; set; }
    }
}
