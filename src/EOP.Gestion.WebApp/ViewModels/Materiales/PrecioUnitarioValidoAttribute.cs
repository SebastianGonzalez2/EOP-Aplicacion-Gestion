using System.ComponentModel.DataAnnotations;
using EOP.Gestion.Core.Comun;

namespace EOP.Gestion.WebApp.ViewModels.Materiales
{
    // S2-T22: valida el precio de los formularios con la misma regla que usan los casos de uso
    // (ValidadorPrecioUnitario), así el mensaje junto al campo coincide con el del servidor.
    // Si no viene valor, lo deja pasar: de eso se encarga [Required].
    public class PrecioUnitarioValidoAttribute : ValidationAttribute
    {
        // Devolvemos el error como resultado en vez de guardarlo en el atributo: los atributos
        // se comparten entre todas las solicitudes y no deben cambiar de estado.
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            string? error = ValidadorPrecioUnitario.ObtenerError((decimal)value);

            if (error != null)
            {
                // Indicamos a qué campo pertenece el error para que aparezca junto a ese campo.
                return new ValidationResult(error, new[] { validationContext.MemberName! });
            }

            return ValidationResult.Success;
        }
    }
}
