using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;

namespace EOP.Gestion.Core.DTOs.Materiales
{
    // S2-T20 (US-071): los datos actuales de un material para mostrarlos en el formulario de edición.
    public class MaterialEdicionDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public UnidadMedida Unidad { get; set; }

        // Las relaciones con proveedores que ya tiene el material.
        public List<ReferenciaEdicionDto> Referencias { get; set; } = new();
    }

    // Una referencia de precio existente. A diferencia de ReferenciaPrecioMaterialDto (que sirve
    // para el listado), esta trae el Id de la relación: es lo que permite saber cuál se editó.
    public class ReferenciaEdicionDto
    {
        public int RelacionId { get; set; }

        public string ProveedorNombre { get; set; } = string.Empty;

        public decimal PrecioUnitario { get; set; }

        public Moneda Moneda { get; set; }

        public DateTime FechaUltimaActualizacion { get; set; }
    }
}
