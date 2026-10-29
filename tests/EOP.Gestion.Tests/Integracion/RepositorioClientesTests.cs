using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Clientes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

public class RepositorioClientesTests
{
    [Fact]
    public async Task ExistePorRutAsync_DebeDevolverTrueSiElRutExiste()
    {
        // Creamos una base de datos temporal en memoria.
        using var conexion = new SqliteConnection("Data Source=:memory:");
        conexion.Open();

        // Configuramos EopDbContext para usar SQLite en esta prueba.
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(conexion)
            .Options;

        using var contexto = new EopDbContext(opciones);

        // Creamos las tablas en la base de datos temporal.
        contexto.Database.EnsureCreated();

        // Registramos un cliente de prueba.
        var cliente = new Cliente
        {
            Nombre = "Cliente de prueba",
            Rut = "215521750017",
            NumeroWhatsApp = "+59899123456",
            CorreoContacto = "cliente@ejemplo.com"
        };

        contexto.Clientes.Add(cliente);
        await contexto.SaveChangesAsync();

        // Creamos el repositorio usando nuestro contexto de prueba.
        var repositorio = new RepositorioClientes(contexto);

        // Consultamos si el RUT registrado existe.
        bool resultado = await repositorio.ExistePorRutAsync("215521750017");

        // Comprobamos que el repositorio lo haya encontrado.
        Assert.True(resultado);
    }


    [Fact]
    public async Task ExistePorRutAsync_DebeDevolverFalseSiElRutNoExiste()
    {
        // Creamos una base de datos temporal en memoria.
        using var conexion = new SqliteConnection("Data Source=:memory:");
        conexion.Open();

        // Configuramos el contexto para utilizar SQLite.
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(conexion)
            .Options;

        using var contexto = new EopDbContext(opciones);
        contexto.Database.EnsureCreated();

        // Creamos el repositorio.
        var repositorio = new RepositorioClientes(contexto);

        // Buscamos un RUT que no está registrado.
        bool resultado = await repositorio.ExistePorRutAsync("215521750017");

        // Comprobamos que el repositorio indique que no existe.
        Assert.False(resultado);
    }










}