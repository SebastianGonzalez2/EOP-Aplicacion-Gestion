using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EOP.Gestion.Tests.Integracion;

// Las mismas tablas, pero en un SQL Server real y creadas por las migraciones, que es lo que
// va a tener la aplicación de verdad. Acá comprobamos lo que SQLite no puede demostrar: que la
// migración funciona, las longitudes máximas y cómo compara textos SQL Server.
public class PersistenciaProveedoresSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    private EopDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer(_cadenaConexion)
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

    private static async Task<List<string>> ObtenerTablasAsync(EopDbContext contexto)
    {
        return await contexto.Database
            .SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.tables")
            .ToListAsync();
    }

    [FactConSqlServer]
    public async Task Migracion_DebeCrearLasTablasNuevasYMantenerLasAnteriores()
    {
        using EopDbContext contexto = CrearContexto();

        List<string> tablas = await ObtenerTablasAsync(contexto);

        Assert.Contains("Proveedores", tablas);
        Assert.Contains("CuentasBancarias", tablas);
        Assert.Contains("Usuarios", tablas);
        Assert.Contains("Clientes", tablas);
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());
    }

    // Deshacer la migración tiene que sacar solamente lo suyo. Dejamos un cliente guardado para
    // comprobar que las tablas de las tareas anteriores no se tocan.
    [FactConSqlServer]
    public async Task Migracion_AlRevertirla_DebeQuitarSoloSusTablas()
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Clientes.Add(new Cliente
        {
            Nombre = "Cliente de prueba",
            Rut = "215521750017",
            NumeroWhatsApp = "+59899123456",
            CorreoContacto = "cliente@ejemplo.com"
        });
        await contexto.SaveChangesAsync();

        IMigrator migrador = contexto.GetService<IMigrator>();
        await migrador.MigrateAsync("S1_T18_CrearTablaClientes");

        List<string> tablas = await ObtenerTablasAsync(contexto);
        Assert.DoesNotContain("Proveedores", tablas);
        Assert.DoesNotContain("CuentasBancarias", tablas);
        Assert.Contains("Usuarios", tablas);
        Assert.Equal(1, await contexto.Clientes.CountAsync());

        // Y se tiene que poder volver a aplicar sin problemas.
        await contexto.Database.MigrateAsync();

        Assert.Contains("Proveedores", await ObtenerTablasAsync(contexto));
    }

    [FactConSqlServer]
    public async Task Proveedor_ConVariasCuentas_DebeRecuperarseConTodasSusCuentas()
    {
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor();
        proveedorNuevo.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));
        await GuardarAsync(proveedorNuevo);

        using EopDbContext contexto = CrearContexto();
        Proveedor proveedor = await contexto.Proveedores
            .Include(p => p.CuentasBancarias)
            .SingleAsync();

        Assert.Equal(2, proveedor.CuentasBancarias.Count);
        Assert.All(proveedor.CuentasBancarias, cuenta => Assert.Equal(proveedor.Id, cuenta.ProveedorId));
    }

    [FactConSqlServer]
    public async Task Cuenta_DeUnProveedorInexistente_DebeRechazarse()
    {
        using EopDbContext contexto = CrearContexto();
        CuentaBancaria cuenta = DatosDePruebaProveedores.CrearCuenta();
        cuenta.ProveedorId = 999;
        contexto.CuentasBancarias.Add(cuenta);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("FK_CuentasBancarias_Proveedores_ProveedorId", excepcion.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task BorrarProveedor_ConCuentas_DebeRechazarseYConservarLasCuentas()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor());

        using (EopDbContext contexto = CrearContexto())
        {
            Proveedor proveedor = await contexto.Proveedores.SingleAsync();
            contexto.Proveedores.Remove(proveedor);

            DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

            Assert.Contains("FK_CuentasBancarias_Proveedores_ProveedorId", excepcion.GetBaseException().Message);
        }

        using EopDbContext contextoDeLectura = CrearContexto();
        Assert.Equal(1, await contextoDeLectura.Proveedores.CountAsync());
        Assert.Equal(1, await contextoDeLectura.CuentasBancarias.CountAsync());
    }

    [TheoryConSqlServer]
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

        await ComprobarQueLaBaseRechazaAsync(proveedorRepetido, "IX_Proveedores_NombreNormalizado");
    }

    // Peña y Pena son dos proveedores distintos: la ñ no es un tilde. Esto depende de cómo compare
    // el servidor: si la base se creara con una intercalación que ignora estas marcas, esta prueba
    // fallaría y nos avisaría de que la regla dejó de cumplirse.
    [FactConSqlServer]
    public async Task Nombre_QueSoloCambiaEnUnaEnie_DebePermitirse()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Construcciones Peña", "215521750017"));
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Construcciones Pena", "210000000019"));

        using EopDbContext contexto = CrearContexto();
        Assert.Equal(2, await contexto.Proveedores.CountAsync());
    }

    [FactConSqlServer]
    public async Task Rut_Repetido_DebeRechazarse()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Primero", "215521750017"));

        Proveedor proveedorRepetido = DatosDePruebaProveedores.CrearProveedor("Segundo", "215521750017");

        await ComprobarQueLaBaseRechazaAsync(proveedorRepetido, "IX_Proveedores_Rut");
    }

    // Esta es la razón de ser de los índices únicos: dos usuarios registran el mismo proveedor a
    // la vez. Los dos preguntan si existe, como va a hacer la validación de S2-T11, y a los dos
    // les da que no. Solo la base puede frenar al segundo.
    [FactConSqlServer]
    public async Task DosAltasSimultaneas_ConElMismoNombre_SoloDebeGuardarseUna()
    {
        using EopDbContext primeraSolicitud = CrearContexto();
        using EopDbContext segundaSolicitud = CrearContexto();
        Proveedor primerProveedor = DatosDePruebaProveedores.CrearProveedor("Fivisa", "215521750017");
        Proveedor segundoProveedor = DatosDePruebaProveedores.CrearProveedor(" FIVISA ", "210000000019");

        Assert.False(await primeraSolicitud.Proveedores.AnyAsync(p => p.NombreNormalizado == primerProveedor.NombreNormalizado));
        Assert.False(await segundaSolicitud.Proveedores.AnyAsync(p => p.NombreNormalizado == segundoProveedor.NombreNormalizado));

        primeraSolicitud.Proveedores.Add(primerProveedor);
        await primeraSolicitud.SaveChangesAsync();

        segundaSolicitud.Proveedores.Add(segundoProveedor);
        await Assert.ThrowsAsync<DbUpdateException>(() => segundaSolicitud.SaveChangesAsync());

        using EopDbContext contextoDeLectura = CrearContexto();
        Assert.Equal(1, await contextoDeLectura.Proveedores.CountAsync());
    }

    // Todos estos tienen 12 caracteres, así que los frena la parte de la restricción que mira
    // cada carácter. Si entrara un RUT con separadores, el índice único ya no podría detectar el
    // duplicado. Los últimos cuatro son el motivo de no usar LIKE '%[^0-9]%': SQL Server los
    // considera dentro del rango 0-9, y esa forma los dejaría pasar.
    [TheoryConSqlServer]
    [InlineData("21.552.175-0")]
    [InlineData("21552175001A")]
    [InlineData("21552175001 ")]
    [InlineData("2155217 0017")]
    [InlineData("21552175001²")] // el 2 chiquito de "al cuadrado"
    [InlineData("21552175001½")] // un medio
    [InlineData("21552175001٢")] // un 2 en dígitos árabes
    [InlineData("21552175001１")] // un 1 de ancho completo
    public async Task Rut_ConCaracteresQueNoSonDigitos_DebeRechazarse(string rut)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor(rut: rut);

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_Proveedores_RutFormato");
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("21552175001")]
    public async Task Rut_ConMenosDeDoceCaracteres_DebeRechazarse(string rut)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor(rut: rut);

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_Proveedores_RutFormato");
    }

    [TheoryConSqlServer]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", null)]
    public async Task Correos_SinNingunoInformado_DebeRechazarse(string? correoContacto, string? correoFacturacion)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.CorreoContacto = correoContacto;
        proveedor.CorreoFacturacion = correoFacturacion;

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_Proveedores_AlMenosUnCorreo");
    }

    [TheoryConSqlServer]
    [InlineData("")]
    [InlineData("    ")]
    public async Task NombreYTelefono_Vacios_DebenRechazarse(string textoVacio)
    {
        Proveedor proveedorSinNombre = DatosDePruebaProveedores.CrearProveedor(textoVacio);
        await ComprobarQueLaBaseRechazaAsync(proveedorSinNombre, "CK_Proveedores_NombreNoVacio");

        Proveedor proveedorSinTelefono = DatosDePruebaProveedores.CrearProveedor();
        proveedorSinTelefono.Telefono = textoVacio;
        await ComprobarQueLaBaseRechazaAsync(proveedorSinTelefono, "CK_Proveedores_TelefonoNoVacio");
    }

    [FactConSqlServer]
    public async Task Cuenta_SinCompletar_DebeRechazarse()
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.CuentasBancarias.Clear();
        proveedor.CuentasBancarias.Add(new CuentaBancaria());

        using EopDbContext contexto = CrearContexto();
        contexto.Proveedores.Add(proveedor);

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        // SQL Server informa una sola restricción por vez, alcanza con ver que es de la cuenta.
        Assert.Contains("CK_CuentasBancarias_", excepcion.GetBaseException().Message);
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(12)]
    public async Task Cuenta_ConBancoFueraDeLaLista_DebeRechazarse(int numeroBanco)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.CuentasBancarias[0].Banco = (Banco)numeroBanco;

        await ComprobarQueLaBaseRechazaAsync(proveedor, "CK_CuentasBancarias_BancoValido");
    }

    // Las longitudes máximas solo se pueden comprobar acá, porque SQLite no las controla.
    [FactConSqlServer]
    public async Task Nombre_MasLargoQueElMaximo_DebeRechazarse()
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor(new string('a', 201));

        using EopDbContext contexto = CrearContexto();
        contexto.Proveedores.Add(proveedor);

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [FactConSqlServer]
    public async Task Nombre_ConLaLongitudMaxima_DebeGuardarse()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor(new string('a', 200)));

        using EopDbContext contexto = CrearContexto();
        Proveedor proveedor = await contexto.Proveedores.SingleAsync();

        Assert.Equal(200, proveedor.Nombre.Length);
        Assert.Equal(200, proveedor.NombreNormalizado.Length);
    }
}
