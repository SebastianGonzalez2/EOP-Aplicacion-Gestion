using EOP.Gestion.Core.DTOs.Clientes;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static EOP.Gestion.Tests.Compartido.DatosDePruebaClientes;

namespace EOP.Gestion.Tests.Integracion;

// S1-T32: completa ConsultarClientesTests con el listado sin filtros, los criterios vacíos
// y los resultados sin coincidencias.
public class ConsultarClientesListadoTests
{
    [Fact]
    public async Task SinCriterios_DevuelveActivosEInactivosOrdenadosPorNombreYRut()
    {
        const string rutSegundaAnaPereira = "215521750041";
        Cliente homonima = new()
        {
            Nombre = AnaPereira,
            Rut = rutSegundaAnaPereira,
            CorreoContacto = "otra.ana@ejemplo.com",
            NumeroWhatsApp = "+59899456789"
        };
        using var baseDePrueba = new BaseEnMemoria([homonima, .. CrearClientes()]);

        List<ClienteListadoDto> resultado = await baseDePrueba.ConsultarAsync(null, null);

        Assert.Equal(
            [RutAnaGarcia, RutAnaPereira, rutSegundaAnaPereira, RutBrunoSilva],
            resultado.Select(cliente => cliente.Rut));
    }

    [Fact]
    public async Task Resultado_ConservaLosDatosDelClienteYDejaNuloElCorreoAusente()
    {
        using var baseDePrueba = new BaseEnMemoria(CrearClientes());
        int idAnaGarcia = await baseDePrueba.Contexto.Clientes
            .Where(cliente => cliente.Rut == RutAnaGarcia)
            .Select(cliente => cliente.Id)
            .SingleAsync();

        List<ClienteListadoDto> resultado = await baseDePrueba.ConsultarAsync(RutAnaGarcia, null);

        Assert.Equivalent(
            new ClienteListadoDto
            {
                Id = idAnaGarcia,
                Nombre = AnaGarcia,
                Rut = RutAnaGarcia,
                CorreoContacto = null,
                CorreoFacturacion = "facturas@garcia.com",
                NumeroWhatsApp = "+59899234567",
                Activo = false
            },
            Assert.Single(resultado),
            strict: true);
    }

    [Theory]
    [InlineData("", null, AnaGarcia, AnaPereira, BrunoSilva)]
    [InlineData("   ", null, AnaGarcia, AnaPereira, BrunoSilva)]
    [InlineData("", true, AnaPereira, BrunoSilva)]
    [InlineData("   ", false, AnaGarcia)]
    public async Task BusquedaEnBlanco_NoRestringeElResultado(string busqueda, bool? activo, params string[] esperados)
    {
        using var baseDePrueba = new BaseEnMemoria(CrearClientes());

        List<ClienteListadoDto> resultado = await baseDePrueba.ConsultarAsync(busqueda, activo);

        Assert.Equal(esperados, resultado.Select(cliente => cliente.Nombre));
    }

    [Theory]
    [InlineData("  Bruno  ", BrunoSilva)]
    [InlineData(" " + RutAnaGarcia + " ", AnaGarcia)]
    public async Task Busqueda_IgnoraLosEspaciosDeLosExtremos(string busqueda, string esperado)
    {
        using var baseDePrueba = new BaseEnMemoria(CrearClientes());

        List<ClienteListadoDto> resultado = await baseDePrueba.ConsultarAsync(busqueda, null);

        Assert.Equal(esperado, Assert.Single(resultado).Nombre);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Ana", null)]
    [InlineData(RutAnaPereira, null)]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData("Ana", true)]
    public async Task SinClientesRegistrados_DevuelveListaVacia(string? busqueda, bool? activo)
    {
        using var baseDePrueba = new BaseEnMemoria();

        Assert.Empty(await baseDePrueba.ConsultarAsync(busqueda, activo));
    }

    [Theory]
    [InlineData("Inexistente", null)]
    [InlineData("999999999999", null)]
    [InlineData("%", null)]
    [InlineData("_", null)]
    [InlineData("Inexistente", true)]
    [InlineData("Pereira", false)]
    [InlineData("Garcia", true)]
    [InlineData(RutBrunoSilva, false)]
    [InlineData(RutAnaGarcia, true)]
    public async Task CriteriosSinCoincidencias_DevuelvenListaVacia(string busqueda, bool? activo)
    {
        using var baseDePrueba = new BaseEnMemoria(CrearClientes());

        Assert.Empty(await baseDePrueba.ConsultarAsync(busqueda, activo));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EstadoSinClientes_DevuelveListaVacia(bool estadoRegistrado)
    {
        using var baseDePrueba = new BaseEnMemoria(new Cliente
        {
            Nombre = AnaPereira,
            Rut = RutAnaPereira,
            CorreoContacto = "ana@ejemplo.com",
            NumeroWhatsApp = "+59899123456",
            Activo = estadoRegistrado
        });

        Assert.Empty(await baseDePrueba.ConsultarAsync(null, !estadoRegistrado));
    }

    private sealed class BaseEnMemoria : IDisposable
    {
        private readonly SqliteConnection _conexion = new("Data Source=:memory:");

        public EopDbContext Contexto { get; }

        public BaseEnMemoria(params Cliente[] clientes)
        {
            _conexion.Open();
            Contexto = new EopDbContext(new DbContextOptionsBuilder<EopDbContext>().UseSqlite(_conexion).Options);
            Contexto.Database.EnsureCreated();
            Contexto.Clientes.AddRange(clientes);
            Contexto.SaveChanges();
        }

        public Task<List<ClienteListadoDto>> ConsultarAsync(string? busqueda, bool? activo) =>
            new ConsultarClientes(Contexto).EjecutarAsync(busqueda, activo);

        public void Dispose()
        {
            Contexto.Dispose();
            _conexion.Dispose();
        }
    }
}
