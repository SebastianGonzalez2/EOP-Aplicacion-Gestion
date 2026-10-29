using EOP.Gestion.Core.ServiciosDominio.Clientes;

namespace EOP.Gestion.Tests.Unitarias;

public class ValidadorRutTests
{
    [Fact]
    public void Normalizar_DebeEliminarPuntosGuionesYEspacios()
    {
        // RUT ingresado con separadores.
        string rutIngresado = "21.552.175-0017";

        // Ejecutamos el método que queremos probar.
        string resultado = ValidadorRut.Normalizar(rutIngresado);

        // Comprobamos que el resultado contenga únicamente los dígitos.
        Assert.Equal("215521750017", resultado);
    }

    [Fact]
    public void EsValido_DebeAceptarRutConDigitoVerificadorCorrecto()
    {
        // RUT de ejemplo con dígito verificador correcto.
        string rut = "215521750017";

        // Ejecutamos el método que valida el RUT.
        bool resultado = ValidadorRut.EsValido(rut);

        // Comprobamos que el RUT sea válido.
        Assert.True(resultado);
    }


    [Fact]
    public void EsValido_DebeRechazarRutConDigitoVerificadorIncorrecto()
    {
        // RUT con el último dígito incorrecto.
        string rut = "215521750018";

        // Ejecutamos el método que valida el RUT.
        bool resultado = ValidadorRut.EsValido(rut);

        // Comprobamos que el RUT sea rechazado.
        Assert.False(resultado);
    }


    [Theory]
    [InlineData("12345678901")]     // Tiene 11 dígitos.
    [InlineData("1234567890123")]   // Tiene 13 dígitos.
    [InlineData("21552175001A")]    // Contiene una letra.
    public void EsValido_DebeRechazarRutConFormatoIncorrecto(string rut)
    {
        // Ejecutamos la validación del RUT.
        bool resultado = ValidadorRut.EsValido(rut);

        // Comprobamos que el RUT sea rechazado.
        Assert.False(resultado);
    }


}