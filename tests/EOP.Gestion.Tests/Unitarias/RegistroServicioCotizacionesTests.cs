using EOP.Gestion.Core.Interfaces.Cotizaciones;
using EOP.Gestion.Infrastructure;
using EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T18 — El servicio queda registrado como una sola instancia para toda la aplicación, con la
// caché en memoria compartida y el HttpClient configurado por IHttpClientFactory.
public sealed class RegistroServicioCotizacionesTests : IDisposable
{
    private readonly ServiceProvider _proveedorDeServicios;

    public RegistroServicioCotizacionesTests()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EopDb"] = "Server=no-se-usa;Database=no-se-usa"
            })
            .Build();

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        _proveedorDeServicios = servicios.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    public void Dispose() => _proveedorDeServicios.Dispose();

    [Fact]
    public void ElServicioEsUnSingletonImplementadoConDatosUruguay()
    {
        var primero = _proveedorDeServicios.GetRequiredService<IServicioCotizaciones>();
        var segundo = _proveedorDeServicios.GetRequiredService<IServicioCotizaciones>();

        Assert.IsType<ServicioCotizacionesDatosUruguay>(primero);
        Assert.Same(primero, segundo);
    }

    [Fact]
    public void SeRegistraLaCacheEnMemoriaYElReloj()
    {
        Assert.NotNull(_proveedorDeServicios.GetService<IMemoryCache>());
        Assert.Same(TimeProvider.System, _proveedorDeServicios.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void ElHttpClientApuntaALaApiConTiempoMaximoDeEspera()
    {
        var fabrica = _proveedorDeServicios.GetRequiredService<IHttpClientFactory>();

        using var cliente = fabrica.CreateClient(nameof(ClienteDatosUruguay));

        Assert.Equal(new Uri("https://datosuruguay.com/api/v1/"), cliente.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(10), cliente.Timeout);
        Assert.Equal(Uri.UriSchemeHttps, cliente.BaseAddress!.Scheme);
    }
}
