using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EOP.Gestion.WebApp.ViewModels.Proveedores
{
    // Lo que muestra y recibe el formulario de alta de proveedores (US-009, RF-23).
    //
    // Acá solo van las validaciones simples, las que se pueden decir con un atributo. Lo que hay
    // que calcular o consultar (dígito verificador del RUT, formato del teléfono y de los correos,
    // "al menos un correo", duplicados) le corresponde a S2-T11.
    public class RegistrarProveedorViewModel
    {
        [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
        [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        // El RUT no lleva largo máximo a propósito: en la base entran 12 caracteres, pero ya
        // normalizados. RF-23 deja escribirlo con separadores ("21.552.175-0017"), así que
        // con ese límite acá rechazaríamos datos que son válidos.
        [Required(ErrorMessage = "El RUT es obligatorio.")]
        public string Rut { get; set; } = string.Empty;

        // Código ISO del país elegido (UY, AR...). Es un dato del formulario y no se guarda en el Proveedor.
        // Uruguay se preselecciona en el GET del Controller y no acá: si lo dejáramos como valor inicial,
        // un POST que no mande el país pasaría como Uruguay sin que nadie lo haya elegido.
        [Required(ErrorMessage = "Seleccioná el país del teléfono.")]
        public string PaisTelefono { get; set; } = string.Empty;

        // El número como lo escribe el usuario, sin prefijo (099 123 456). En la base va a quedar
        // el internacional (+59899123456), como pide RF-23. Esa conversión es del servidor y se va
        // a hacer al validar y guardar (S2-T11 y S2-T13), no en este formulario.
        [Required(ErrorMessage = "Ingresá el número de teléfono.")]
        public string NumeroNacionalTelefono { get; set; } = string.Empty;

        // Las opciones del desplegable. Las carga el servidor, no vienen del formulario:
        // BindNever impide que un POST las cargue y ValidateNever evita validarlas.
        [BindNever]
        [ValidateNever]
        public List<SelectListItem> PaisesTelefono { get; set; } = new();

        // Los dos son opcionales por separado. La regla que los une ("al menos uno válido")
        // mira los dos campos a la vez y va con las validaciones de S2-T11.
        [StringLength(254, ErrorMessage = "El correo de contacto no puede superar los 254 caracteres.")]
        public string? CorreoContacto { get; set; }

        [StringLength(254, ErrorMessage = "El correo de facturación no puede superar los 254 caracteres.")]
        public string? CorreoFacturacion { get; set; }

        // La primera cuenta vacía la agrega el GET del Controller y no este inicializador: si la
        // pusiéramos acá, un POST que no mande ninguna cuenta se vería como si trajera una.
        //
        // Cuando llega el formulario, ASP.NET Core arma esta lista leyendo los campos
        // CuentasBancarias[0].Banco, CuentasBancarias[1].Banco, etc. También valida cada cuenta
        // de la lista con los atributos de CuentaBancariaViewModel.
        [MinLength(1, ErrorMessage = "Ingresá al menos una cuenta bancaria.")]
        public List<CuentaBancariaViewModel> CuentasBancarias { get; set; } = new();
    }
}
