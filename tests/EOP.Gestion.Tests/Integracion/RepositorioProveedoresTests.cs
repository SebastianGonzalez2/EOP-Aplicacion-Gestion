using EOP.Gestion.Core.CasosDeUso.Proveedores;
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
}
