
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EOP.Gestion.WebApp.ViewModels.Materiales
{
    public class AgregarProveedorMaterialViewModel
    {
        // Identifica el material existente al que vamos a agregarle un proveedor.
        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un material válido.")]
        public int MaterialId { get; set; }



        // Mostramos el nombre del material, pero no permitimos modificarlo.
        [BindNever]
        [ValidateNever]
        public string NombreMaterial { get; set; } = string.Empty;




        // El proveedor debe seleccionarse entre los registrados.
        [Required(ErrorMessage = "Seleccioná un proveedor.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccioná un proveedor válido.")]
        public int? ProveedorId { get; set; }




        // El precio debe ser estrictamente mayor que cero.
        [Required(ErrorMessage = "El precio unitario es obligatorio.")]
        [Range(typeof(decimal),
            "0",
            "79228162514264337593543950335",
            MinimumIsExclusive = true,
            ErrorMessage = "El precio unitario debe ser mayor que cero.")]
        public decimal? PrecioUnitario { get; set; }




        // Solamente permitimos pesos uruguayos o dólares.
        [Required(ErrorMessage = "Seleccioná una moneda.")]
        [RegularExpression("^(UYU|USD)$",
            ErrorMessage = "La moneda debe ser UYU o USD.")]
        public string Moneda { get; set; } = string.Empty;




        // Opciones que el servidor carga con los proveedores existentes.
        [BindNever]
        [ValidateNever]
        public List<SelectListItem> Proveedores { get; set; } = new();
    }
}
