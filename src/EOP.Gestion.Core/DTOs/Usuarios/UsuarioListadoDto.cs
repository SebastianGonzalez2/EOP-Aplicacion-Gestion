
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Core.DTOs.Usuarios
{
    public class UsuarioListadoDto
    {
        public int Id { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string CorreoCorporativo { get; set; } = string.Empty;

        public RolUsuario Rol { get; set; }

        public bool Activo { get; set; }
    }
}
