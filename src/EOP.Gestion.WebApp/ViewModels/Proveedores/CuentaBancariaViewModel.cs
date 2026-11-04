using System.ComponentModel.DataAnnotations;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Proveedores;

namespace EOP.Gestion.WebApp.ViewModels.Proveedores
{
    // Los datos de UNA cuenta bancaria tal como vienen del formulario.
    public class CuentaBancariaViewModel
    {
        // Banco y Moneda son anulables a propósito, aunque en la entidad no lo sean. Un enum común
        // sin dato vale 0 y para [Required] eso ya es "un valor", así que la validación pasaría
        // igual. Con el "?" una cuenta sin banco queda en null y [Required] la frena.
        [Required(ErrorMessage = "Seleccioná el banco de la cuenta.")]
        public Banco? Banco { get; set; }

        // Los largos máximos son los mismos de CuentaBancariaConfiguracion. Además de validar,
        // Razor los usa para ponerle maxlength al input.
        [Required(ErrorMessage = "Ingresá el número de cuenta.")]
        [StringLength(50, ErrorMessage = "El número de cuenta no puede superar los 50 caracteres.")]
        public string NumeroCuenta { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá el titular de la cuenta.")]
        [StringLength(200, ErrorMessage = "El titular no puede superar los 200 caracteres.")]
        public string Titular { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccioná la moneda de la cuenta.")]
        public Moneda? Moneda { get; set; }
    }
}
