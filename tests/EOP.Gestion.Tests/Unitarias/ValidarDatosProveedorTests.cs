using EOP.Gestion.Core.CasosDeUso.Proveedores;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Interfaces.Proveedores;
using EOP.Gestion.Infrastructure.Telefonia;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

// El repositorio es falso porque acá probamos las reglas, no la base. El servicio de teléfonos,
// en cambio, es el de verdad: no toca nada externo y queremos ver los mensajes reales.
public class ValidarDatosProveedorTests
{
    private class RepositorioProveedoresFalso : IRepositorioProveedores
    {
        // Lo que "ya está guardado". Se compara tal cual, igual que hace la base con los valores
        // normalizados: si la normalización fallara, estas listas no encontrarían el duplicado.
        public List<string> NombresRegistrados { get; } = new();
        public List<string> RutsRegistrados { get; } = new();

        public string? NombreConsultado { get; private set; }
        public string? RutConsultado { get; private set; }

        public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default)
        {
            NombreConsultado = nombreNormalizado;
            return Task.FromResult(NombresRegistrados.Contains(nombreNormalizado));
        }

        public Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default)
        {
            RutConsultado = rutNormalizado;
            return Task.FromResult(RutsRegistrados.Contains(rutNormalizado));
        }
    }

    private const string PaisValido = "UY";
    private const string NumeroValido = "098 765 432";
    private const string NumeroValidoInternacional = "+59898765432";

    private readonly RepositorioProveedoresFalso _repositorio = new();
    private readonly ValidarDatosProveedor _validador;

    public ValidarDatosProveedorTests()
    {
        _validador = new ValidarDatosProveedor(_repositorio, new ServicioTelefonos());
    }

    // El proveedor de prueba viene con un teléfono ya cargado. Lo vaciamos para que el único
    // teléfono posible sea el que arma la validación a partir de los datos del formulario.
    private static Proveedor CrearProveedorValido(string nombre = "Fivisa S.A.", string rut = "21.552.175-0017")
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor(nombre, rut);
        proveedor.Telefono = string.Empty;

        return proveedor;
    }

    private Task<List<string>> ValidarAsync(Proveedor proveedor)
    {
        return _validador.ValidarAsync(proveedor, PaisValido, NumeroValido);
    }

    // ---------- Proveedor completo ----------

    [Fact]
    public async Task ValidarAsync_ConDatosCorrectos_NoDebeDevolverErroresYDebeNormalizar()
    {
        Proveedor proveedor = CrearProveedorValido();

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Empty(errores);
        Assert.Equal("215521750017", proveedor.Rut);
        Assert.Equal(NumeroValidoInternacional, proveedor.Telefono);
    }

    [Fact]
    public async Task ValidarAsync_ConVariosProblemas_DebeInformarlosTodosJuntos()
    {
        Proveedor proveedor = new Proveedor
        {
            Nombre = "",
            Rut = "",
            CorreoContacto = null,
            CorreoFacturacion = null
        };

        List<string> errores = await _validador.ValidarAsync(proveedor, PaisValido, "");

        Assert.Equal(
            [
                "El nombre del proveedor es obligatorio.",
                "El RUT del proveedor es obligatorio.",
                "Ingresá el número de teléfono.",
                "Debe informar al menos un correo electrónico.",
                "Debe informar al menos una cuenta bancaria."
            ],
            errores);
    }

    // Si hay errores el formulario se vuelve a mostrar: no tiene sentido tocar los datos.
    [Fact]
    public async Task ValidarAsync_ConErrores_NoDebeModificarLosDatosDelProveedor()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoFacturacion = "   ";
        proveedor.CuentasBancarias.Clear();

        List<string> errores = await ValidarAsync(proveedor);

        Assert.NotEmpty(errores);
        Assert.Equal("21.552.175-0017", proveedor.Rut);
        Assert.Equal(string.Empty, proveedor.Telefono);
        Assert.Equal("   ", proveedor.CorreoFacturacion);
    }

    // ---------- Nombre ----------

    // El último es un tilde suelto: no parece vacío, pero al normalizarlo no queda nada.
    [Theory]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("\t\n")]
    [InlineData("́")]
    public async Task ValidarAsync_ConNombreVacio_DebeRechazarloSinConsultarDuplicados(string nombre)
    {
        Proveedor proveedor = CrearProveedorValido(nombre);

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal(["El nombre del proveedor es obligatorio."], errores);
        Assert.Null(_repositorio.NombreConsultado);
    }

    [Theory]
    [InlineData("  Construcciones   Pérez ", "CONSTRUCCIONES PEREZ")]
    [InlineData("construcciones perez", "CONSTRUCCIONES PEREZ")]
    [InlineData("Construcciones Pérez", "CONSTRUCCIONES PEREZ")] // La letra y el tilde escritos por separado.
    [InlineData("Peña", "PEÑA")]
    [InlineData("Güemes", "GÜEMES")]
    public async Task ValidarAsync_DebeConsultarDuplicadosConElNombreNormalizado(string nombre, string nombreEsperado)
    {
        await ValidarAsync(CrearProveedorValido(nombre));

        Assert.Equal(nombreEsperado, _repositorio.NombreConsultado);
    }

    [Theory]
    [InlineData("Construcciones Pérez")]
    [InlineData("CONSTRUCCIONES PEREZ")]
    [InlineData("construcciones perez")]
    [InlineData("  Construcciones    Perez  ")]
    [InlineData("Cónstrucciónes Pérez")]
    public async Task ValidarAsync_ConNombreDuplicado_DebeRechazarloAunqueCambieLaEscritura(string nombre)
    {
        _repositorio.NombresRegistrados.Add("CONSTRUCCIONES PEREZ");

        List<string> errores = await ValidarAsync(CrearProveedorValido(nombre));

        Assert.Equal(["Ya existe un proveedor registrado con ese nombre."], errores);
    }

    // La ñ y la ü no son tildes (RF-23): estos nombres son de proveedores distintos.
    [Theory]
    [InlineData("Pena", "PEÑA")]
    [InlineData("Peña", "PENA")]
    [InlineData("Guemes", "GÜEMES")]
    [InlineData("Güemes", "GUEMES")]
    public async Task ValidarAsync_ConEnieODieresis_NoDebeConsiderarloDuplicado(string nombre, string nombreRegistrado)
    {
        _repositorio.NombresRegistrados.Add(nombreRegistrado);

        List<string> errores = await ValidarAsync(CrearProveedorValido(nombre));

        Assert.Empty(errores);
    }

    // ---------- RUT ----------

    [Theory]
    [InlineData("215521750017")]
    [InlineData("21.552.175-0017")]
    [InlineData(" 21 552 175 0017 ")]
    public async Task ValidarAsync_ConRutValido_DebeNormalizarloADigitos(string rut)
    {
        Proveedor proveedor = CrearProveedorValido(rut: rut);

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Empty(errores);
        Assert.Equal("215521750017", _repositorio.RutConsultado);
        Assert.Equal("215521750017", proveedor.Rut);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(". - .")] // Solo separadores: no hay RUT.
    public async Task ValidarAsync_ConRutVacio_DebeRechazarloSinConsultarDuplicados(string? rut)
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.Rut = rut!;

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal(["El RUT del proveedor es obligatorio."], errores);
        Assert.Null(_repositorio.RutConsultado);
    }

    [Theory]
    [InlineData("215521750018")]   // Dígito verificador incorrecto.
    [InlineData("21552175001")]    // Le falta un dígito.
    [InlineData("2155217500170")]  // Le sobra un dígito.
    [InlineData("21552175001A")]   // Tiene una letra.
    [InlineData("21552175001٧")]   // Tiene un dígito de otro alfabeto.
    public async Task ValidarAsync_ConRutInvalido_DebeRechazarloSinConsultarDuplicados(string rut)
    {
        List<string> errores = await ValidarAsync(CrearProveedorValido(rut: rut));

        Assert.Equal(["El RUT ingresado no es válido."], errores);
        Assert.Null(_repositorio.RutConsultado);
    }

    [Fact]
    public async Task ValidarAsync_ConRutDuplicado_DebeRechazarlo()
    {
        _repositorio.RutsRegistrados.Add("215521750017");

        List<string> errores = await ValidarAsync(CrearProveedorValido(rut: "21.552.175-0017"));

        Assert.Equal(["Ya existe un proveedor registrado con ese RUT."], errores);
    }

    // RF-23: Nombre y RUT se comprueban de forma independiente, así que se informan los dos.
    [Fact]
    public async Task ValidarAsync_ConNombreYRutDuplicados_DebeInformarAmbos()
    {
        _repositorio.NombresRegistrados.Add("FIVISA S.A.");
        _repositorio.RutsRegistrados.Add("215521750017");

        List<string> errores = await ValidarAsync(CrearProveedorValido());

        Assert.Equal(
            ["Ya existe un proveedor registrado con ese nombre.", "Ya existe un proveedor registrado con ese RUT."],
            errores);
    }

    // ---------- Teléfono ----------

    [Theory]
    [InlineData("UY", "099 123 456", "+59899123456")]
    [InlineData("UY", "(099) 123-456", "+59899123456")]
    [InlineData("AR", "011 15 2345-6789", "+5491123456789")]
    public async Task ValidarAsync_ConTelefonoValido_DebeDejarloEnFormatoInternacional(string pais, string numero, string numeroEsperado)
    {
        Proveedor proveedor = CrearProveedorValido();

        List<string> errores = await _validador.ValidarAsync(proveedor, pais, numero);

        Assert.Empty(errores);
        Assert.Equal(numeroEsperado, proveedor.Telefono);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    public async Task ValidarAsync_ConTelefonoVacio_DebeRechazarlo(string? numero)
    {
        List<string> errores = await _validador.ValidarAsync(CrearProveedorValido(), PaisValido, numero);

        Assert.Equal(["Ingresá el número de teléfono."], errores);
    }

    [Fact]
    public async Task ValidarAsync_ConTelefonoQueNoEsValidoParaElPais_DebeRechazarlo()
    {
        List<string> errores = await _validador.ValidarAsync(CrearProveedorValido(), PaisValido, "099123");

        Assert.Equal(["El número de teléfono no es válido para Uruguay (+598). Revisá el número y el país seleccionado."], errores);
    }

    // Un país inventado no puede pasar aunque no esté en el selector del formulario.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ZZ")]
    public async Task ValidarAsync_ConPaisInexistente_DebeRechazarlo(string? pais)
    {
        List<string> errores = await _validador.ValidarAsync(CrearProveedorValido(), pais, NumeroValido);

        Assert.Equal(["Seleccioná un país válido para el número de teléfono."], errores);
    }

    [Theory]
    [InlineData("099-CALL")]
    [InlineData("+59899123456")]
    public async Task ValidarAsync_ConCaracteresNoPermitidosEnElTelefono_NoDebeHablarDeWhatsApp(string numero)
    {
        List<string> errores = await _validador.ValidarAsync(CrearProveedorValido(), PaisValido, numero);

        string error = Assert.Single(errores);
        Assert.Contains("número de teléfono", error);
        Assert.DoesNotContain("WhatsApp", error);
    }

    // ---------- Correos ----------

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("    ", null)]
    [InlineData(null, "    ")]
    public async Task ValidarAsync_ConAmbosCorreosVacios_DebeRechazarlo(string? correoContacto, string? correoFacturacion)
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoContacto = correoContacto;
        proveedor.CorreoFacturacion = correoFacturacion;

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal(["Debe informar al menos un correo electrónico."], errores);
    }

    [Theory]
    [InlineData("contacto@ejemplo.com", null)]
    [InlineData(null, "facturas@ejemplo.com")]
    [InlineData("contacto@ejemplo.com", "facturas@ejemplo.com")]
    public async Task ValidarAsync_ConCorreosValidos_DebeConservarCadaUnoEnSuCampo(string? correoContacto, string? correoFacturacion)
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoContacto = correoContacto;
        proveedor.CorreoFacturacion = correoFacturacion;

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Empty(errores);
        Assert.Equal(correoContacto, proveedor.CorreoContacto);
        Assert.Equal(correoFacturacion, proveedor.CorreoFacturacion);
    }

    [Theory]
    [InlineData("contacto.ejemplo.com", null, "El correo de contacto no tiene un formato válido.")]
    [InlineData(null, "facturas.ejemplo.com", "El correo de facturación no tiene un formato válido.")]
    [InlineData("contacto@ejemplo.com", "facturas.ejemplo.com", "El correo de facturación no tiene un formato válido.")]
    [InlineData("contacto.ejemplo.com", "facturas@ejemplo.com", "El correo de contacto no tiene un formato válido.")]
    public async Task ValidarAsync_ConUnCorreoInformadoInvalido_DebeRechazarlo(string? correoContacto, string? correoFacturacion, string errorEsperado)
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoContacto = correoContacto;
        proveedor.CorreoFacturacion = correoFacturacion;

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal([errorEsperado], errores);
    }

    [Fact]
    public async Task ValidarAsync_ConAmbosCorreosInvalidos_DebeInformarAmbos()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoContacto = "contacto.ejemplo.com";
        proveedor.CorreoFacturacion = "facturas.ejemplo.com";

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal(
            ["El correo de contacto no tiene un formato válido.", "El correo de facturación no tiene un formato válido."],
            errores);
    }

    [Fact]
    public async Task ValidarAsync_ConCorreoSoloDeEspacios_DebeDejarloSinInformar()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoContacto = "contacto@ejemplo.com";
        proveedor.CorreoFacturacion = "    ";

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Empty(errores);
        Assert.Equal("contacto@ejemplo.com", proveedor.CorreoContacto);
        Assert.Null(proveedor.CorreoFacturacion);
    }

    // ---------- Cuentas bancarias ----------

    [Fact]
    public async Task ValidarAsync_SinCuentas_DebeRechazarlo()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias.Clear();

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal(["Debe informar al menos una cuenta bancaria."], errores);
    }

    [Fact]
    public async Task ValidarAsync_ConCuentaIncompleta_DebeInformarCadaDatoQueFalta()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias.Clear();
        proveedor.CuentasBancarias.Add(new CuentaBancaria());

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal(
            [
                "Cuenta bancaria 1: Seleccioná un banco de la lista.",
                "Cuenta bancaria 1: Ingresá el número de cuenta.",
                "Cuenta bancaria 1: Ingresá el titular de la cuenta.",
                "Cuenta bancaria 1: Seleccioná una moneda válida: UYU o USD."
            ],
            errores);
    }

    [Fact]
    public async Task ValidarAsync_ConBancoYMonedaInvalidos_DebeRechazarlos()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias[0].Banco = (Banco)99;
        proveedor.CuentasBancarias[0].Moneda = (Moneda)99;

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal(
            ["Cuenta bancaria 1: Seleccioná un banco de la lista.", "Cuenta bancaria 1: Seleccioná una moneda válida: UYU o USD."],
            errores);
    }

    [Fact]
    public async Task ValidarAsync_ConVariasCuentasCorrectas_DebeConservarlasTodas()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("333"));

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Empty(errores);
        Assert.Equal(3, proveedor.CuentasBancarias.Count);
    }

    // Que la primera esté bien no alcanza: se tienen que revisar todas, y el mensaje tiene que
    // decir cuál es la que hay que corregir.
    [Fact]
    public async Task ValidarAsync_ConCuentasCorrectasEIncorrectas_DebeInformarSoloLasIncorrectas()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias.Clear();
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("111"));
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta(""));
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("333"));

        CuentaBancaria cuentaSinTitular = DatosDePruebaProveedores.CrearCuenta("444");
        cuentaSinTitular.Titular = "   ";
        proveedor.CuentasBancarias.Add(cuentaSinTitular);

        List<string> errores = await ValidarAsync(proveedor);

        Assert.Equal(
            ["Cuenta bancaria 2: Ingresá el número de cuenta.", "Cuenta bancaria 4: Ingresá el titular de la cuenta."],
            errores);
    }
}
