using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Infrastructure;

/// <summary>
/// Punto único de registro de los servicios de Infrastructure en el contenedor.
/// </summary>
public static class InyeccionDeDependencias
{
    public const string NombreCadenaConexion = "EopDb";

    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // La cadena nunca se versiona (RNF-2 / RNF-11): en desarrollo viene de
        // User Secrets y en Azure de la configuración del App Service.
        var cadenaConexion = configuration.GetConnectionString(NombreCadenaConexion);

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{NombreCadenaConexion}'. En desarrollo, configurala con: " +
                $"dotnet user-secrets set \"ConnectionStrings:{NombreCadenaConexion}\" \"<cadena>\" " +
                "--project src/EOP.Gestion.WebApp");
        }

        services.AddDbContext<EopDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));

        return services;
    }
}
