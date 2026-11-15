
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion
{
    public class ConsultarPresupuestoDetalleTests
    {
        [Fact]
        public async Task EjecutarAsync_PresupuestoExistente_DevuelveDatosCorrectos()
        {
            // Creamos una base de datos temporal SQLite.
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            // Registramos un cliente y un responsable inactivos
            // para comprobar que también se pueden consultar
            // presupuestos históricos.
            var cliente = new Cliente
            {
                Nombre = "Empresa Solar",
                Rut = "215521750017",
                CorreoContacto = "cliente@ejemplo.com",
                NumeroWhatsApp = "+59899123456",
                Activo = false
            };

            var responsable = new Usuario
            {
                NombreCompleto = "Ana Pereira",
                CorreoCorporativo = "ana@eop.com.uy",
                Rol = RolUsuario.Operador,
                Activo = false
            };

            contexto.Clientes.Add(cliente);
            contexto.Usuarios.Add(responsable);

            await contexto.SaveChangesAsync();

            // Utilizamos un tipo de proyecto existente.
            var tipoProyecto = await contexto.TiposProyecto
                .SingleAsync(tipo => tipo.Nombre == "Obra");

            // Creamos el presupuesto que vamos a consultar.
            var presupuesto = new Presupuesto
            {
                Codigo = "PRES-20261101-OBRA-SOLAR-0001",
                NumeroCorrelativo = 1,
                NombreProyecto = "Obra Solar",
                ClienteId = cliente.Id,
                TipoProyectoId = tipoProyecto.Id,
                ResponsableId = responsable.Id,
                FechaCreacion = new DateTime(2026, 11, 1),
                Estado = EstadoPresupuesto.Borrador,
                PlazoVigenciaDias = 30
            };

            contexto.Presupuestos.Add(presupuesto);
            await contexto.SaveChangesAsync();

            // Quitamos el seguimiento de las entidades
            // para comprobar que la consulta es de solo lectura.
            contexto.ChangeTracker.Clear();

            var consulta = new ConsultarPresupuestoDetalle(contexto);

            var resultado = await consulta.EjecutarAsync(presupuesto.Id);

            // Comprobamos que encontramos el presupuesto.
            Assert.NotNull(resultado);

            // Verificamos todos los datos del detalle.
            Assert.Equal(presupuesto.Id, resultado.Id);
            Assert.Equal(presupuesto.Codigo, resultado.Codigo);
            Assert.Equal("Obra Solar", resultado.NombreProyecto);
            Assert.Equal("Empresa Solar", resultado.NombreCliente);
            Assert.Equal("Obra", resultado.NombreTipoProyecto);
            Assert.Equal("Ana Pereira", resultado.NombreResponsable);
            Assert.Equal(EstadoPresupuesto.Borrador, resultado.Estado);
            Assert.Equal(30, resultado.PlazoVigenciaDias);
            Assert.Equal(new DateTime(2026, 11, 1), resultado.FechaCreacion);

            // La consulta no debe dejar entidades rastreadas.
            Assert.Empty(contexto.ChangeTracker.Entries<Presupuesto>());
        }

        [Fact]
        public async Task EjecutarAsync_PresupuestoInexistente_DevuelveNull()
        {
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            var consulta = new ConsultarPresupuestoDetalle(contexto);

            // Buscamos un presupuesto que no existe.
            var resultado = await consulta.EjecutarAsync(999999);

            // No debería encontrar ningún presupuesto.
            Assert.Null(resultado);
        }
    }
}
