using System.ComponentModel.DataAnnotations;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.WebApp.ViewModels.Usuarios
{
    // Los datos que manda el formulario para cambiar el rol de un usuario.
    public class ModificarRolViewModel
    {
        // Los únicos textos que se aceptan como rol: el nombre exacto de cada uno de los tres.
        private const string RolesPermitidos =
            $"^({nameof(RolUsuario.Administrador)}|{nameof(RolUsuario.Supervisor)}|{nameof(RolUsuario.Operador)})$";

        // Si el formulario no manda el Id, un int queda en 0, que no es el Id de nadie.
        [Range(1, int.MaxValue, ErrorMessage = "El usuario seleccionado no es válido.")]
        public int UsuarioId { get; set; }

        // Es un string y no un RolUsuario a propósito. Si fuera el enum, ASP.NET Core
        // aceptaría "Administrador,Supervisor" o "1,2": los convierte combinando los números
        // (1 + 2 = 3) y el usuario terminaría como Operador, que es el rol con valor 3, sin
        // que nadie lo haya elegido. Comparando el texto, eso se rechaza.
        //
        // Por eso el formulario tiene que mandar el nombre del rol ("Supervisor") y no su número.
        [Required(ErrorMessage = "Seleccioná un rol válido.")]
        [RegularExpression(RolesPermitidos, ErrorMessage = "Seleccioná un rol válido.")]
        public string? NuevoRol { get; set; }
    }
}
