
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Consultas.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion
{
    public class ConsultarUsuariosTests
    {
        [Fact]
        public async Task ConsultarUsuarios_DevuelveActivosEInactivos()
        {
            // Creamos una base SQLite temporal en memoria.
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            // Registramos un usuario activo y otro inactivo.
            contexto.Usuarios.AddRange(
                new Usuario
                {
                    NombreCompleto = "Ana Pérez",
                    CorreoCorporativo = "ana@eop.com.uy",
                    Rol = RolUsuario.Administrador,
                    Activo = true
                },
                new Usuario
                {
                    NombreCompleto = "Juan González",
                    CorreoCorporativo = "juan@eop.com.uy",
                    Rol = RolUsuario.Operador,
                    Activo = false
                }
            );

            await contexto.SaveChangesAsync();

            // Consultamos todos los usuarios.
            var consulta = new ConsultarUsuarios(contexto);
            var resultado = await consulta.EjecutarAsync();

            // Deben aparecer ambos usuarios, sin filtrar por estado.
            Assert.Equal(2, resultado.Count);

            Assert.Contains(resultado, usuario =>
                usuario.NombreCompleto == "Ana Pérez" && usuario.Activo);

            Assert.Contains(resultado, usuario =>
                usuario.NombreCompleto == "Juan González" && !usuario.Activo);
        }

        [Fact]
        public async Task ConsultarUsuarios_DevuelveLosDatosYOrdenCorrectos()
        {
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            contexto.Usuarios.AddRange(
                new Usuario
                {
                    NombreCompleto = "Zoe Rodríguez",
                    CorreoCorporativo = "zoe@eop.com.uy",
                    Rol = RolUsuario.Supervisor,
                    Activo = true
                },
                new Usuario
                {
                    NombreCompleto = "Ana Pérez",
                    CorreoCorporativo = "ana@eop.com.uy",
                    Rol = RolUsuario.Administrador,
                    Activo = true
                }
            );

            await contexto.SaveChangesAsync();

            // Limpiamos los objetos seguidos por EF Core
            // para comprobar que la consulta no los vuelve a rastrear.
            contexto.ChangeTracker.Clear();

            var consulta = new ConsultarUsuarios(contexto);
            var resultado = await consulta.EjecutarAsync();

            Assert.Equal(2, resultado.Count);

            // Verificamos que estén ordenados por nombre.
            Assert.Equal("Ana Pérez", resultado[0].NombreCompleto);
            Assert.Equal("Zoe Rodríguez", resultado[1].NombreCompleto);

            // Comprobamos los datos que necesita la vista.
            Assert.True(resultado[0].Id > 0);
            Assert.Equal("ana@eop.com.uy", resultado[0].CorreoCorporativo);
            Assert.Equal(RolUsuario.Administrador, resultado[0].Rol);
            Assert.True(resultado[0].Activo);

            // La consulta no debe dejar entidades rastreadas.
            Assert.Empty(contexto.ChangeTracker.Entries());
        }

        [Fact]
        public async Task ConsultarUsuarios_SinRegistros_DevuelveListaVacia()
        {
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            var consulta = new ConsultarUsuarios(contexto);

            var resultado = await consulta.EjecutarAsync();

            Assert.Empty(resultado);
        }
    }
}
