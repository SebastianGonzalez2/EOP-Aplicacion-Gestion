using EOP.Gestion.Infrastructure;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Unitarias;

/// <summary>
/// Verifica el registro de Infrastructure sin conectarse a una base real.
/// </summary>
public class InfraestructuraTests
{
    private const string CadenaDePrueba =
        "Server=localhost;Database=EopPruebas;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void AgregarInfraestructura_ConCadena_RegistraEopDbContextConSqlServer()
    {
        var services = new ServiceCollection();
        services.AgregarInfraestructura(CrearConfiguracion(CadenaDePrueba));

        using var proveedor = services.BuildServiceProvider();
        using var alcance = proveedor.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        Assert.True(contexto.Database.IsSqlServer());
        Assert.Equal(CadenaDePrueba, contexto.Database.GetConnectionString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarInfraestructura_SinCadena_LanzaExcepcion(string? cadena)
    {
        var services = new ServiceCollection();

        var excepcion = Assert.Throws<InvalidOperationException>(
            () => services.AgregarInfraestructura(CrearConfiguracion(cadena)));

        Assert.Contains(InyeccionDeDependencias.NombreCadenaConexion, excepcion.Message);
    }

    private static IConfiguration CrearConfiguracion(string? cadena) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{InyeccionDeDependencias.NombreCadenaConexion}"] = cadena
            })
            .Build();
}
