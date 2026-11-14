
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion
{
    public class FiltrarPresupuestosTests
    {
        // Creamos una base SQLite en memoria con tres presupuestos.
        // Utilizamos distintos clientes, estados, fechas y responsables
        // para comprobar que los filtros funcionan correctamente.
        private static async Task<(
            SqliteConnection Conexion,
            EopDbContext Contexto,
            int ClienteAlternativoId,
            int ResponsableAlternativoId)> CrearBaseAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            // Registramos dos clientes.
            var clientePrincipal = new Cliente
            {
                Nombre = "Empresa Solar",
                Rut = "215521750017",
                CorreoContacto = "solar@ejemplo.com",
                NumeroWhatsApp = "+59899123456",
                Activo = false
            };

            var clienteAlternativo = new Cliente
            {
                Nombre = "Empresa Industrial",
                Rut = "215521750025",
                CorreoContacto = "industrial@ejemplo.com",
                NumeroWhatsApp = "+59899234567",
                Activo = true
            };

            // Registramos dos usuarios responsables.
            var responsablePrincipal = new Usuario
            {
                NombreCompleto = "Ana Pereira",
                CorreoCorporativo = "ana@eop.com.uy",
                Rol = RolUsuario.Operador,
                Activo = false
            };

            var responsableAlternativo = new Usuario
            {
                NombreCompleto = "Luis Silva",
                CorreoCorporativo = "luis@eop.com.uy",
                Rol = RolUsuario.Operador,
                Activo = true
            };

            contexto.Clientes.AddRange(
                clientePrincipal,
                clienteAlternativo);

            contexto.Usuarios.AddRange(
                responsablePrincipal,
                responsableAlternativo);

            await contexto.SaveChangesAsync();

            // Buscamos un tipo de proyecto existente.
            int tipoProyectoId = await contexto.TiposProyecto
                .Where(tipo => tipo.Nombre == "Obra")
                .Select(tipo => tipo.Id)
                .SingleAsync();

            // Primer presupuesto: Borrador, Empresa Solar, Ana.
            var presupuestoA = new Presupuesto
            {
                Codigo = "PRES-20261101-OBRA-A-0001",
                NumeroCorrelativo = 1,
                NombreProyecto = "Obra A",
                ClienteId = clientePrincipal.Id,
                ResponsableId = responsablePrincipal.Id,
                TipoProyectoId = tipoProyectoId,
                PlazoVigenciaDias = 30,
                FechaCreacion = new DateTime(2026, 11, 1),
                Estado = EstadoPresupuesto.Borrador
            };

            // Segundo presupuesto: Aprobado, Empresa Solar, Ana.
            var presupuestoB = new Presupuesto
            {
                Codigo = "PRES-20261103-OBRA-B-0002",
                NumeroCorrelativo = 2,
                NombreProyecto = "Obra B",
                ClienteId = clientePrincipal.Id,
                ResponsableId = responsablePrincipal.Id,
                TipoProyectoId = tipoProyectoId,
                PlazoVigenciaDias = 30,
                FechaCreacion = new DateTime(2026, 11, 3),
                Estado = EstadoPresupuesto.Aprobado
            };

            // Tercer presupuesto: Borrador, Empresa Industrial, Luis.
            // Le asignamos una hora para verificar que el filtro
            // por fecha compare el día completo.
            var presupuestoC = new Presupuesto
            {
                Codigo = "PRES-20261104-OBRA-C-0003",
                NumeroCorrelativo = 3,
                NombreProyecto = "Obra C",
                ClienteId = clienteAlternativo.Id,
                ResponsableId = responsableAlternativo.Id,
                TipoProyectoId = tipoProyectoId,
                PlazoVigenciaDias = 30,
                FechaCreacion = new DateTime(2026, 11, 4, 14, 30, 0),
                Estado = EstadoPresupuesto.Borrador
            };

            contexto.Presupuestos.AddRange(
                presupuestoA,
                presupuestoB,
                presupuestoC);

            await contexto.SaveChangesAsync();

            return (
                conexion,
                contexto,
                clienteAlternativo.Id,
                responsableAlternativo.Id);
        }

        [Fact]
        public async Task SinFiltros_DevuelveTodosLosPresupuestos()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            var resultado = await consulta.EjecutarAsync();

            // Sin filtros deben aparecer los tres presupuestos.
            // El más reciente debe aparecer primero.
            Assert.Equal(3, resultado.Count);
            Assert.Equal("Obra C", resultado[0].NombreProyecto);
            Assert.Equal("Obra B", resultado[1].NombreProyecto);
            Assert.Equal("Obra A", resultado[2].NombreProyecto);
        }

        [Fact]
        public async Task FiltrarPorCodigo_DevuelvePresupuestoCorrecto()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            // Buscamos una parte del código del presupuesto B.
            var resultado = await consulta.EjecutarAsync(
                codigo: "OBRA-B");

            var presupuesto = Assert.Single(resultado);

            Assert.Equal("Obra B", presupuesto.NombreProyecto);
        }

        [Fact]
        public async Task FiltrarPorEstado_DevuelveSoloAprobados()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            var resultado = await consulta.EjecutarAsync(
                estado: EstadoPresupuesto.Aprobado);

            // Solamente la Obra B está aprobada.
            var presupuesto = Assert.Single(resultado);

            Assert.Equal("Obra B", presupuesto.NombreProyecto);
            Assert.Equal(EstadoPresupuesto.Aprobado, presupuesto.Estado);
        }

        [Fact]
        public async Task FiltrarPorCliente_DevuelveSoloSusPresupuestos()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            var resultado = await consulta.EjecutarAsync(
                clienteId: datos.ClienteAlternativoId);

            // Solo la Obra C pertenece a Empresa Industrial.
            var presupuesto = Assert.Single(resultado);

            Assert.Equal("Obra C", presupuesto.NombreProyecto);
            Assert.Equal("Empresa Industrial", presupuesto.NombreCliente);
        }

        [Fact]
        public async Task FiltrarPorFecha_ConsideraTodoElDia()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            // Buscamos el día 4 de noviembre sin indicar una hora.
            var resultado = await consulta.EjecutarAsync(
                fecha: new DateTime(2026, 11, 4));

            // Debe encontrar la Obra C, creada a las 14:30.
            var presupuesto = Assert.Single(resultado);

            Assert.Equal("Obra C", presupuesto.NombreProyecto);
        }

        // FIX-UTC-FECHAS: el filtro interpreta el día elegido en hora de Uruguay (UTC-3),
        // con inicio inclusivo (00:00 locales = 03:00 UTC) y fin exclusivo (00:00 del día siguiente).
        [Theory]
        [InlineData(2026, 11, 14, 2, 59, false, false)] // 23:59 del 13 en Uruguay: es del día 13, no aparece en ninguno
        [InlineData(2026, 11, 14, 3, 0, true, false)]   // 00:00 del 14: primer instante del día 14
        [InlineData(2026, 11, 15, 1, 0, true, false)]   // 22:00 del 14: es del día 14 aunque en UTC ya sea el 15
        [InlineData(2026, 11, 15, 2, 59, true, false)]  // 23:59 del 14: último instante del día 14
        [InlineData(2026, 11, 15, 3, 0, false, true)]   // 00:00 del 15: ya es el día siguiente (fin exclusivo)
        public async Task FiltrarPorFecha_UsaLosLimitesDelDiaEnUruguay(int anio, int mes, int dia, int hora, int minuto, bool apareceEl14, bool apareceEl15)
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            // Dejamos solo el presupuesto de este caso, con su fecha guardada en UTC.
            contexto.Presupuestos.RemoveRange(contexto.Presupuestos);
            await contexto.SaveChangesAsync();

            int tipoProyectoId = await contexto.TiposProyecto.Select(tipo => tipo.Id).FirstAsync();
            int clienteId = await contexto.Clientes.Select(cliente => cliente.Id).FirstAsync();
            int responsableId = await contexto.Usuarios.Select(usuario => usuario.Id).FirstAsync();

            contexto.Presupuestos.Add(new Presupuesto
            {
                Codigo = "PRES-LIMITE-0001",
                NumeroCorrelativo = 1,
                NombreProyecto = "Obra límite",
                ClienteId = clienteId,
                ResponsableId = responsableId,
                TipoProyectoId = tipoProyectoId,
                PlazoVigenciaDias = 30,
                FechaCreacion = new DateTime(anio, mes, dia, hora, minuto, 0, DateTimeKind.Utc),
                Estado = EstadoPresupuesto.Borrador
            });
            await contexto.SaveChangesAsync();

            var consulta = new ConsultarPresupuestos(contexto);

            var del14 = await consulta.EjecutarAsync(fecha: new DateTime(2026, 11, 14));
            var del15 = await consulta.EjecutarAsync(fecha: new DateTime(2026, 11, 15));

            Assert.Equal(apareceEl14, del14.Count == 1);
            Assert.Equal(apareceEl15, del15.Count == 1);
        }

        [Fact]
        public async Task FiltrarPorFecha_ElListadoDevuelveLaFechaEnUtcSinConvertir()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            var resultado = await consulta.EjecutarAsync(fecha: new DateTime(2026, 11, 4));

            // La conversión a hora de Uruguay se hace una sola vez, al mostrar la fecha en la vista.
            Assert.Equal(new DateTime(2026, 11, 4, 14, 30, 0), Assert.Single(resultado).FechaCreacion);
        }

        [Fact]
        public async Task FiltrarPorResponsable_DevuelveSoloSusPresupuestos()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            var resultado = await consulta.EjecutarAsync(
                responsableId: datos.ResponsableAlternativoId);

            // Solo la Obra C tiene como responsable a Luis.
            var presupuesto = Assert.Single(resultado);

            Assert.Equal("Obra C", presupuesto.NombreProyecto);
            Assert.Equal("Luis Silva", presupuesto.NombreResponsable);
        }

        [Fact]
        public async Task CombinarCincoFiltros_DevuelvePresupuestoCorrecto()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            // Combinamos los cinco criterios disponibles.
            var resultado = await consulta.EjecutarAsync(
                codigo: "OBRA-C",
                estado: EstadoPresupuesto.Borrador,
                clienteId: datos.ClienteAlternativoId,
                fecha: new DateTime(2026, 11, 4),
                responsableId: datos.ResponsableAlternativoId);

            // Solamente la Obra C cumple todas las condiciones.
            var presupuesto = Assert.Single(resultado);

            Assert.Equal("Obra C", presupuesto.NombreProyecto);
        }

        [Fact]
        public async Task FiltrosSinCoincidencias_DevuelveListaVacia()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarPresupuestos(contexto);

            // La Obra B está aprobada, pero no pertenece
            // al cliente alternativo.
            var resultado = await consulta.EjecutarAsync(
                codigo: "OBRA-B",
                clienteId: datos.ClienteAlternativoId);

            Assert.Empty(resultado);
        }
    }
}
