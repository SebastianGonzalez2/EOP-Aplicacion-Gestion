
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EOP.Gestion.WebApp.ViewModels.Presupuestos
{
    public class CrearPresupuestoViewModel
    {
        // Nombre del proyecto o trabajo que vamos a presupuestar.
        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
        public string NombreProyecto { get; set; } = string.Empty;




        // Cliente seleccionado en el formulario.
        [Required(ErrorMessage = "Seleccioná un cliente.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un cliente válido.")]
        public int? ClienteId { get; set; }




        // Tipo de proyecto elegido entre las opciones fijas.
        [Required(ErrorMessage = "Seleccioná un tipo de proyecto.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un tipo de proyecto válido.")]
        public int? TipoProyectoId { get; set; }




        // Cantidad de días corridos durante los que será válido el presupuesto.
        [Required(ErrorMessage = "Ingresá el plazo de vigencia.")]
        [Range(1, int.MaxValue, ErrorMessage = "El plazo debe ser mayor que cero.")]
        public int? PlazoVigenciaDias { get; set; }




        // Estas listas contienen las opciones de los desplegables.
        // Las carga el servidor y no se reciben desde el formulario.
        [BindNever]
        [ValidateNever]
        public List<SelectListItem> Clientes { get; set; } = new();



        [BindNever]
        [ValidateNever]
        public List<SelectListItem> TiposProyecto { get; set; } = new();
    }
}
