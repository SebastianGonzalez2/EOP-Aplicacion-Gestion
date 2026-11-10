using EOP.Gestion.Core.Comun;

namespace EOP.Gestion.Core.DTOs.Materiales
{
    public class ReferenciaPrecioMaterialDto
    {
        public string ProveedorNombre { get; set; } = string.Empty;

        public decimal PrecioUnitario { get; set; }

        public Moneda Moneda { get; set; }

        public DateTime FechaUltimaActualizacion { get; set; }
    }
}