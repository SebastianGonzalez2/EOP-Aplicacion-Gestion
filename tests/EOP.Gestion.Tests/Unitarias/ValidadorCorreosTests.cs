using EOP.Gestion.Core.ServiciosDominio.Compartidos;

public class ValidadorCorreosTests
{
    [Fact]
    public void TieneAlMenosUnCorreo_DebeRechazarAmbosVacios() //no le ponemos ningun correo, ambos son nulos 
    {
        bool resultado = ValidadorCorreos.TieneAlMenosUnCorreo(null,null);

        Assert.False(resultado);
    }


    [Fact]
    public void TieneAlMenosUnCorreo_DebeAceptarSoloCorreoContacto()//Probamos con un formato valido solo el correo de contacto
    {
        string correoContacto = "cliente@gmail.com";
        string? correoFacturacion = null;

        bool resultado = ValidadorCorreos.TieneAlMenosUnCorreo(correoContacto,correoFacturacion);

        Assert.True(resultado);
    }

    [Fact]
    public void TieneAlMenosUnCorreo_DebeAceptarSoloCorreoFacturacion() //Probamos con un formato valido solo el correo de facturacion
    {
        string? correoContacto = null;
        string correoFacturacion = "facturacion@gmail.com";

        bool resultado = ValidadorCorreos.TieneAlMenosUnCorreo(correoContacto,correoFacturacion);

        Assert.True(resultado);
    }


    [Fact]
    public void TieneAlMenosUnCorreo_DebeAceptarAmbosCorreos() //Probamos con los dos correos con formato valido
    {
        string correoContacto = "cliente@gmail.com";
        string correoFacturacion = "facturacion@gmail.com";

        bool resultado = ValidadorCorreos.TieneAlMenosUnCorreo(correoContacto,correoFacturacion);

        Assert.True(resultado);
    }


    [Fact]
    public void TieneFormatoValido_DebeRechazarCorreoSinArroba() //Probamos formato invalido
    {
        string correo = "cliente.gmail.com";
        bool resultado = ValidadorCorreos.TieneFormatoValido(correo);

        Assert.False(resultado);
    }

    [Fact]
    public void TieneFormatoValido_DebeAceptarCorreoCorrecto() //Probamos formato valido
    {
        string correo = "cliente@gmail.com";
        bool resultado = ValidadorCorreos.TieneFormatoValido(correo);

        Assert.True(resultado);
    }


}