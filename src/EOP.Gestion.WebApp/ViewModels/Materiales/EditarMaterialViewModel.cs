using System.ComponentModel.DataAnnotations;
using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Entidades.Catalogo;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EOP.Gestion.WebApp.ViewModels.Materiales
{
    // S2-T20 (US-071): datos del formulario de edición de un material.
    public class EditarMaterialViewModel
    {
        // Identifica el material que se está editando. Viaja escondido en el formulario.
        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un material válido.")]
        public int MaterialId { get; set; }

        [Required(ErrorMessage = "El nombre del material es obligatorio.")]
        // Avisa en el navegador y al lado del campo; el control definitivo lo hace EditarMaterial.
        [StringLength(RegistrarMaterial.LargoMaximoNombre, ErrorMessage = "El nombre del material no puede superar los 200 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccioná una unidad de medida.")]
        [EnumDataType(typeof(UnidadMedida), ErrorMessage = "Seleccioná una unidad de medida válida.")]
        public UnidadMedida? Unidad { get; set; }

        // Una fila por cada relación material-proveedor que ya existe.
        // No se pueden agregar ni quitar filas desde acá: solo cambiar sus valores.
        public List<EditarReferenciaPrecioViewModel> Referencias { get; set; } = new();
    }

    public class EditarReferenciaPrecioViewModel
    {
        // Cuál de las relaciones del material es esta fila.
        [Range(1, int.MaxValue, ErrorMessage = "La referencia de precio no es válida.")]
        public int RelacionId { get; set; }

        // El proveedor se muestra, pero no se puede cambiar. Lo vuelve a cargar el servidor.
        [BindNever]
        [ValidateNever]
        public string ProveedorNombre { get; set; } = string.Empty;

        // La fecha tampoco se edita: la actualiza el sistema cuando cambia el precio o la moneda.
        [BindNever]
        [ValidateNever]
        public DateTime FechaUltimaActualizacion { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio.")]
        [Range(typeof(decimal),
        "0",
        "79228162514264337593543950335",
        MinimumIsExclusive = true,
        ErrorMessage = "El precio unitario debe ser mayor que cero.")]
        public decimal? PrecioUnitario { get; set; }

        [Required(ErrorMessage = "Seleccioná una moneda.")]
        [RegularExpression("^(UYU|USD)$",
        ErrorMessage = "La moneda debe ser UYU o USD.")]
        public string Moneda { get; set; } = string.Empty;
    }
}
