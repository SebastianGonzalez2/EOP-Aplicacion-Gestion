using EOP.Gestion.Core.CasosDeUso.Proveedores;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;
using EOP.Gestion.Infrastructure.Telefonia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// Las consultas de duplicados y la validación completa, pero contra una base de verdad.
// Cómo compara textos SQL Server se prueba aparte, en RepositorioProveedoresSqlServerTests.
public class RepositorioProveedoresTests : IDisposable
{
    private readonly SqliteConnection _conexion;

    public RepositorioProveedoresTests()
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

    private static ValidarDatosProveedor CrearValidador(EopDbContext contexto)
    {
        return new ValidarDatosProveedor(new RepositorioProveedores(contexto), new ServicioTelefonos());
    }

    [Fact]
    public async Task ExistePorNombreNormalizadoAsync_DebeEncontrarElNombreRegistrado()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("  Construcciones   Pérez "));

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);

        Assert.True(await repositorio.ExistePorNombreNormalizadoAsync("CONSTRUCCIONES PEREZ"));
    }

    [Fact]
    public async Task ExistePorNombreNormalizadoAsync_DebeDevolverFalseSiElNombreNoExiste()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa"));

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);

        Assert.False(await repositorio.ExistePorNombreNormalizadoAsync("OTRO PROVEEDOR"));
    }

    [Fact]
    public async Task ExistePorRutAsync_DebeEncontrarElRutRegistrado()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor(rut: "215521750017"));

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);

        Assert.True(await repositorio.ExistePorRutAsync("215521750017"));
    }

    [Fact]
    public async Task ExistePorRutAsync_DebeDevolverFalseSiElRutNoExiste()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor(rut: "215521750017"));

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);

        Assert.False(await repositorio.ExistePorRutAsync("210000010014"));
    }

    [Fact]
    public async Task ValidarAsync_ConNombreYRutYaGuardados_DebeInformarAmbosDuplicados()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Construcciones Pérez", "215521750017"));

        using EopDbContext contexto = CrearContexto();
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor(" CONSTRUCCIONES  PEREZ ", "21.552.175-0017");

        List<string> errores = await CrearValidador(contexto).ValidarAsync(proveedorNuevo, "UY", "099 123 456");

        Assert.Equal(
            ["Ya existe un proveedor registrado con ese nombre.", "Ya existe un proveedor registrado con ese RUT."],
            errores);
    }

    [Fact]
    public async Task ValidarAsync_ConProveedorNuevo_NoDebeDevolverErrores()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa", "215521750017"));

        using EopDbContext contexto = CrearContexto();
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor("Otro proveedor", "21.000.001-0014");

        List<string> errores = await CrearValidador(contexto).ValidarAsync(proveedorNuevo, "UY", "099 123 456");

        Assert.Empty(errores);
    }

    // US-009: rechazar un duplicado no puede tocar lo que ya estaba guardado. Validar tampoco
    // puede guardar nada: un proveedor correcto sigue sin existir hasta que S2-T13 lo registre.
    [Theory]
    [InlineData("Proveedor existente", "215521750017")]
    [InlineData("Proveedor nuevo", "210000010014")]
    public async Task ValidarAsync_NoDebeModificarNiAgregarRegistros(string nombre, string rut)
    {
        Proveedor proveedorExistente = DatosDePruebaProveedores.CrearProveedor("Proveedor existente", "215521750017");
        proveedorExistente.CorreoFacturacion = "facturas@ejemplo.com";
        proveedorExistente.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));
        await GuardarAsync(proveedorExistente);

        using (EopDbContext contexto = CrearContexto())
        {
            Proveedor proveedorAValidar = DatosDePruebaProveedores.CrearProveedor(nombre, rut);

            await CrearValidador(contexto).ValidarAsync(proveedorAValidar, "UY", "098 765 432");

            // ChangeTracker es donde EF anota lo que guardaría en el próximo SaveChanges.
            Assert.False(contexto.ChangeTracker.HasChanges());
            Assert.Empty(contexto.ChangeTracker.Entries());
        }

        using EopDbContext contextoDeLectura = CrearContexto();
        Proveedor proveedorGuardado = await contextoDeLectura.Proveedores
            .Include(p => p.CuentasBancarias)
            .SingleAsync();

        Assert.Equal("Proveedor existente", proveedorGuardado.Nombre);
        Assert.Equal("PROVEEDOR EXISTENTE", proveedorGuardado.NombreNormalizado);
        Assert.Equal("215521750017", proveedorGuardado.Rut);
        Assert.Equal("+59899123456", proveedorGuardado.Telefono);
        Assert.Equal("contacto@ejemplo.com", proveedorGuardado.CorreoContacto);
        Assert.Equal("facturas@ejemplo.com", proveedorGuardado.CorreoFacturacion);
        Assert.Equal(2, proveedorGuardado.CuentasBancarias.Count);
        Assert.Equal(2, await contextoDeLectura.CuentasBancarias.CountAsync());
    }

    // ---------- Guardado (S2-T13) ----------

    // Leemos con un contexto nuevo para que lo que comprobamos venga de la base y no de la memoria.
    [Fact]
    public async Task AgregarAsync_DebeGuardarElProveedorConTodasSusCuentasRelacionadas()
    {
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor("Fivisa S.A.", "215521750017");
        proveedorNuevo.CorreoFacturacion = "facturas@ejemplo.com";
        proveedorNuevo.CuentasBancarias.Add(new CuentaBancaria
        {
            Banco = Banco.BancoSantander,
            NumeroCuenta = "000222",
            Titular = "Otro titular",
            Moneda = Moneda.USD
        });

        ResultadoAgregarProveedor resultado;

        using (EopDbContext contexto = CrearContexto())
        {
            resultado = await new RepositorioProveedores(contexto).AgregarAsync(proveedorNuevo);
        }

        Assert.Equal(ResultadoAgregarProveedor.Agregado, resultado);

        using EopDbContext contextoDeLectura = CrearContexto();
        Proveedor guardado = await contextoDeLectura.Proveedores
            .Include(p => p.CuentasBancarias)
            .SingleAsync();

        Assert.Equal("Fivisa S.A.", guardado.Nombre);
        Assert.Equal("FIVISA S.A.", guardado.NombreNormalizado);
        Assert.Equal("215521750017", guardado.Rut);
        Assert.Equal("+59899123456", guardado.Telefono);
        Assert.Equal("contacto@ejemplo.com", guardado.CorreoContacto);
        Assert.Equal("facturas@ejemplo.com", guardado.CorreoFacturacion);

        Assert.Equal(2, guardado.CuentasBancarias.Count);
        Assert.Equal(2, await contextoDeLectura.CuentasBancarias.CountAsync());
        Assert.All(guardado.CuentasBancarias, cuenta => Assert.Equal(guardado.Id, cuenta.ProveedorId));

        CuentaBancaria segundaCuenta = guardado.CuentasBancarias.Single(cuenta => cuenta.NumeroCuenta == "000222");
        Assert.Equal(Banco.BancoSantander, segundaCuenta.Banco);
        Assert.Equal("Otro titular", segundaCuenta.Titular);
        Assert.Equal(Moneda.USD, segundaCuenta.Moneda);
    }

    // Reconocer un duplicado de SQL Server no puede volver el repositorio ciego a lo demás: en SQLite
    // el error tiene otra forma, y lo correcto es que siga como excepción en vez de esconderlo.
    [Fact]
    public async Task AgregarAsync_ConUnErrorQueNoReconoce_DebeDejarPasarLaExcepcion()
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.CuentasBancarias[0].Titular = "   ";

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);

        await Assert.ThrowsAsync<DbUpdateException>(() => repositorio.AgregarAsync(proveedor));

        using EopDbContext contextoDeLectura = CrearContexto();
        Assert.Equal(0, await contextoDeLectura.Proveedores.CountAsync());
        Assert.Equal(0, await contextoDeLectura.CuentasBancarias.CountAsync());
    }
}
