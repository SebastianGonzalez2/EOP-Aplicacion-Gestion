
using System.ComponentModel.DataAnnotations;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.WebApp.ViewModels.Materiales;

namespace EOP.Gestion.Tests.Unitarias
{
    public class RegistrarMaterialViewModelTests
    {
        // Creamos un formulario con datos válidos para usarlo
        // como punto de partida en las pruebas.
        private static RegistrarMaterialViewModel CrearModeloValido()
        {
            return new RegistrarMaterialViewModel
            {
                Nombre = "Cable de cobre",
                Unidad = UnidadMedida.Metro,
                ProveedorId = 1,
                PrecioUnitario = 150,
                Moneda = "UYU"
            };
        }

        // Ejecutamos las validaciones del ViewModel.
        private static List<ValidationResult> Validar(
            RegistrarMaterialViewModel modelo)
        {
            var resultados = new List<ValidationResult>();

            Validator.TryValidateObject(
                modelo,
                new ValidationContext(modelo),
                resultados,
                true);

            return resultados;
        }

        [Fact]
        public void DatosValidos_NoGeneranErrores()
        {
            var modelo = CrearModeloValido();

            var errores = Validar(modelo);

            Assert.Empty(errores);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void PrecioNoPositivo_GeneraError(int precio)
        {
            var modelo = CrearModeloValido();
            modelo.PrecioUnitario = precio;

            var errores = Validar(modelo);

            Assert.Contains(errores, error =>
                error.MemberNames.Contains(nameof(modelo.PrecioUnitario)));
        }

        [Fact]
        public void MonedaInvalida_GeneraError()
        {
            var modelo = CrearModeloValido();
            modelo.Moneda = "EUR";

            var errores = Validar(modelo);

            Assert.Contains(errores, error =>
                error.MemberNames.Contains(nameof(modelo.Moneda)));
        }

        [Fact]
        public void UnidadInvalida_GeneraError()
        {
            var modelo = CrearModeloValido();
            modelo.Unidad = (UnidadMedida)99;

            var errores = Validar(modelo);

            Assert.Contains(errores, error =>
                error.MemberNames.Contains(nameof(modelo.Unidad)));
        }

        [Fact]
        public void TodasLasUnidadesDelEnum_SonValidas()
        {
            foreach (UnidadMedida unidad in Enum.GetValues<UnidadMedida>())
            {
                var modelo = CrearModeloValido();
                modelo.Unidad = unidad;

                var errores = Validar(modelo);

                Assert.Empty(errores);
            }
        }

        [Fact]
        public void ProveedorIdInvalido_GeneraError()
        {
            var modelo = CrearModeloValido();
            modelo.ProveedorId = 0;

            var errores = Validar(modelo);

            Assert.Contains(errores, error =>
                error.MemberNames.Contains(nameof(modelo.ProveedorId)));
        }
    }
}
