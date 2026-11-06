using System.Text.RegularExpressions;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace EOP.Gestion.Tests.Integracion;

// S1-T27: las demás pruebas crean la base con MigrateAsync, que manda cada orden por separado.
// En Azure las migraciones se suelen aplicar como script SQL, donde varias órdenes comparten un
// lote y SQL Server revisa el lote entero antes de ejecutarlo. Una migración puede andar de una
// forma y fallar de la otra (le pasó a S2_T16), así que acá se prueban los scripts.
public class ScriptsDeMigracionSqlServerTests : IAsyncLifetime
{
    private const string MigracionAnteriorAMateriales = "RF_068_AmpliarUnidadesMedida";
    private const string MigracionMateriales = "S2_T16_RegistrarMaterialProveedor";

    private static readonly Regex SeparadorDeLotes = new(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private readonly string _cadenaPorScript = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();
    private readonly string _cadenaPorMigrate = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        // Base vacía, sin tablas: el script tiene que crear todo.
        using EopDbContext contexto = CrearContexto(_cadenaPorScript);
        await contexto.GetService<IRelationalDatabaseCreator>().CreateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        foreach (string cadena in new[] { _cadenaPorScript, _cadenaPorMigrate })
        {
            using EopDbContext contexto = CrearContexto(cadena);
            await contexto.Database.EnsureDeletedAsync();
        }
    }

