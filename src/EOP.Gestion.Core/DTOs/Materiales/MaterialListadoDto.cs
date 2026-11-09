using EOP.Gestion.Core.Entidades.Catalogo;

namespace EOP.Gestion.Core.DTOs.Materiales
{
    public class MaterialListadoDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public UnidadMedida Unidad { get; set; }

        // Un material puede tener precios de distintos proveedores.
        public List<ReferenciaPrecioMaterialDto> Referencias { get; set; } = new();
    }
}