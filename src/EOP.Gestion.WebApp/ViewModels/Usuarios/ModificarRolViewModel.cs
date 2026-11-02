using System.ComponentModel.DataAnnotations;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.WebApp.ViewModels.Usuarios
{
    // Los datos que manda el formulario para cambiar el rol de un usuario.
    public class ModificarRolViewModel
    {
        // Si el formulario no manda el Id, un int queda en 0, que no es el Id de nadie.
        [Range(1, int.MaxValue, ErrorMessage = "El usuario seleccionado no es válido.")]
        public int UsuarioId { get; set; }

        // Lo dejamos anulable a propósito. Si no lo fuera, un formulario que no mande
        // el rol quedaría con el valor 0 del enum en vez de marcarse como faltante.
        //
        // Un valor que no corresponde a ningún rol ("Gerente", "99") también
        // deja este dato sin cargar, y ASP.NET Core lo marca como inválido.
        [Required(ErrorMessage = "Seleccioná un rol válido.")]
        public RolUsuario? NuevoRol { get; set; }
    }
}
