using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using EOP.Gestion.Core.Interfaces;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios;

using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Clientes;
using EOP.Gestion.Infrastructure.Telefonia;

namespace EOP.Gestion.Infrastructure;

using EOP.Gestion.Infrastructure.Consultas.Clientes;

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
        //AddDbContext<EopDbContext>: registra nuestro contexto para que la aplicación pueda utilizarlo.
        //UseSqlServer(cadenaConexion): indica que trabajaremos con SQL Server y proporciona los datos de conexión.

        // Registramos el repositorio de clientes para consultar sus datos.
        services.AddScoped<IRepositorioClientes, RepositorioClientes>();

        // Registramos el servicio encargado de consultar
        // y filtrar los clientes para el listado.
        services.AddScoped<ConsultarClientes>();



        // Singleton: arma la lista de países una sola vez y no guarda nada de cada pedido,
        // así que todos los pedidos pueden compartir la misma instancia sin pisarse.
        services.AddSingleton<IServicioTelefonos, ServicioTelefonos>();

        services.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        //Cuando alguna clase necesite un IRepositorioUsuarios, creá y proporcionale una instancia de RepositorioUsuarios.

        return services;
    }
}
