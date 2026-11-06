using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Clientes;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S1-T27: el índice único IX_Clientes_Rut como respuesta definitiva cuando dos altas con el
// mismo RUT llegan juntas. Necesita SQL Server porque el repositorio reconoce el error por su
// número (2601) y el nombre del índice, que son propios de SQL Server.
public class RepositorioClientesSqlServerTests : IAsyncLifetime
{
    private const string Rut = "215521750017";

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

    private EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(_cadenaConexion).Options);

    private static Cliente CrearCliente(string nombre = "Cliente de prueba", string rut = Rut) => new()
    {
        Nombre = nombre,
        Rut = rut,
        CorreoContacto = "cliente@ejemplo.com",
        NumeroWhatsApp = "+59899123456",
        Activo = true
    };

    private async Task<int> ContarClientesAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.Clientes.CountAsync();
    }

    [FactConSqlServer]
    public async Task AgregarSiRutDisponibleAsync_ConRutLibre_GuardaYDevuelveTrue()
    {
        using EopDbContext contexto = CrearContexto();

        bool guardado = await new RepositorioClientes(contexto).AgregarSiRutDisponibleAsync(CrearCliente());

        Assert.True(guardado);
        Assert.Equal(1, await ContarClientesAsync());
    }

    // Simula la carrera sin hilos: el primer cliente entra por otro contexto, como si fuera otra
    // solicitud que guardó después de que esta ya había consultado ExistePorRutAsync.
    [FactConSqlServer]
    public async Task AgregarSiRutDisponibleAsync_ConRutYaGuardado_DevuelveFalseSinLanzarNiPisarAlOtro()
    {
        using (EopDbContext otraSolicitud = CrearContexto())
        {
            Assert.True(await new RepositorioClientes(otraSolicitud).AgregarSiRutDisponibleAsync(CrearCliente("Primero")));
        }

        using EopDbContext contexto = CrearContexto();
        bool guardado = await new RepositorioClientes(contexto).AgregarSiRutDisponibleAsync(CrearCliente("Segundo"));

        Assert.False(guardado);

        using EopDbContext lectura = CrearContexto();
        Cliente unico = await lectura.Clientes.SingleAsync();
        Assert.Equal("Primero", unico.Nombre);
    }

    // Después del rechazo, el mismo contexto (que dura todo el pedido) tiene que seguir sirviendo
    // y no reintentar el cliente rechazado en el próximo guardado.
    [FactConSqlServer]
    public async Task AgregarSiRutDisponibleAsync_DespuesDeUnRechazo_ElMismoContextoSigueSirviendo()
    {
        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioClientes(contexto);
        await repositorio.AgregarSiRutDisponibleAsync(CrearCliente("Primero"));

        Assert.False(await repositorio.AgregarSiRutDisponibleAsync(CrearCliente("Repetido")));
        Assert.True(await repositorio.AgregarSiRutDisponibleAsync(CrearCliente("Otro", "215521750025")));

        Assert.Equal(2, await ContarClientesAsync());
    }

    // Una restricción CHECK violada no es un RUT repetido: no se tiene que disimular como tal.
    [FactConSqlServer]
    public async Task AgregarSiRutDisponibleAsync_ConOtroErrorDeLaBase_NoLoDisimulaComoDuplicado()
    {
        using EopDbContext contexto = CrearContexto();
        Cliente sinCorreos = CrearCliente();
        sinCorreos.CorreoContacto = null;

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => new RepositorioClientes(contexto).AgregarSiRutDisponibleAsync(sinCorreos));

        Assert.Contains("CK_Clientes_AlMenosUnCorreo", excepcion.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task MuchasAltasSimultaneasConElMismoRut_SoloSeGuardaUna_YLasDemasDevuelvenFalse()
    {
        const int cantidadDeAltas = 10;

        bool[] resultados = await Task.WhenAll(Enumerable.Range(1, cantidadDeAltas).Select(async numero =>
        {
            using EopDbContext contexto = CrearContexto();
            return await new RepositorioClientes(contexto).AgregarSiRutDisponibleAsync(CrearCliente($"Alta {numero}"));
        }));

        Assert.Equal(1, resultados.Count(guardado => guardado));
        Assert.Equal(1, await ContarClientesAsync());
    }
}
