
namespace EOP.Gestion.Core.DTOs.Clientes
{
    public class ClienteListadoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Rut { get; set; } = string.Empty;
        public string? CorreoContacto { get; set; }
        public string? CorreoFacturacion { get; set; }
        public string NumeroWhatsApp { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
