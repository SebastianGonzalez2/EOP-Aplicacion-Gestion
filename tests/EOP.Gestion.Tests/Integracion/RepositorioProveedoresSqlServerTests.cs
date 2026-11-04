using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// Quien compara los textos en la consulta de duplicados es SQL Server, no C#, y lo hace según la
// intercalación de la base. SQLite compara distinto, así que esto solo se puede demostrar acá.
public class RepositorioProveedoresSqlServerTests : IAsyncLifetime
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

    [TheoryConSqlServer]
    [InlineData("Construcciones Pérez")]
    [InlineData("CONSTRUCCIONES PEREZ")]
    [InlineData("  construcciones   perez ")]
    public async Task ExistePorNombreNormalizadoAsync_DebeEncontrarElNombreAunqueCambieLaEscritura(string nombreNuevo)
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Construcciones Perez"));

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);
        var proveedorNuevo = new Proveedor { Nombre = nombreNuevo };

        Assert.True(await repositorio.ExistePorNombreNormalizadoAsync(proveedorNuevo.NombreNormalizado));
    }

    // Si la base usara una intercalación que ignora estas marcas, la consulta diría que Pena ya
    // existe cuando solo está Peña, y estas pruebas avisarían de que la regla de RF-23 dejó de cumplirse.
    [TheoryConSqlServer]
    [InlineData("Peña", "Pena")]
    [InlineData("Pena", "Peña")]
    [InlineData("Güemes", "Guemes")]
    [InlineData("Guemes", "Güemes")]
    public async Task ExistePorNombreNormalizadoAsync_DebeDistinguirEnieYDieresis(string nombreGuardado, string nombreConsultado)
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor(nombreGuardado));

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);
        var proveedorNuevo = new Proveedor { Nombre = nombreConsultado };

        Assert.False(await repositorio.ExistePorNombreNormalizadoAsync(proveedorNuevo.NombreNormalizado));
    }

    [FactConSqlServer]
    public async Task ExistePorRutAsync_DebeEncontrarSoloElRutRegistrado()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor(rut: "215521750017"));

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);

        Assert.True(await repositorio.ExistePorRutAsync("215521750017"));
        Assert.False(await repositorio.ExistePorRutAsync("210000010014"));
    }
}
