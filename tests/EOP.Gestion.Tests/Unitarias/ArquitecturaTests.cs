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
}
