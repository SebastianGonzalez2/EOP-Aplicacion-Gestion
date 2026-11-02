using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EOP.Gestion.WebApp.ViewModels.Clientes
{
    public class RegistrarClienteViewModel
    {


        // Como estos string no admiten null, ASP.NET Core ya los trata como obligatorios, pero
        // con su mensaje en inglés ("The Nombre field is required."). Con [Required] ponemos el nuestro,
        // que además es el que usa la validación del navegador.
        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUT es obligatorio.")]
        public string Rut { get; set; } = string.Empty;
        public string? CorreoContacto { get; set; }
        public string? CorreoFacturacion { get; set; }

        // Código ISO del país elegido (UY, AR...). Es un dato del formulario y no se guarda en el Cliente.
        // Uruguay se preselecciona en el GET del Controller y no acá: si lo dejáramos como valor inicial,
        // un POST que no mande el país pasaría como Uruguay sin que nadie lo haya elegido.
        [Required(ErrorMessage = "Seleccioná el país del número de WhatsApp.")]
        public string PaisWhatsApp { get; set; } = string.Empty;

        // El número como lo escribe el usuario, sin prefijo (099 123 456). Tampoco se guarda
        // así: el servidor lo convierte y en la base queda el internacional (+59899123456).
        [Required(ErrorMessage = "Ingresá el número de WhatsApp.")]
        public string NumeroNacionalWhatsApp { get; set; } = string.Empty;

        // Las opciones del desplegable. Las carga el servidor, no vienen del formulario:
        // BindNever impide que un POST las cargue y ValidateNever evita validarlas.
        [BindNever]
        [ValidateNever]
        public List<SelectListItem> PaisesWhatsApp { get; set; } = new();






    }



}
