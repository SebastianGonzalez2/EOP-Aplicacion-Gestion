using System.Reflection;

namespace EOP.Gestion.Tests.Unitarias;

/// <summary>
/// Protege la regla de dependencias entre capas:
/// Core no conoce a nadie; Infrastructure solo conoce a Core.
/// </summary>
public class ArquitecturaTests
{
    [Theory]
    [InlineData("EOP.Gestion.Core", "EOP.Gestion.Infrastructure")]
    [InlineData("EOP.Gestion.Core", "EOP.Gestion.WebApp")]
    [InlineData("EOP.Gestion.Infrastructure", "EOP.Gestion.WebApp")]
    public void Proyecto_NoDebeDependerDe(string proyecto, string dependenciaProhibida)
    {
        var referencias = Assembly.Load(proyecto)
            .GetReferencedAssemblies()
            .Select(a => a.Name);

        Assert.DoesNotContain(dependenciaProhibida, referencias);
    }

    [Fact]
    public void Core_NoDebeDependerDelSdkDeAzure()
    {
        var referencias = Assembly.Load("EOP.Gestion.Core")
            .GetReferencedAssemblies()
            .Select(a => a.Name);

        Assert.DoesNotContain(referencias, nombre => nombre != null && nombre.StartsWith("Azure"));
    }

    // S3-T18: la caché y el HttpClient del servicio de cotizaciones son detalles de Infrastructure.
    [Theory]
    [InlineData("Microsoft.Extensions.Caching")]
    [InlineData("Microsoft.Extensions.Http")]
    [InlineData("System.Net.Http")]
    public void Core_NoDebeDependerDeLaCacheNiDeHttp(string prefijoProhibido)
    {
        var referencias = Assembly.Load("EOP.Gestion.Core")
            .GetReferencedAssemblies()
            .Select(a => a.Name);

        Assert.DoesNotContain(referencias, nombre => nombre != null && nombre.StartsWith(prefijoProhibido));
    }
}
