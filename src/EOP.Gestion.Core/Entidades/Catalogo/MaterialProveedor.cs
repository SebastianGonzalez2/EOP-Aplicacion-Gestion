
using EOP.Gestion.Core.Comun;

namespace EOP.Gestion.Core.Entidades.Catalogo
{
    public class MaterialProveedor
    {
        public int Id { get; set; }

        // Identifica el material al que pertenece este precio.
        public int MaterialId { get; set; }

        // Identifica el proveedor que ofrece el material.
        public int ProveedorId { get; set; }

        // Precio actual del material para este proveedor.
        public decimal PrecioUnitario { get; set; }

        // Moneda del precio: UYU o USD.
        public Moneda Moneda { get; set; }

        // Fecha en que se registró o actualizó el precio.
        public DateTime FechaUltimaActualizacion { get; set; }
    }
}
