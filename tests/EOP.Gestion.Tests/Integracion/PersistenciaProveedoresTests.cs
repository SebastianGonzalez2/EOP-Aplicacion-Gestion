using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// Guarda y recupera proveedores en una base SQLite temporal, para que estas pruebas corran en
// cualquier máquina. SQLite no controla las longitudes máximas ni la migración: eso se prueba
// contra SQL Server real en PersistenciaProveedoresSqlServerTests.
public class PersistenciaProveedoresTests : IDisposable
{
    private readonly SqliteConnection _conexion;

    public PersistenciaProveedoresTests()
    {
        // La base en memoria vive mientras la conexión esté abierta, por eso la compartimos
        // entre todos los contextos de la prueba.
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();

        using EopDbContext contexto = CrearContexto();
        contexto.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _conexion.Dispose();
    }

    // Para leer usamos siempre un contexto nuevo. Si reusáramos el que guardó, EF nos
    // devolvería los objetos que ya tiene en memoria y no probaríamos lo que quedó en la base.
    private EopDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(_conexion)
            .Options;

        return new EopDbContext(opciones);
    }

    private async Task GuardarAsync(Proveedor proveedor)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();
    }

    // Comprueba que la base rechace el proveedor y que sea por el motivo esperado. Sin mirar
    // el motivo, la prueba podría pasar porque falló otra restricción.
    private async Task ComprobarQueLaBaseRechazaAsync(Proveedor proveedor, string motivoEsperado)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Proveedores.Add(proveedor);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains(motivoEsperado, excepcion.GetBaseException().Message);
    }

    private Task ComprobarQueLaBaseRechazaLaCuentaAsync(CuentaBancaria cuenta, string motivoEsperado)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.CuentasBancarias.Clear();
        proveedor.CuentasBancarias.Add(cuenta);

        return ComprobarQueLaBaseRechazaAsync(proveedor, motivoEsperado);
    }

    [Fact]
    public async Task Proveedor_DebeRecuperarseConTodosSusDatos()
    {
        var proveedorNuevo = new Proveedor
        {
            Nombre = "  Fivisa   S.A. ",
            Rut = "215521750017",
            Telefono = "+59899123456",
            CorreoContacto = "contacto@fivisa.com",
            CorreoFacturacion = "facturas@fivisa.com"
        };
        proveedorNuevo.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta());
        await GuardarAsync(proveedorNuevo);

        using EopDbContext contexto = CrearContexto();
        Proveedor proveedor = await contexto.Proveedores.SingleAsync();

        Assert.Equal("  Fivisa   S.A. ", proveedor.Nombre);
        Assert.Equal("FIVISA S.A.", proveedor.NombreNormalizado);
        Assert.Equal("215521750017", proveedor.Rut);
        Assert.Equal("+59899123456", proveedor.Telefono);
        Assert.Equal("contacto@fivisa.com", proveedor.CorreoContacto);
        Assert.Equal("facturas@fivisa.com", proveedor.CorreoFacturacion);
    }

    [Fact]
    public async Task Proveedor_ConVariasCuentas_DebeRecuperarseConTodasSusCuentas()
    {
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor();
        proveedorNuevo.CuentasBancarias.Clear();
        proveedorNuevo.CuentasBancarias.Add(new CuentaBancaria
        {
            Banco = Banco.BancoItauUruguay,
            NumeroCuenta = "0012-345",
            Titular = "Fivisa S.A.",
            Moneda = Moneda.UYU
        });
        proveedorNuevo.CuentasBancarias.Add(new CuentaBancaria
        {
            Banco = Banco.BancoSantander,
            NumeroCuenta = "9876",
            Titular = "Fivisa Uruguay",
            Moneda = Moneda.USD
        });
        await GuardarAsync(proveedorNuevo);

        using EopDbContext contexto = CrearContexto();

        // Include le pide a EF que traiga también las cuentas: sin él, la lista vendría vacía.
        Proveedor proveedor = await contexto.Proveedores
            .Include(p => p.CuentasBancarias)
            .SingleAsync();

        Assert.Equal(2, proveedor.CuentasBancarias.Count);

        CuentaBancaria cuentaEnPesos = Assert.Single(proveedor.CuentasBancarias, c => c.Moneda == Moneda.UYU);
        Assert.Equal(Banco.BancoItauUruguay, cuentaEnPesos.Banco);
        Assert.Equal("0012-345", cuentaEnPesos.NumeroCuenta);
        Assert.Equal("Fivisa S.A.", cuentaEnPesos.Titular);
        Assert.Equal(proveedor.Id, cuentaEnPesos.ProveedorId);

        CuentaBancaria cuentaEnDolares = Assert.Single(proveedor.CuentasBancarias, c => c.Moneda == Moneda.USD);
        Assert.Equal(Banco.BancoSantander, cuentaEnDolares.Banco);
        Assert.Equal("9876", cuentaEnDolares.NumeroCuenta);
        Assert.Equal("Fivisa Uruguay", cuentaEnDolares.Titular);
        Assert.Equal(proveedor.Id, cuentaEnDolares.ProveedorId);
    }

    [Fact]
    public async Task Cuentas_DebenRecuperarseSoloConSuProveedor()
    {
        Proveedor primerProveedor = DatosDePruebaProveedores.CrearProveedor("Primero", "215521750017");
        primerProveedor.CuentasBancarias.Clear();
        primerProveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("111"));
        await GuardarAsync(primerProveedor);

        Proveedor segundoProveedor = DatosDePruebaProveedores.CrearProveedor("Segundo", "210000000019");
        segundoProveedor.CuentasBancarias.Clear();
        segundoProveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));
        await GuardarAsync(segundoProveedor);

        using EopDbContext contexto = CrearContexto();
        Proveedor proveedor = await contexto.Proveedores
            .Include(p => p.CuentasBancarias)
            .SingleAsync(p => p.Nombre == "Primero");

        CuentaBancaria cuenta = Assert.Single(proveedor.CuentasBancarias);
        Assert.Equal("111", cuenta.NumeroCuenta);
    }

    // RF-102: agregar una cuenta no crea otro Proveedor ni reemplaza las que ya estaban.
    [Fact]
    public async Task AgregarCuenta_AProveedorExistente_DebeConservarLasAnteriores()
    {
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor();
        proveedorNuevo.CuentasBancarias.Clear();
        proveedorNuevo.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("111"));
        await GuardarAsync(proveedorNuevo);

        using (EopDbContext contexto = CrearContexto())
        {
            Proveedor proveedorGuardado = await contexto.Proveedores
                .Include(p => p.CuentasBancarias)
                .SingleAsync();

            proveedorGuardado.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));
            await contexto.SaveChangesAsync();
        }

        using EopDbContext contextoDeLectura = CrearContexto();
        Proveedor proveedor = await contextoDeLectura.Proveedores
            .Include(p => p.CuentasBancarias)
            .SingleAsync();

        Assert.Equal(2, proveedor.CuentasBancarias.Count);
        Assert.Contains(proveedor.CuentasBancarias, c => c.NumeroCuenta == "111");
        Assert.Contains(proveedor.CuentasBancarias, c => c.NumeroCuenta == "222");
    }

    [Fact]
    public async Task Cuenta_DeUnProveedorInexistente_DebeRechazarse()
    {
        using EopDbContext contexto = CrearContexto();
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.ProveedorId = 999;
        contexto.CuentasBancarias.Add(cuenta);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("FOREIGN KEY", excepcion.GetBaseException().Message);
    }

    [Fact]
    public async Task BorrarProveedor_ConCuentas_DebeRechazarseYConservarLasCuentas()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor());

        using (EopDbContext contexto = CrearContexto())
        {
            Proveedor proveedor = await contexto.Proveedores.SingleAsync();
            contexto.Proveedores.Remove(proveedor);

            DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

            Assert.Contains("FOREIGN KEY", excepcion.GetBaseException().Message);
        }

        using EopDbContext contextoDeLectura = CrearContexto();
        Assert.Equal(1, await contextoDeLectura.Proveedores.CountAsync());
        Assert.Equal(1, await contextoDeLectura.CuentasBancarias.CountAsync());
    }

    [Theory]
    [InlineData("Fivisa S.A.")]
    [InlineData("FIVISA S.A.")]
    [InlineData("fivisa s.a.")]
    [InlineData("  Fivisa S.A.  ")]
    [InlineData("Fivisa    S.A.")]
    [InlineData("Fívisa S.Á.")]
    public async Task Nombre_RepetidoAunqueCambienMayusculasEspaciosOTildes_DebeRechazarse(string nombreRepetido)
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa S.A.", "215521750017"));

        Proveedor proveedorRepetido = DatosDePruebaProveedores.CrearProveedor(nombreRepetido, "210000000019");

        await ComprobarQueLaBaseRechazaAsync(proveedorRepetido, "Proveedores.NombreNormalizado");
    }

    [Fact]
    public async Task Nombre_Distinto_DebePermitirse()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa S.A.", "215521750017"));
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa Uruguay", "210000000019"));

        using EopDbContext contexto = CrearContexto();
        Assert.Equal(2, await contexto.Proveedores.CountAsync());
    }

    // RF-109: la regla también vale al corregir un nombre, porque la columna normalizada se
    // recalcula sola al asignar Nombre.
    [Fact]
    public async Task Nombre_CorregidoAlDeOtroProveedor_DebeRechazarse()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa", "215521750017"));
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Otro nombre", "210000000019"));

        using EopDbContext contexto = CrearContexto();
        Proveedor proveedor = await contexto.Proveedores.SingleAsync(p => p.Nombre == "Otro nombre");
        proveedor.Nombre = " fivisa ";

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("Proveedores.NombreNormalizado", excepcion.GetBaseException().Message);
    }

    [Fact]
    public async Task Nombre_Corregido_DebeActualizarElNormalizadoEnLaBase()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa"));

        using (EopDbContext contexto = CrearContexto())
        {
            Proveedor proveedorGuardado = await contexto.Proveedores.SingleAsync();
            proveedorGuardado.Nombre = "Fivisa Uruguay";
            await contexto.SaveChangesAsync();
        }

        using EopDbContext contextoDeLectura = CrearContexto();
        Proveedor proveedor = await contextoDeLectura.Proveedores.SingleAsync();
        Assert.Equal("FIVISA URUGUAY", proveedor.NombreNormalizado);
    }

    [Fact]
    public async Task Rut_Repetido_DebeRechazarse()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Primero", "215521750017"));

        Proveedor proveedorRepetido = DatosDePruebaProveedores.CrearProveedor("Segundo", "215521750017");

        await ComprobarQueLaBaseRechazaAsync(proveedorRepetido, "Proveedores.Rut");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task Nombre_Vacio_DebeRechazarse(string nombre)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor(nombre);

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_Proveedores_NombreNoVacio");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task Telefono_Vacio_DebeRechazarse(string telefono)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.Telefono = telefono;

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_Proveedores_TelefonoNoVacio");
    }

    [Theory]
    [InlineData("")]
    [InlineData("21552175001")]
    [InlineData("2155217500170")]
    public async Task Rut_SinDoceCaracteres_DebeRechazarse(string rut)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor(rut: rut);

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_Proveedores_RutFormato");
    }

    [Theory]
    [InlineData("21.552.175-0")]
    [InlineData("21552175001A")]
    [InlineData("21552175001 ")]
    public async Task Rut_ConCaracteresQueNoSonDigitos_DebeRechazarse(string rut)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor(rut: rut);

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_Proveedores_RutFormato");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", null)]
    [InlineData(null, "   ")]
    public async Task Correos_SinNingunoInformado_DebeRechazarse(string? correoContacto, string? correoFacturacion)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.CorreoContacto = correoContacto;
        proveedor.CorreoFacturacion = correoFacturacion;

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_Proveedores_AlMenosUnCorreo");
    }

    [Theory]
    [InlineData("contacto@ejemplo.com", null)]
    [InlineData(null, "facturas@ejemplo.com")]
    [InlineData("contacto@ejemplo.com", "facturas@ejemplo.com")]
    public async Task Correos_ConAlMenosUnoInformado_DebenConservarseCadaUnoEnSuCampo(string? correoContacto, string? correoFacturacion)
    {
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor();
        proveedorNuevo.CorreoContacto = correoContacto;
        proveedorNuevo.CorreoFacturacion = correoFacturacion;
        await GuardarAsync(proveedorNuevo);

        using EopDbContext contexto = CrearContexto();
        Proveedor proveedor = await contexto.Proveedores.SingleAsync();

        Assert.Equal(correoContacto, proveedor.CorreoContacto);
        Assert.Equal(correoFacturacion, proveedor.CorreoFacturacion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(-1)]
    public async Task Cuenta_ConBancoFueraDeLaLista_DebeRechazarse(int numeroBanco)
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.Banco = (Banco)numeroBanco;

        await ComprobarQueLaBaseRechazaLaCuentaAsync(cuenta, "CK_CuentasBancarias_BancoValido");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public async Task Cuenta_ConMonedaFueraDeLaLista_DebeRechazarse(int numeroMoneda)
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.Moneda = (Moneda)numeroMoneda;

        await ComprobarQueLaBaseRechazaLaCuentaAsync(cuenta, "CK_CuentasBancarias_MonedaValida");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task Cuenta_SinNumero_DebeRechazarse(string numeroCuenta)
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta(numeroCuenta);

        await ComprobarQueLaBaseRechazaLaCuentaAsync(cuenta, "CK_CuentasBancarias_NumeroCuentaNoVacio");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task Cuenta_SinTitular_DebeRechazarse(string titular)
    {
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.Titular = titular;

        await ComprobarQueLaBaseRechazaLaCuentaAsync(cuenta, "CK_CuentasBancarias_TitularNoVacio");
    }

    // La lista de la restricción está escrita a mano en la configuración. Esta prueba avisa
    // si alguien agrega un banco o una moneda al enum y se olvida de sumarlo también allá.
    [Fact]
    public async Task Cuenta_DebeAceptarTodosLosBancosYMonedasDeLasListas()
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.CuentasBancarias.Clear();

        foreach (Banco banco in Enum.GetValues<Banco>())
        {
            foreach (Moneda moneda in Enum.GetValues<Moneda>())
            {
                CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
                cuenta.Banco = banco;
                cuenta.Moneda = moneda;
                proveedor.CuentasBancarias.Add(cuenta);
            }
        }

        await GuardarAsync(proveedor);

        using EopDbContext contexto = CrearContexto();
        Assert.Equal(22, await contexto.CuentasBancarias.CountAsync());
    }
}
