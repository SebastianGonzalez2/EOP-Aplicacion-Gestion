using EOP.Gestion.Core.ServiciosDominio.Clientes;

public class ValidadorCorreosClienteTests
{
    [Fact]
    public void TieneAlMenosUnCorreo_DebeRechazarAmbosVacios() //no le ponemos ningun correo, ambos son nulos 
    {
        bool resultado = ValidadorCorreosCliente.TieneAlMenosUnCorreo(null,null);

        Assert.False(resultado);
    }


    [Fact]
    public void TieneAlMenosUnCorreo_DebeAceptarSoloCorreoContacto()//Probamos con un formato valido solo el correo de contacto
    {
        string correoContacto = "cliente@gmail.com";
        string? correoFacturacion = null;

        bool resultado = ValidadorCorreosCliente.TieneAlMenosUnCorreo(correoContacto,correoFacturacion);

        Assert.True(resultado);
    }

    [Fact]
    public void TieneAlMenosUnCorreo_DebeAceptarSoloCorreoFacturacion() //Probamos con un formato valido solo el correo de facturacion
    {
        string? correoContacto = null;
        string correoFacturacion = "facturacion@gmail.com";

        bool resultado = ValidadorCorreosCliente.TieneAlMenosUnCorreo(correoContacto,correoFacturacion);

        Assert.True(resultado);
    }


    [Fact]
    public void TieneAlMenosUnCorreo_DebeAceptarAmbosCorreos() //Probamos con los dos correos con formato valido
    {
        string correoContacto = "cliente@gmail.com";
        string correoFacturacion = "facturacion@gmail.com";

        bool resultado = ValidadorCorreosCliente.TieneAlMenosUnCorreo(correoContacto,correoFacturacion);

        Assert.True(resultado);
    }


    [Fact]
    public void TieneFormatoValido_DebeRechazarCorreoSinArroba() //Probamos formato invalido
    {
        string correo = "cliente.gmail.com";
        bool resultado = ValidadorCorreosCliente.TieneFormatoValido(correo);

        Assert.False(resultado);
    }

    [Fact]
    public void TieneFormatoValido_DebeAceptarCorreoCorrecto() //Probamos formato valido
    {
        string correo = "cliente@gmail.com";
        bool resultado = ValidadorCorreosCliente.TieneFormatoValido(correo);

        Assert.True(resultado);
    }


}