    private static EopDbContext CrearContexto(string cadena) =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(cadena).Options);

    private static string GenerarScript(
        MigrationsSqlGenerationOptions opciones,
        string? desdeMigracion = null,
        string? hastaMigracion = null)
    {
        using EopDbContext contexto = CrearContexto("Server=.;Database=SoloParaGenerar");
        return contexto.GetService<IMigrator>().GenerateScript(desdeMigracion, hastaMigracion, opciones);
    }

    // Igual que sqlcmd o SSMS: cada "GO" separa un lote que se manda por separado.
    private static async Task EjecutarScriptAsync(string cadena, string script)
    {
        await using var conexion = new SqlConnection(cadena);
        await conexion.OpenAsync();

        foreach (string lote in SeparadorDeLotes.Split(script).Where(lote => !string.IsNullOrWhiteSpace(lote)))
        {
            await using var comando = new SqlCommand(lote, conexion);
            await comando.ExecuteNonQueryAsync();
        }
    }

    private static async Task<List<string>> ConsultarAsync(string cadena, string sql)
    {
        await using var conexion = new SqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion);
        await using SqlDataReader lector = await comando.ExecuteReaderAsync();

        var filas = new List<string>();
        while (await lector.ReadAsync())
        {
            filas.Add(lector.GetString(0));
        }

        return filas;
    }

    // Una "foto" del esquema: columnas con su tipo, índices, restricciones CHECK y claves foráneas.
    // Las vistas sys.* mezclan intercalaciones, por eso cada texto se pasa a la de la base.
    private static async Task<List<string>> DescribirEsquemaAsync(string cadena)
    {
        const string sql = """
            SELECT CONCAT('COL ', t.name COLLATE DATABASE_DEFAULT, '.', c.name COLLATE DATABASE_DEFAULT, ' ', ty.name COLLATE DATABASE_DEFAULT,
                          '(', c.max_length, ',', c.precision, ',', c.scale, ') null=', c.is_nullable)
            FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            UNION ALL
            SELECT CONCAT('IDX ', t.name COLLATE DATABASE_DEFAULT, '.', i.name COLLATE DATABASE_DEFAULT,
                          ' unique=', i.is_unique, ' filtro=', i.filter_definition COLLATE DATABASE_DEFAULT)
            FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id WHERE i.name IS NOT NULL
            UNION ALL
            SELECT CONCAT('CK ', OBJECT_NAME(parent_object_id) COLLATE DATABASE_DEFAULT, '.', name COLLATE DATABASE_DEFAULT,
                          ' ', definition COLLATE DATABASE_DEFAULT)
            FROM sys.check_constraints
            UNION ALL
            SELECT CONCAT('FK ', name COLLATE DATABASE_DEFAULT, ' borrar=', delete_referential_action_desc COLLATE DATABASE_DEFAULT)
            FROM sys.foreign_keys
            """;

        List<string> esquema = await ConsultarAsync(cadena, sql);
        esquema.Sort(StringComparer.Ordinal);
        return esquema;
    }

    private static Task<List<string>> ObtenerMigracionesAplicadasAsync(string cadena) =>
        ConsultarAsync(cadena, "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId");

    [FactConSqlServer]
    public async Task ScriptCompleto_SeAplicaEnUnaBaseVacia_YDejaElMismoEsquemaQueMigrate()
    {
        await EjecutarScriptAsync(_cadenaPorScript, GenerarScript(MigrationsSqlGenerationOptions.Default));

        using (EopDbContext porMigrate = CrearContexto(_cadenaPorMigrate))
        {
            await porMigrate.Database.MigrateAsync();
        }

        Assert.Equal(await DescribirEsquemaAsync(_cadenaPorMigrate), await DescribirEsquemaAsync(_cadenaPorScript));
        Assert.Equal(await ObtenerMigracionesAplicadasAsync(_cadenaPorMigrate), await ObtenerMigracionesAplicadasAsync(_cadenaPorScript));
    }

    // El idempotente es el que se usa para desplegar: se puede correr sobre una base en
    // cualquier estado, incluso una que ya está al día, y solo aplica lo que falta.
    [FactConSqlServer]
    public async Task ScriptIdempotente_SePuedeAplicarDosVecesSeguidas()
    {
        string script = GenerarScript(MigrationsSqlGenerationOptions.Idempotent);

        await EjecutarScriptAsync(_cadenaPorScript, script);
        List<string> esquemaDespuesDeLaPrimera = await DescribirEsquemaAsync(_cadenaPorScript);

        await EjecutarScriptAsync(_cadenaPorScript, script);

        Assert.Equal(esquemaDespuesDeLaPrimera, await DescribirEsquemaAsync(_cadenaPorScript));

        using EopDbContext contexto = CrearContexto(_cadenaPorScript);
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());
    }

    // El caso que motivó el arreglo: una base que ya tenía materiales recibe S2_T16 por script.
    // El UPDATE dentro del EXEC tiene que completar NombreNormalizado igual que antes.
    [FactConSqlServer]
    public async Task ScriptDeMateriales_SobreMaterialesExistentes_CompletaElNombreNormalizado()
    {
        using (EopDbContext contexto = CrearContexto(_cadenaPorScript))
        {
            await contexto.GetService<IMigrator>().MigrateAsync(MigracionAnteriorAMateriales);
        }

        await EjecutarScriptAsync(_cadenaPorScript, """
            INSERT INTO Materiales (Nombre, Unidad) VALUES (N'  Caño   de  cobre ', 1);
            INSERT INTO Materiales (Nombre, Unidad) VALUES (CONCAT(N'Cable', CHAR(9), N'unipolar'), 1);
            """);

        string script = GenerarScript(MigrationsSqlGenerationOptions.Default, MigracionAnteriorAMateriales, MigracionMateriales);
        await EjecutarScriptAsync(_cadenaPorScript, script);

        List<string> normalizados = await ConsultarAsync(_cadenaPorScript, "SELECT NombreNormalizado FROM Materiales ORDER BY Id");
        Assert.Equal(["CAÑO DE COBRE", "CABLE UNIPOLAR"], normalizados);

        List<string> restricciones = await ConsultarAsync(_cadenaPorScript,
            "SELECT name FROM sys.check_constraints WHERE name = 'CK_Materiales_NombreNoVacio'");
        Assert.Single(restricciones);
    }
}
