using System.ComponentModel.DataAnnotations;

namespace EOP.Gestion.WebApp.ViewModels.Usuarios
{
    // Los datos que muestra y recibe el formulario de alta de usuarios (US-043, RF-70).
    //
    // Acá hay únicamente tres campos a propósito. ASP.NET Core solo carga lo que existe en esta
    // clase, así que aunque alguien agregue a mano Activo o EntraObjectId al pedido, esos
    // valores se ignoran. Si recibiéramos directamente la entidad Usuario, sí se podrían cargar.
    //
    // Estas validaciones permiten mostrar cada error al lado de su campo. Las reglas definitivas
    // (formato, duplicados, rol) las vuelve a aplicar el caso de uso RegistrarUsuario.
    public class RegistrarUsuarioViewModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre completo no puede superar los 150 caracteres.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo corporativo es obligatorio.")]
        [StringLength(254, ErrorMessage = "El correo corporativo no puede superar los 254 caracteres.")]
        [EmailAddress(ErrorMessage = "El correo corporativo no tiene un formato válido.")]
        public string CorreoCorporativo { get; set; } = string.Empty;

        // Es un string y no un RolUsuario por lo mismo que explica ModificarRolViewModel: si fuera
        // el enum, ASP.NET Core aceptaría "1,2" o "Administrador,Supervisor" combinando los números.
        //
        // No tiene un valor inicial para que un POST que no mande el rol no pase como si alguien
        // lo hubiera elegido.
        [Required(ErrorMessage = "Seleccioná un rol.")]
        [RegularExpression(ModificarRolViewModel.RolesPermitidos, ErrorMessage = "Seleccioná un rol válido.")]
        public string? Rol { get; set; }
    }
}
