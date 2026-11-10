
namespace EOP.Gestion.Core.DTOs.Proveedores
{
    public class ProveedorListadoDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Rut { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string? CorreoContacto { get; set; }

        public string? CorreoFacturacion { get; set; }
    }
}
