using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using static EOP.Gestion.Tests.Compartido.DatosDePruebaClientes;

namespace EOP.Gestion.Tests.Integracion;

// EF Core traduce Contains de forma distinta en cada proveedor. Estas pruebas ejecutan la
// consulta real sobre SQL Server, en una base temporal creada con las migraciones del proyecto.
public class ConsultarClientesSqlServerTests : IAsyncLifetime
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
        contexto.Clientes.AddRange(CrearClientes());
        await contexto.SaveChangesAsync();
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

    [TheoryConSqlServer]
    [InlineData(null, null, AnaGarcia, AnaPereira, BrunoSilva)]
    [InlineData("Ana", null, AnaGarcia, AnaPereira)]
    [InlineData(RutBrunoSilva, null, BrunoSilva)]
    [InlineData(null, true, AnaPereira, BrunoSilva)]
    [InlineData(null, false, AnaGarcia)]
    [InlineData("Ana", true, AnaPereira)]
    [InlineData(RutAnaGarcia, false, AnaGarcia)]
    [InlineData("Pereira", false)]
    [InlineData("Inexistente", null)]
    [InlineData("%", null)]
    [InlineData("_", null)]
    public async Task EjecutarAsync_AplicaLosCriteriosEnSqlServer(string? busqueda, bool? activo, params string[] esperados)
    {
        using EopDbContext contexto = CrearContexto();

        var resultado = await new ConsultarClientes(contexto).EjecutarAsync(busqueda, activo);

        Assert.Equal(esperados, resultado.Select(cliente => cliente.Nombre));
    }

    private EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(_cadenaConexion).Options);
}
