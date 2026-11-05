using EOP.Gestion.Core.DTOs.Clientes;
using EOP.Gestion.Infrastructure.Telefonia;
using PhoneNumbers;

namespace EOP.Gestion.Tests.Unitarias;

// Estas pruebas usan la biblioteca de telefonía de verdad, sin simularla: justamente
// queremos comprobar que nuestra conversión respeta las reglas de cada país.
public class ServicioTelefonosTests
{
    private readonly ServicioTelefonos _servicio = new();

    [Fact]
    public void ConvertirAInternacional_UruguayCelular_DebeArmarElNumeroCompleto()
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional("UY", "099123456");

        Assert.Equal("+59899123456", resultado.NumeroInternacional);
        Assert.Null(resultado.Error);
    }

    [Theory]
    [InlineData("099 123 456")]
    [InlineData("099-123-456")]
    [InlineData("099.123.456")]
    [InlineData("(099) 123-456")]
    [InlineData("  099123456  ")]
    public void ConvertirAInternacional_DebeAceptarSeparadoresPermitidos(string numero)
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional("UY", numero);

        Assert.Equal("+59899123456", resultado.NumeroInternacional);
    }

    [Theory]
    [InlineData("AR", "011 15 2345-6789", "+5491123456789")] // El 15 del celular argentino se transforma en un 9.
    [InlineData("BR", "(11) 96123-4567", "+5511961234567")]
    [InlineData("ES", "612 34 56 78", "+34612345678")]
    [InlineData("MX", "55 1234 5678", "+525512345678")]
    [InlineData("DE", "0151 12345678", "+4915112345678")]
    [InlineData("IT", "06 1234 5678", "+390612345678")]      // En Italia el 0 inicial NO se saca.
    public void ConvertirAInternacional_DebeRespetarLasReglasDeCadaPais(string pais, string numero, string esperado)
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional(pais, numero);

        Assert.Equal(esperado, resultado.NumeroInternacional);
    }

    [Theory]
    [InlineData("US", "CA", "(202) 555-0123", "+12025550123")] // Washington es de Estados Unidos.
    [InlineData("CA", "US", "(416) 555-0123", "+14165550123")] // Toronto es de Canadá.
    [InlineData("KZ", "RU", "701 123 4567", "+77011234567")]   // +7 lo comparten Kazajistán y Rusia.
    [InlineData("GG", "GB", "07781 123456", "+447781123456")]  // +44 lo comparten Guernsey y Reino Unido.
    public void ConvertirAInternacional_ConPrefijoCompartido_DebeDistinguirElPaisElegido(
        string paisDelNumero, string otroPaisConMismoPrefijo, string numero, string esperado)
    {
        Assert.Equal(esperado, _servicio.ConvertirAInternacional(paisDelNumero, numero).NumeroInternacional);

        ResultadoNumeroInternacional conOtroPais = _servicio.ConvertirAInternacional(otroPaisConMismoPrefijo, numero);

        Assert.Null(conOtroPais.NumeroInternacional);
        Assert.Contains("no es válido para", conOtroPais.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ZZ")]          // No existe.
    [InlineData("uy")]          // El código tiene que ser exacto.
    [InlineData("UY ")]
    [InlineData("598")]         // Un prefijo no identifica al país.
    [InlineData("+598")]
    [InlineData("001")]         // Región "no geográfica" de la biblioteca: no es un país.
    [InlineData("<script>")]
    public void ConvertirAInternacional_DebeRechazarPaisInexistenteOManipulado(string? pais)
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional(pais, "099123456");

        Assert.Null(resultado.NumeroInternacional);
        Assert.Equal("Seleccioná un país válido para el número de WhatsApp.", resultado.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("- . ( )")]     // Solo separadores: no hay número.
    public void ConvertirAInternacional_DebeRechazarNumeroVacio(string? numero)
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional("UY", numero);

        Assert.Null(resultado.NumeroInternacional);
        Assert.Equal("Ingresá el número de WhatsApp.", resultado.Error);
    }

    // Todos estos la biblioteca sola los aceptaría o los reinterpretaría.
    [Theory]
    [InlineData("099-CALL")]            // Letras que un teclado de teléfono convertiría en números.
    [InlineData("099123456 ext 5")]     // Extensión.
    [InlineData("tel:099123456")]
    [InlineData("+59899123456")]        // Con el prefijo escrito a mano.
    [InlineData("٠٩٩١٢٣٤٥٦")]           // Dígitos de otro alfabeto.
    [InlineData("099123456<b>")]
    public void ConvertirAInternacional_DebeRechazarCaracteresNoPermitidos(string numero)
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional("UY", numero);

        Assert.Null(resultado.NumeroInternacional);
        Assert.Contains("solo puede tener dígitos", resultado.Error);
    }

    [Fact]
    public void ConvertirAInternacional_DebeRechazarTextoDemasiadoLargo()
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional("UY", new string('9', 31));

        Assert.Null(resultado.NumeroInternacional);
        Assert.Equal("El número de WhatsApp es demasiado largo.", resultado.Error);
    }

    [Theory]
    [InlineData("099123")]               // Le faltan dígitos.
    [InlineData("0991234567")]           // Le sobra un dígito.
    [InlineData("0123456789012345678")]  // Más largo que cualquier número posible.
    [InlineData("123")]
    [InlineData("0")]
    [InlineData("005491123456789")]      // Un número argentino con discado internacional, con Uruguay elegido.
    public void ConvertirAInternacional_DebeRechazarNumeroQueNoEsValidoParaElPais(string numero)
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional("UY", numero);

        Assert.Null(resultado.NumeroInternacional);
        Assert.Equal(
            "El número de WhatsApp no es válido para Uruguay (+598). Revisá el número y el país seleccionado.",
            resultado.Error);
    }

    // Proveedores usa el mismo servicio, y sus mensajes tienen que hablar de su teléfono.
    [Theory]
    [InlineData("ZZ", "099123456", "Seleccioná un país válido para el número de teléfono.")]
    [InlineData("UY", "", "Ingresá el número de teléfono.")]
    [InlineData("UY", "099123", "El número de teléfono no es válido para Uruguay (+598). Revisá el número y el país seleccionado.")]
    public void ConvertirAInternacional_ConNombreDelNumero_DebeUsarloEnElMensaje(string pais, string numero, string errorEsperado)
    {
        ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional(pais, numero, "teléfono");

        Assert.Null(resultado.NumeroInternacional);
        Assert.Equal(errorEsperado, resultado.Error);
    }

    // Para no conformarnos con los países que se nos ocurrieron: le pedimos a la biblioteca
    // un número de ejemplo de CADA país, escrito como se usa ahí, y comprobamos que se convierta bien.
    [Fact]
    public void ConvertirAInternacional_DebeFuncionarParaTodosLosPaisesDeLaLista()
    {
        PhoneNumberUtil telefonos = PhoneNumberUtil.GetInstance();
        int paisesProbados = 0;

        foreach (PaisTelefonico pais in _servicio.ObtenerPaises())
        {
            PhoneNumber? ejemplo = telefonos.GetExampleNumber(pais.CodigoIso);

            if (ejemplo == null)
            {
                continue;
            }

            string numeroNacional = telefonos.Format(ejemplo, PhoneNumberFormat.NATIONAL);

            ResultadoNumeroInternacional resultado = _servicio.ConvertirAInternacional(pais.CodigoIso, numeroNacional);

            Assert.True(
                resultado.NumeroInternacional == telefonos.Format(ejemplo, PhoneNumberFormat.E164),
                $"{pais.Descripcion}: '{numeroNacional}' dio '{resultado.NumeroInternacional}'. Error: {resultado.Error}");

            paisesProbados++;
        }

        // Si la biblioteca dejara de dar ejemplos, la prueba no estaría comprobando nada.
        Assert.True(paisesProbados > 200, $"Solo se probaron {paisesProbados} países.");
    }

    [Theory]
    [InlineData("UY", "Uruguay (+598)")]
    [InlineData("AR", "Argentina (+54)")]
    [InlineData("BR", "Brasil (+55)")]
    [InlineData("ES", "España (+34)")]
    public void ObtenerPaises_DebeIncluirPaisConNombreYPrefijo(string codigoIso, string descripcion)
    {
        PaisTelefonico pais = Assert.Single(_servicio.ObtenerPaises(), p => p.CodigoIso == codigoIso);

        Assert.Equal(descripcion, pais.Descripcion);
    }

    [Fact]
    public void ObtenerPaises_DebeSerUnaListaInternacionalCompleta()
    {
        IReadOnlyList<PaisTelefonico> paises = _servicio.ObtenerPaises();

        Assert.True(paises.Count > 200, $"La lista solo tiene {paises.Count} países.");

        // Cada país se identifica de forma inequívoca, aunque compartan prefijo.
        Assert.Equal(paises.Count, paises.Select(p => p.CodigoIso).Distinct().Count());

        var prefijoUno = paises.Where(p => p.Prefijo == 1).Select(p => p.CodigoIso).ToList();
        Assert.Contains("US", prefijoUno);
        Assert.Contains("CA", prefijoUno);
    }

    [Fact]
    public void ObtenerPaises_DebeTenerNombreEnEspanolParaTodos()
    {
        // Si esta prueba falla al actualizar la biblioteca, hay una región nueva
        // sin nombre: hay que sumarla a NombresQueFaltanEnLaBiblioteca.
        var sinNombre = _servicio.ObtenerPaises()
            .Where(p => string.IsNullOrWhiteSpace(p.Nombre) || p.Nombre == p.CodigoIso)
            .Select(p => p.CodigoIso);

        Assert.Empty(sinNombre);
    }

    [Fact]
    public void ObtenerPaises_DebeEstarOrdenadaPorNombre()
    {
        var nombres = _servicio.ObtenerPaises().Select(p => p.Nombre).ToList();

        Assert.True(nombres.IndexOf("Argentina") < nombres.IndexOf("Brasil"));
        Assert.True(nombres.IndexOf("Canadá") < nombres.IndexOf("Uruguay"));
    }
}
