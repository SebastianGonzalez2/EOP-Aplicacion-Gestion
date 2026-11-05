using EOP.Gestion.Core.CasosDeUso.Proveedores;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Interfaces.Proveedores;
using EOP.Gestion.Infrastructure.Telefonia;
using EOP.Gestion.Tests.Compartido;

namespace EOP.Gestion.Tests.Unitarias;

// Acá probamos la decisión del caso de uso: cuándo guarda, cuándo no y qué le devuelve al usuario.
// El repositorio es falso para poder simular respuestas que con una base de verdad cuesta provocar,
// como el duplicado que aparece recién al guardar. Las reglas de cada dato ya tienen sus pruebas en
// ValidarDatosProveedorTests: acá solo comprobamos que una regla incumplida frena el guardado.
public class RegistrarProveedorTests
{
    private class RepositorioProveedoresFalso : IRepositorioProveedores
    {
        public List<Proveedor> ProveedoresGuardados { get; } = new();
        public List<string> NombresRegistrados { get; } = new();
        public List<string> RutsRegistrados { get; } = new();

        // Lo que va a contestar AgregarAsync, para simular lo que descubre la base al guardar.
        public ResultadoAgregarProveedor ResultadoAlAgregar { get; set; } = ResultadoAgregarProveedor.Agregado;
        public Exception? FallaAlAgregar { get; set; }

