using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

public class ConsultarClientesTests
{
    [Fact]
    public async Task BuscarPorNombre_DebeDevolverClienteCorrespondiente()
    {
        // Creamos una base de datos temporal SQLite en memoria.
        using var conexion = new SqliteConnection("Data Source=:memory:");
        conexion.Open();

        // Configuramos Entity Framework para utilizar esa base.
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(conexion)
            .Options;

        using var contexto = new EopDbContext(opciones);
        contexto.Database.EnsureCreated();

        // Registramos dos clientes para realizar la prueba.
        contexto.Clientes.AddRange(
            new Cliente
            {
                Nombre = "Ana Pereira",
                Rut = "215521750017",
                CorreoContacto = "ana@ejemplo.com",
                NumeroWhatsApp = "+59899123456"
            },
            new Cliente
            {
                Nombre = "Juan Gonzalez",
                Rut = "215521750025",
                CorreoContacto = "juan@ejemplo.com",
                NumeroWhatsApp = "+59899234567"
            }
        );

        await contexto.SaveChangesAsync();

        // Creamos el servicio que consulta los clientes.
        var consulta = new ConsultarClientes(contexto);

        // Buscamos los clientes cuyo nombre contiene "Ana".
        var resultado = await consulta.EjecutarAsync("Ana", null);

        // Verificamos que solamente se encontró un cliente.
        Assert.Single(resultado);

        // Comprobamos que el cliente encontrado es el correcto.
        Assert.Equal("Ana Pereira", resultado[0].Nombre);
    }


    [Fact]
    public async Task BuscarPorRut_DebeDevolverClienteCorrespondiente()
    {
        // Creamos una base de datos temporal SQLite en memoria.
        using var conexion = new SqliteConnection("Data Source=:memory:");
        conexion.Open();

        // Configuramos Entity Framework para utilizar esa base.
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(conexion)
            .Options;

        using var contexto = new EopDbContext(opciones);
        contexto.Database.EnsureCreated();

        // Registramos dos clientes con diferentes RUT.
        contexto.Clientes.AddRange(
            new Cliente
            {
                Nombre = "Ana Pereira",
                Rut = "215521750017",
                CorreoContacto = "ana@ejemplo.com",
                NumeroWhatsApp = "+59899123456"
            },
            new Cliente
            {
                Nombre = "Juan Gonzalez",
                Rut = "215521750025",
                CorreoContacto = "juan@ejemplo.com",
                NumeroWhatsApp = "+59899234567"
            }
        );

        await contexto.SaveChangesAsync();

        // Creamos el servicio que consulta los clientes.
        var consulta = new ConsultarClientes(contexto);

        // Buscamos un cliente por su RUT.
        var resultado = await consulta.EjecutarAsync("215521750017", null);

        // Verificamos que encontramos exactamente un cliente.
        Assert.Single(resultado);

        // Comprobamos que pertenece a Ana Pereira.
        Assert.Equal("Ana Pereira", resultado[0].Nombre);
    }


    [Theory]
    [InlineData(true, "Ana Pereira")]
    [InlineData(false, "Juan Gonzalez")]
    public async Task FiltrarPorEstado_DebeDevolverClientesDelEstadoSeleccionado(
    bool estado,
    string nombreEsperado)
    {
        // Creamos una base de datos temporal SQLite en memoria.
        using var conexion = new SqliteConnection("Data Source=:memory:");
        conexion.Open();

        // Configuramos Entity Framework para usar SQLite.
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(conexion)
            .Options;

        using var contexto = new EopDbContext(opciones);
        contexto.Database.EnsureCreated();

        // Registramos un cliente activo y otro inactivo.
        contexto.Clientes.AddRange(
            new Cliente
            {
                Nombre = "Ana Pereira",
                Rut = "215521750017",
                CorreoContacto = "ana@ejemplo.com",
                NumeroWhatsApp = "+59899123456",
                Activo = true
            },
            new Cliente
            {
                Nombre = "Juan Gonzalez",
                Rut = "215521750025",
                CorreoContacto = "juan@ejemplo.com",
                NumeroWhatsApp = "+59899234567",
                Activo = false
            }
        );

        await contexto.SaveChangesAsync();

        // Creamos el servicio de consulta de clientes.
        var consulta = new ConsultarClientes(contexto);

        // Aplicamos únicamente el filtro por estado.
        var resultado = await consulta.EjecutarAsync(null, estado);

        // Comprobamos que encontramos exactamente un cliente.
        Assert.Single(resultado);

        // Verificamos que corresponde al estado seleccionado.
        Assert.Equal(nombreEsperado, resultado[0].Nombre);
        Assert.Equal(estado, resultado[0].Activo);
    }

    [Theory]
    [InlineData("Ana", true, "Ana Pereira")]
    [InlineData("215521750025", false, "Ana Garcia")]
    public async Task BuscarYFiltrarPorEstado_DebeCombinarAmbosCriterios(
    string busqueda,
    bool estado,
    string nombreEsperado)
    {
        // Creamos una base de datos temporal SQLite en memoria.
        using var conexion = new SqliteConnection("Data Source=:memory:");
        conexion.Open();

        // Configuramos Entity Framework para utilizar SQLite.
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(conexion)
            .Options;

        using var contexto = new EopDbContext(opciones);
        contexto.Database.EnsureCreated();

        // Registramos clientes con diferentes nombres y estados.
        contexto.Clientes.AddRange(
            new Cliente
            {
                Nombre = "Ana Pereira",
                Rut = "215521750017",
                CorreoContacto = "ana@ejemplo.com",
                NumeroWhatsApp = "+59899123456",
                Activo = true
            },
            new Cliente
            {
                Nombre = "Ana Garcia",
                Rut = "215521750025",
                CorreoContacto = "garcia@ejemplo.com",
                NumeroWhatsApp = "+59899234567",
                Activo = false
            },
            new Cliente
            {
                Nombre = "Juan Gonzalez",
                Rut = "215521750033",
                CorreoContacto = "juan@ejemplo.com",
                NumeroWhatsApp = "+59899345678",
                Activo = true
            },
            new Cliente
            {
                Nombre = "Pedro Silva",
                Rut = "215521750041",
                CorreoContacto = "pedro@ejemplo.com",
                NumeroWhatsApp = "+59899456789",
                Activo = false
            }
        );

        await contexto.SaveChangesAsync();

        // Creamos el servicio de consulta de clientes.
        var consulta = new ConsultarClientes(contexto);

        // Aplicamos la búsqueda y el filtro de estado juntos.
        var resultado = await consulta.EjecutarAsync(busqueda, estado);

        // Comprobamos que encontramos exactamente un cliente.
        Assert.Single(resultado);

        // Verificamos que el cliente encontrado sea el esperado.
        Assert.Equal(nombreEsperado, resultado[0].Nombre);

        // Comprobamos que también coincida su estado.
        Assert.Equal(estado, resultado[0].Activo);
    }


}