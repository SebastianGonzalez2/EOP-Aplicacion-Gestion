using EOP.Gestion.Infrastructure;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Clientes;


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

        // Comprobamos que ASP.NET Core pueda obtener el repositorio de clientes.
        var repositorio = alcance.ServiceProvider.GetRequiredService<IRepositorioClientes>();

        // Verificamos que la implementación obtenida sea RepositorioClientes.
        Assert.IsType<RepositorioClientes>(repositorio);

        Assert.True(contexto.Database.IsSqlServer());

        // EF Core normaliza la cadena y le agrega datos propios (por ejemplo Application Name),
        // así que se comparan los valores relevantes y no el texto literal.
        var cadenaUsada = new SqlConnectionStringBuilder(contexto.Database.GetConnectionString());
        Assert.Equal("localhost", cadenaUsada.DataSource);
        Assert.Equal("EopPruebas", cadenaUsada.InitialCatalog);
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
