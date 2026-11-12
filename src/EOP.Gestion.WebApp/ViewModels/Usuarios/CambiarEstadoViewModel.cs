using System.ComponentModel.DataAnnotations;

namespace EOP.Gestion.WebApp.ViewModels.Usuarios
{
    // Los datos que manda el formulario para activar o inactivar a un usuario (US-046).
    public class CambiarEstadoViewModel
    {
        // Los dos únicos textos que se aceptan como estado nuevo.
        public const string EstadoActivo = "Activo";
        public const string EstadoInactivo = "Inactivo";

        // Si el formulario no manda el Id, un int queda en 0, que no es el Id de nadie.
        [Range(1, int.MaxValue, ErrorMessage = "El usuario seleccionado no es válido.")]
        public int UsuarioId { get; set; }

        // Es un string y no un bool a propósito: si el campo faltara en el pedido, un bool
        // quedaría en false y el usuario se inactivaría sin que nadie lo haya pedido.
        // Con el texto obligatorio, un pedido incompleto se rechaza.
        [Required(ErrorMessage = "Seleccioná un estado válido.")]
        [RegularExpression($"^({EstadoActivo}|{EstadoInactivo})$", ErrorMessage = "Seleccioná un estado válido.")]
        public string? NuevoEstado { get; set; }
    }
}
