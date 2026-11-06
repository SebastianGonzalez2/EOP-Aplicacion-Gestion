using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EOP.Gestion.Tests.Integracion;

// S1-T27: Materiales.Nombre pasa de nvarchar(max) a nvarchar(200). La migración tiene que sacar
// y volver a crear CK_Materiales_NombreNoVacio, porque SQL Server no deja cambiar una columna
// que usa un CHECK; acá se comprueba que la restricción sigue funcionando en los dos sentidos.
public class NombreMaterialSqlServerTests : IAsyncLifetime
{
    private const string MigracionAnterior = "S1_T27_RestriccionesClientesYUsuarios";

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

    private async Task GuardarAsync(string nombre)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Materiales.Add(new Material { Nombre = nombre, Unidad = UnidadMedida.Metro });
        await contexto.SaveChangesAsync();
    }

    [FactConSqlServer]
    public async Task NombreDe200Caracteres_EntraEnLaColumna()
    {
        await GuardarAsync(new string('a', 200));
    }

    [FactConSqlServer]
    public async Task NombreDe201Caracteres_LoRechazaLaBase()
    {
        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => GuardarAsync(new string('a', 201)));

        Assert.Contains("truncated", excepcion.GetBaseException().Message);
        Assert.Contains("'Nombre'", excepcion.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task NombreEnBlanco_LoSigueRechazandoElCheckRecreado()
    {
        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => GuardarAsync("   "));

        Assert.Contains("CK_Materiales_NombreNoVacio", excepcion.GetBaseException().Message);
    }

    [FactConSqlServer]
    public async Task Migracion_AlRevertirla_ConservaLosMaterialesYElCheck()
    {
        await GuardarAsync("Caño de cobre");

        using EopDbContext contexto = CrearContexto();
        await contexto.GetService<IMigrator>().MigrateAsync(MigracionAnterior);

        Assert.Equal(1, await contexto.Materiales.CountAsync());
        Assert.Contains("CK_Materiales_NombreNoVacio", await contexto.Database
            .SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.check_constraints")
            .ToListAsync());

        // Y vuelve a aplicarse sin problemas.
        await contexto.Database.MigrateAsync();
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());
    }
}
