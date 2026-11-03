using Microsoft.Data.SqlClient;

namespace EOP.Gestion.Tests.Compartido;

// Los bloqueos UPDLOCK y HOLDLOCK solo existen en SQL Server, así que para probarlos de
// verdad hace falta un SQL Server real. Por defecto usamos LocalDB, que viene con
// Visual Studio. Para usar otro servidor, se indica su cadena de conexión en la
// variable de entorno EOP_PRUEBAS_SQLSERVER.
//
// Cada prueba crea su propia base con un nombre al azar y la borra al terminar. Nunca
// se usa la base de desarrollo, aunque la cadena de conexión nombre alguna.
internal static class ServidorSqlDePrueba
{
    public const string MotivoParaOmitir =
        "No hay un SQL Server disponible: instalá LocalDB o configurá la variable de entorno EOP_PRUEBAS_SQLSERVER.";

    private static readonly Lazy<bool> ServidorDisponible = new(ComprobarSiElServidorResponde);

    public static bool EstaDisponible
    {
        get { return ServidorDisponible.Value; }
    }

    public static string CrearCadenaParaBaseNueva()
    {
        var cadena = new SqlConnectionStringBuilder(ObtenerCadenaDelServidor())
        {
            InitialCatalog = $"EOP_Pruebas_T14_{Guid.NewGuid():N}"
        };

        return cadena.ConnectionString;
    }

    private static string ObtenerCadenaDelServidor()
    {
        string? cadenaConfigurada = Environment.GetEnvironmentVariable("EOP_PRUEBAS_SQLSERVER");

        if (!string.IsNullOrWhiteSpace(cadenaConfigurada))
        {
            return cadenaConfigurada;
        }

        return @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";
    }

    private static bool ComprobarSiElServidorResponde()
    {
        // Elegimos atrapar cualquier excepción porque, sin LocalDB instalado, el error
        // cambia según el sistema operativo. En todos los casos la respuesta es la misma.
        try
        {
            using var conexion = new SqlConnection(ObtenerCadenaDelServidor());
            conexion.Open();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

// Es como [Fact], pero la prueba aparece como "omitida" si no hay SQL Server. Así, en una
// máquina sin SQL Server queda claro que no se ejecutó, en vez de aparecer como un fallo.
public sealed class FactConSqlServerAttribute : FactAttribute
{
    public FactConSqlServerAttribute()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            Skip = ServidorSqlDePrueba.MotivoParaOmitir;
        }
    }
}

public sealed class TheoryConSqlServerAttribute : TheoryAttribute
{
    public TheoryConSqlServerAttribute()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            Skip = ServidorSqlDePrueba.MotivoParaOmitir;
        }
    }
}
