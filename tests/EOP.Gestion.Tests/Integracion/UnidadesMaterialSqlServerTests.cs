using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EOP.Gestion.Tests.Integracion;

// La lista de unidades también está cuidada en la base, con la restricción CK_Materiales_UnidadValida.
// Acá comprobamos en un SQL Server real, con las tablas creadas por las migraciones, que la lista
// nueva funciona y que los materiales que ya estaban guardados no cambian.
public class UnidadesMaterialSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    public Task InitializeAsync()
    {
        // Cada prueba migra hasta donde necesita, así que acá no hay nada que preparar.
        return Task.CompletedTask;
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

    [FactConSqlServer]
    public async Task Base_DebeAceptarTodasLasUnidadesDelEnum()
    {
        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();

        UnidadMedida[] unidades = Enum.GetValues<UnidadMedida>();

        foreach (UnidadMedida unidad in unidades)
        {
            contexto.Materiales.Add(new Material { Nombre = $"Material {unidad}", Unidad = unidad });
        }

        await contexto.SaveChangesAsync();

        Assert.Equal(unidades.Length, await contexto.Materiales.CountAsync());
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(99)]
    public async Task Base_DebeRechazarUnNumeroFueraDeLaLista(int numeroInvalido)
    {
        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();

        contexto.Materiales.Add(new Material { Nombre = "Material inválido", Unidad = (UnidadMedida)numeroInvalido });

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Contains("CK_Materiales_UnidadValida", excepcion.GetBaseException().Message);
    }

    // Simulamos una base que ya tiene materiales guardados con las cinco unidades de antes
    // y le aplicamos la migración. Tienen que seguir siendo los mismos materiales con la misma unidad.
    [FactConSqlServer]
    public async Task Migracion_DebeConservarLosMaterialesYaGuardadosYPermitirLasUnidadesNuevas()
    {
        using EopDbContext contexto = CrearContexto();

        IMigrator migrador = contexto.GetService<IMigrator>();
        await migrador.MigrateAsync("S2_T14_CrearTablaMateriales");

        await contexto.Database.ExecuteSqlRawAsync(
            "INSERT INTO Materiales (Nombre, Unidad) VALUES ('A', 1), ('B', 2), ('C', 3), ('D', 4), ('E', 5)");

        // Antes de la migración, la base todavía rechaza la unidad nueva.
        contexto.Materiales.Add(new Material { Nombre = "Tablero", Unidad = UnidadMedida.Unidad });
        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        contexto.ChangeTracker.Clear();

        await contexto.Database.MigrateAsync();

        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());

        Assert.Equal(UnidadMedida.Metro, await ObtenerUnidadAsync(contexto, "A"));
        Assert.Equal(UnidadMedida.MetroCuadrado, await ObtenerUnidadAsync(contexto, "B"));
        Assert.Equal(UnidadMedida.MetroCubico, await ObtenerUnidadAsync(contexto, "C"));
        Assert.Equal(UnidadMedida.Kilogramo, await ObtenerUnidadAsync(contexto, "D"));
        Assert.Equal(UnidadMedida.Litro, await ObtenerUnidadAsync(contexto, "E"));

        // Después de la migración, la misma unidad ya se puede guardar.
        contexto.Materiales.Add(new Material { Nombre = "Tablero", Unidad = UnidadMedida.Unidad });
        await contexto.SaveChangesAsync();

        Assert.Equal(UnidadMedida.Unidad, await ObtenerUnidadAsync(contexto, "Tablero"));
    }

    private static async Task<UnidadMedida> ObtenerUnidadAsync(EopDbContext contexto, string nombre)
    {
        Material material = await contexto.Materiales.AsNoTracking().SingleAsync(m => m.Nombre == nombre);

        return material.Unidad;
    }
}
