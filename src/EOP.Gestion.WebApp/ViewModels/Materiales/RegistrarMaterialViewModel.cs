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
        public UnidadMedida? Unidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio.")]
        public decimal? PrecioUnitario { get; set; }

        [Required(ErrorMessage = "Seleccioná una moneda.")]
        public string Moneda { get; set; } = string.Empty;

        // Identifica al proveedor que seleccionó el usuario.
        // Es nullable para detectar cuando no eligió ninguno.
        [Required(ErrorMessage = "Seleccioná un proveedor.")]
        public int? ProveedorId { get; set; }

        // Lista de proveedores que mostraremos en el desplegable.
        // La cargará el servidor con proveedores existentes.
        [BindNever]
        [ValidateNever]
        public List<SelectListItem> Proveedores { get; set; } = new();

    }
}