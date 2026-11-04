using EOP.Gestion.Core.ServiciosDominio.Clientes;

namespace EOP.Gestion.Tests.Unitarias;

public class ValidadorRutTests
{
    // Los extremos de cada parte que admite la estructura, con su dígito verificador correcto.
    [Theory]
    [InlineData("010000010011")]    // El prefijo más chico.
    [InlineData("220000010011")]    // El prefijo más grande.
    [InlineData("019999990013")]    // El número de serie más grande.
    [InlineData("229999990013")]
    [InlineData("21-000001-001-4")] // Con los separadores habituales.
    public void EsValido_DebeAceptarRutConEstructuraCorrecta(string rut)
    {
        Assert.True(ValidadorRut.EsValido(rut));
    }

    // Todos estos tienen bien la cuenta del dígito verificador: lo único que falla es la
    // estructura. Antes de agregar ese control, el validador los aceptaba a todos.
    [Theory]
    [InlineData("000000000000")]    // Todo ceros.
    [InlineData("001599340019")]    // Prefijo 00.
    [InlineData("230000010019")]    // Prefijo 23, el primero fuera del rango.
    [InlineData("991599340011")]    // Prefijo 99.
    [InlineData("210000000019")]    // Número de serie en cero.
    [InlineData("211599345519")]    // Sufijo 455.
    [InlineData("211234560027")]    // Sufijo 002.
    public void EsValido_DebeRechazarRutConEstructuraIncorrecta(string rut)
    {
        Assert.False(ValidadorRut.EsValido(rut));
    }

    // Con estos primeros 11 dígitos la cuenta da 10, y 10 no entra en un solo dígito. Por eso
    // no existe ningún RUT que empiece así, sea cual sea el último número.
    [Theory]
    [InlineData("210000020010")]
    [InlineData("210000020011")]
    [InlineData("210000020012")]
    [InlineData("210000020013")]
    [InlineData("210000020014")]
    [InlineData("210000020015")]
    [InlineData("210000020016")]
    [InlineData("210000020017")]
    [InlineData("210000020018")]
    [InlineData("210000020019")]
    public void EsValido_DebeRechazarRutSinDigitoVerificadorPosible(string rut)
    {
        Assert.False(ValidadorRut.EsValido(rut));
    }

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