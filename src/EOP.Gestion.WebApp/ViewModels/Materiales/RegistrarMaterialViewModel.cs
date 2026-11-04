using System.ComponentModel.DataAnnotations;
using EOP.Gestion.Core.Entidades.Catalogo;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace EOP.Gestion.WebApp.ViewModels.Materiales
{
    public class RegistrarMaterialViewModel
    {
        [Required(ErrorMessage = "El nombre del material es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;



        [Required(ErrorMessage = "Seleccioná una unidad de medida.")]
        [EnumDataType(typeof(UnidadMedida),
        ErrorMessage = "Seleccioná una unidad de medida válida.")]
        public UnidadMedida? Unidad { get; set; }



        [Required(ErrorMessage = "El precio unitario es obligatorio.")]
        [Range(typeof(decimal),
        "0.0000000000000000000000000001",
        "79228162514264337593543950335",
        ErrorMessage = "El precio unitario debe ser mayor que cero.")]
        public decimal? PrecioUnitario { get; set; }





        [Required(ErrorMessage = "Seleccioná una moneda.")]
        [RegularExpression("^(UYU|USD)$",
        ErrorMessage = "La moneda debe ser UYU o USD.")]
        public string Moneda { get; set; } = string.Empty;



        [Required(ErrorMessage = "Seleccioná un proveedor.")]
        [Range(1, int.MaxValue,
        ErrorMessage = "Seleccioná un proveedor válido.")]
        public int? ProveedorId { get; set; }



        // Lista de proveedores que mostraremos en el desplegable.
        // La cargará el servidor con proveedores existentes.
        [BindNever]
        [ValidateNever]
        public List<SelectListItem> Proveedores { get; set; } = new();

    }
}