        public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NombresRegistrados.Contains(nombreNormalizado));
        }

        public Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RutsRegistrados.Contains(rutNormalizado));
        }

        public Task<ResultadoAgregarProveedor> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
        {
            if (FallaAlAgregar != null)
            {
                throw FallaAlAgregar;
            }

            if (ResultadoAlAgregar == ResultadoAgregarProveedor.Agregado)
            {
                ProveedoresGuardados.Add(proveedor);
            }

            return Task.FromResult(ResultadoAlAgregar);
        }
    }

    private const string PaisValido = "UY";
    private const string NumeroValido = "098 765 432";
    private const string NumeroValidoInternacional = "+59898765432";

    private readonly RepositorioProveedoresFalso _repositorio = new();
    private readonly RegistrarProveedor _registrarProveedor;

    public RegistrarProveedorTests()
    {
        var validador = new ValidarDatosProveedor(_repositorio, new ServicioTelefonos());
        _registrarProveedor = new RegistrarProveedor(validador, _repositorio);
    }

    private static Proveedor CrearProveedorValido(string nombre = "Fivisa S.A.", string rut = "21.552.175-0017")
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor(nombre, rut);

        // Lo vaciamos para que el único teléfono posible sea el que arma la validación.
        proveedor.Telefono = string.Empty;

        return proveedor;
    }

    private Task<List<string>> RegistrarAsync(Proveedor proveedor)
    {
        return _registrarProveedor.EjecutarAsync(proveedor, PaisValido, NumeroValido);
    }

    // ---------- Registro correcto ----------

    [Fact]
    public async Task EjecutarAsync_ConDatosValidos_DebeGuardarElProveedorYNoDevolverErrores()
    {
        Proveedor proveedor = CrearProveedorValido();

        List<string> errores = await RegistrarAsync(proveedor);

        Assert.Empty(errores);
        Assert.Same(proveedor, Assert.Single(_repositorio.ProveedoresGuardados));
    }

    [Fact]
    public async Task EjecutarAsync_DebeGuardarRutYTelefonoNormalizados()
    {
        await RegistrarAsync(CrearProveedorValido());

        Proveedor guardado = Assert.Single(_repositorio.ProveedoresGuardados);
        Assert.Equal("215521750017", guardado.Rut);
        Assert.Equal(NumeroValidoInternacional, guardado.Telefono);
    }

    [Fact]
    public async Task EjecutarAsync_ConVariasCuentas_DebeGuardarUnSoloProveedorConTodasSusCuentas()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("333"));

        List<string> errores = await RegistrarAsync(proveedor);

        Assert.Empty(errores);
        Assert.Equal(3, Assert.Single(_repositorio.ProveedoresGuardados).CuentasBancarias.Count);
    }

    [Fact]
    public async Task EjecutarAsync_ConLosDosCorreos_DebeConservarlosPorSeparado()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoContacto = "contacto@ejemplo.com";
        proveedor.CorreoFacturacion = "facturas@ejemplo.com";

        await RegistrarAsync(proveedor);

        Proveedor guardado = Assert.Single(_repositorio.ProveedoresGuardados);
        Assert.Equal("contacto@ejemplo.com", guardado.CorreoContacto);
        Assert.Equal("facturas@ejemplo.com", guardado.CorreoFacturacion);
    }

    [Theory]
    [InlineData("contacto@ejemplo.com", null)]
    [InlineData(null, "facturas@ejemplo.com")]
    public async Task EjecutarAsync_ConUnSoloCorreo_DebeGuardarConservandoElInformado(string? correoContacto, string? correoFacturacion)
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoContacto = correoContacto;
        proveedor.CorreoFacturacion = correoFacturacion;

        List<string> errores = await RegistrarAsync(proveedor);

        Assert.Empty(errores);
        Proveedor guardado = Assert.Single(_repositorio.ProveedoresGuardados);
        Assert.Equal(correoContacto, guardado.CorreoContacto);
        Assert.Equal(correoFacturacion, guardado.CorreoFacturacion);
    }

    // ---------- Datos inválidos: no se guarda nada ----------

    [Fact]
    public async Task EjecutarAsync_ConNombreYaRegistrado_NoDebeGuardar()
    {
        _repositorio.NombresRegistrados.Add("FIVISA S.A.");

        List<string> errores = await RegistrarAsync(CrearProveedorValido());

        Assert.Equal(ValidarDatosProveedor.MensajeNombreDuplicado, Assert.Single(errores));
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    [Fact]
    public async Task EjecutarAsync_ConRutYaRegistrado_NoDebeGuardar()
    {
        _repositorio.RutsRegistrados.Add("215521750017");

        List<string> errores = await RegistrarAsync(CrearProveedorValido());

        Assert.Equal(ValidarDatosProveedor.MensajeRutDuplicado, Assert.Single(errores));
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    [Fact]
    public async Task EjecutarAsync_ConRutInvalido_NoDebeGuardar()
    {
        List<string> errores = await RegistrarAsync(CrearProveedorValido(rut: "215521750018"));

        Assert.Equal("El RUT ingresado no es válido.", Assert.Single(errores));
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    [Fact]
    public async Task EjecutarAsync_ConTelefonoInvalido_NoDebeGuardar()
    {
        List<string> errores = await _registrarProveedor.EjecutarAsync(CrearProveedorValido(), "UY", "099123");

        Assert.Contains("no es válido para Uruguay", Assert.Single(errores));
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    [Fact]
    public async Task EjecutarAsync_ConLosDosCorreosVacios_NoDebeGuardar()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CorreoContacto = null;
        proveedor.CorreoFacturacion = "   ";

        List<string> errores = await RegistrarAsync(proveedor);

        Assert.Equal("Debe informar al menos un correo electrónico.", Assert.Single(errores));
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    [Fact]
    public async Task EjecutarAsync_SinCuentasBancarias_NoDebeGuardar()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias.Clear();

        List<string> errores = await RegistrarAsync(proveedor);

        Assert.Equal("Debe informar al menos una cuenta bancaria.", Assert.Single(errores));
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    // Que la primera cuenta esté bien no alcanza: si una falla, no se guarda ninguna (RF-23).
    [Fact]
    public async Task EjecutarAsync_ConUnaCuentaIncompletaEntreOtrasValidas_NoDebeGuardarNada()
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias.Add(new CuentaBancaria { Banco = Banco.BancoSantander, Moneda = Moneda.USD, NumeroCuenta = "222" });

        List<string> errores = await RegistrarAsync(proveedor);

        Assert.Equal("Cuenta bancaria 2: Ingresá el titular de la cuenta.", Assert.Single(errores));
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    [Theory]
    [InlineData(99, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    [InlineData(1, 0)]
    public async Task EjecutarAsync_ConBancoOMonedaFueraDeLaLista_NoDebeGuardar(int banco, int moneda)
    {
        Proveedor proveedor = CrearProveedorValido();
        proveedor.CuentasBancarias[0].Banco = (Banco)banco;
        proveedor.CuentasBancarias[0].Moneda = (Moneda)moneda;

        List<string> errores = await RegistrarAsync(proveedor);

        Assert.Single(errores);
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    // ---------- Lo que se descubre recién al guardar ----------

    // La validación no encontró nada, pero otro usuario guardó primero y la base rechazó el alta.
    // El usuario tiene que recibir el mensaje de siempre y no un aviso de éxito.
    [Theory]
    [InlineData(ResultadoAgregarProveedor.NombreDuplicado, ValidarDatosProveedor.MensajeNombreDuplicado)]
    [InlineData(ResultadoAgregarProveedor.RutDuplicado, ValidarDatosProveedor.MensajeRutDuplicado)]
    public async Task EjecutarAsync_SiLaBaseDetectaUnDuplicadoAlGuardar_DebeInformarlo(ResultadoAgregarProveedor resultado, string mensajeEsperado)
    {
        _repositorio.ResultadoAlAgregar = resultado;

        List<string> errores = await RegistrarAsync(CrearProveedorValido());

        Assert.Equal(mensajeEsperado, Assert.Single(errores));
        Assert.Empty(_repositorio.ProveedoresGuardados);
    }

    // Una falla inesperada no se puede disfrazar de error de validación ni de éxito: tiene que
    // llegar hasta el manejo de errores de la aplicación, que la registra.
    [Fact]
    public async Task EjecutarAsync_SiFallaElGuardado_NoDebeOcultarLaExcepcion()
    {
        var falla = new InvalidOperationException("La base no responde.");
        _repositorio.FallaAlAgregar = falla;

        Exception recibida = await Assert.ThrowsAsync<InvalidOperationException>(() => RegistrarAsync(CrearProveedorValido()));

        Assert.Same(falla, recibida);
    }
}
