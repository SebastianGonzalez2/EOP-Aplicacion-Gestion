using System.Reflection;
using EOP.Gestion.Core.Interfaces.Almacenamiento;
using EOP.Gestion.Infrastructure;
using EOP.Gestion.Infrastructure.Almacenamiento;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Unitarias;

/// <summary>
/// Registro del servicio de almacenamiento en la inyección de dependencias y lectura de su configuración.
/// No se conectan a Azure: crear el cliente no hace ninguna llamada de red.
/// </summary>
public class ConfiguracionAlmacenamientoTests
{
    private const string CadenaDePrueba =
        "Server=localhost;Database=EopPruebas;Trusted_Connection=True;TrustServerCertificate=True";

    private static IConfiguration CrearConfiguracion(Dictionary<string, string?>? almacenamiento = null)
    {
        var valores = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{InyeccionDeDependencias.NombreCadenaConexion}"] = CadenaDePrueba
        };
        foreach (var (clave, valor) in almacenamiento ?? [])
        {
            valores[$"{AlmacenamientoBlobOpciones.NombreSeccion}:{clave}"] = valor;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
    }

    private static Dictionary<string, string?> ConfiguracionValida(string usarIdentidadAdministrada = "false") => new()
    {
        ["NombreCuenta"] = "eopstorage245",
        ["NombreContenedor"] = "documentos-eop",
        ["UsarIdentidadAdministrada"] = usarIdentidadAdministrada
    };

    private static ServiceProvider CrearProveedor(IConfiguration configuracion)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AgregarInfraestructura(configuracion);
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("false")]
    [InlineData("true")]
    public void AgregarInfraestructura_ResuelveElServicioDeAlmacenamientoComoSingleton(string usarIdentidadAdministrada)
    {
        using var proveedor = CrearProveedor(CrearConfiguracion(ConfiguracionValida(usarIdentidadAdministrada)));

        var primero = proveedor.GetRequiredService<IServicioAlmacenamiento>();
        var segundo = proveedor.GetRequiredService<IServicioAlmacenamiento>();

        Assert.IsType<ServicioAlmacenamientoBlob>(primero);
        Assert.Same(primero, segundo);
    }

    [Fact]
    public void AgregarInfraestructura_SinConfiguracionDeAlmacenamiento_NoImpideArrancarPeroFallaAlPedirElServicio()
    {
        // Registrar no falla (RNF-13: la app arranca aunque el almacenamiento no esté configurado)...
        using var proveedor = CrearProveedor(CrearConfiguracion());

        // ...pero al pedir el servicio se explica qué falta.
        var excepcion = Assert.Throws<InvalidOperationException>(
            () => proveedor.GetRequiredService<IServicioAlmacenamiento>());

        Assert.Contains("AlmacenamientoBlob:NombreCuenta", excepcion.Message);
    }

    [Fact]
    public void Crear_ApuntaAlContenedorPrivadoPorHttpsSinCredencialesEnLaDireccion()
    {
        var opciones = AlmacenamientoBlobOpciones.Leer(CrearConfiguracion(ConfiguracionValida("true")));

        var contenedor = FabricaContenedorBlob.Crear(opciones);

        Assert.Equal(new Uri("https://eopstorage245.blob.core.windows.net/documentos-eop"), contenedor.Uri);
        Assert.Equal(string.Empty, contenedor.Uri.Query);
        Assert.Equal(string.Empty, contenedor.Uri.UserInfo);
    }

    [Fact]
    public void Leer_SinIndicarUsarIdentidadAdministrada_UsaLaIdentidadAdministradaPorDefecto()
    {
        var valores = ConfiguracionValida();
        valores.Remove("UsarIdentidadAdministrada");

        var opciones = AlmacenamientoBlobOpciones.Leer(CrearConfiguracion(valores));

        Assert.True(opciones.UsarIdentidadAdministrada);
    }

    [Fact]
    public void Leer_ConTenantEIdentidadAsignada_ConservaEsosValores()
    {
        var valores = ConfiguracionValida("true");
        valores["TenantId"] = "tenant-de-prueba";
        valores["ClientIdIdentidadAdministrada"] = "client-de-prueba";

        var opciones = AlmacenamientoBlobOpciones.Leer(CrearConfiguracion(valores));

        Assert.Equal("tenant-de-prueba", opciones.TenantId);
        Assert.Equal("client-de-prueba", opciones.ClientIdIdentidadAdministrada);
    }

    [Theory]
    [InlineData("NombreCuenta", null)]
    [InlineData("NombreCuenta", "")]
    [InlineData("NombreCuenta", "ab")]
    [InlineData("NombreCuenta", "Cuenta-Con-Guion")]
    [InlineData("NombreCuenta", "otra.cuenta.com/x")]
    [InlineData("NombreCuenta", "cuenta@servidor-malo")]
    [InlineData("NombreContenedor", null)]
    [InlineData("NombreContenedor", "ab")]
    [InlineData("NombreContenedor", "Documentos")]
    [InlineData("NombreContenedor", "doc/../otro")]
    [InlineData("NombreContenedor", "-documentos")]
    [InlineData("NombreContenedor", "doc--umentos")]
    [InlineData("UsarIdentidadAdministrada", "quizas")]
    public void Leer_ConfiguracionInvalida_LanzaInvalidOperationException(string clave, string? valor)
    {
        var valores = ConfiguracionValida();
        valores[clave] = valor;

        var excepcion = Assert.Throws<InvalidOperationException>(
            () => AlmacenamientoBlobOpciones.Leer(CrearConfiguracion(valores)));

        Assert.Contains(clave, excepcion.Message);
    }

    [Fact]
    public void Opciones_NoTienenCamposParaClavesNiCadenasDeConexion()
    {
        // RNF-2 / RNF-11: la configuración del almacenamiento no puede ni siquiera expresar un secreto.
        string[] palabrasProhibidas = ["clave", "key", "secret", "secreto", "connection", "conexion", "sas", "token", "password"];

        var propiedades = typeof(AlmacenamientoBlobOpciones)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant());

        Assert.DoesNotContain(propiedades, nombre => palabrasProhibidas.Any(nombre.Contains));
    }

    [Fact]
    public void Contrato_NoExponeUrlsNiTiposDeAzure()
    {
        // RNF-5: nada de URLs públicas. El contrato solo habla de referencias (texto) y de archivos.
        var tiposUsados = typeof(IServicioAlmacenamiento)
            .GetMethods()
            .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType))
            .SelectMany(ExpandirTiposGenericos)
            .ToList();

        Assert.DoesNotContain(tiposUsados, tipo => tipo == typeof(Uri));
        Assert.DoesNotContain(tiposUsados, tipo => tipo.Namespace != null && tipo.Namespace.StartsWith("Azure"));
    }

    private static IEnumerable<Type> ExpandirTiposGenericos(Type tipo) =>
        tipo.IsGenericType ? tipo.GetGenericArguments().SelectMany(ExpandirTiposGenericos).Append(tipo) : [tipo];
}
