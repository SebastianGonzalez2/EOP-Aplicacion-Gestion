
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

public class ConsultarProveedoresTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly EopDbContext _contexto;

    public ConsultarProveedoresTests()
    {
        // Creamos una base de datos temporal para cada prueba.
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(_conexion)
            .Options;

        _contexto = new EopDbContext(opciones);
        _contexto.Database.EnsureCreated();
    }

    [Fact]
    public async Task SinProveedores_DevuelveListaVacia()
    {
        var consulta = new ConsultarProveedores(_contexto);

        List<ProveedorListadoDto> resultado =
            await consulta.EjecutarAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ConProveedores_DevuelveDatosOrdenadosPorNombre()
    {
        // Creamos proveedores completos usando el helper existente.
        var zeta = DatosDePruebaProveedores.CrearProveedor(
            "Zeta", "215521750017");

        var alfa = DatosDePruebaProveedores.CrearProveedor(
            "Alfa", "210000000019");

        alfa.CorreoContacto = null;
        alfa.CorreoFacturacion = "facturas@alfa.com";

        _contexto.Proveedores.AddRange(zeta, alfa);
        await _contexto.SaveChangesAsync();

        // Ejecutamos la consulta que estamos probando.
        var consulta = new ConsultarProveedores(_contexto);
        List<ProveedorListadoDto> resultado =
            await consulta.EjecutarAsync();

        // Comprobamos la cantidad y el orden.
        Assert.Equal(2, resultado.Count);
        Assert.Equal("Alfa", resultado[0].Nombre);
        Assert.Equal("Zeta", resultado[1].Nombre);

        // Verificamos que el DTO contenga los datos correctos.
        Assert.Equal(alfa.Id, resultado[0].Id);
        Assert.Equal(alfa.Rut, resultado[0].Rut);
        Assert.Equal(alfa.Telefono, resultado[0].Telefono);
        Assert.Null(resultado[0].CorreoContacto);
        Assert.Equal("facturas@alfa.com",
            resultado[0].CorreoFacturacion);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }
}
