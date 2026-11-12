
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;
using EOP.Gestion.WebApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion
{
    public class ConsultarPresupuestosTests
    {
        // Creamos una base SQLite temporal para cada prueba.
        // Así no necesitamos utilizar SQL Server real.
        private static async Task<(SqliteConnection Conexion, EopDbContext Contexto)> CrearBaseAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            return (conexion, contexto);
        }

        // Preparamos dos presupuestos con fechas diferentes.
        private static async Task AgregarDatosDePruebaAsync(EopDbContext contexto)
        {
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

            // Utilizamos un tipo de proyecto que ya existe.
            int tipoProyectoId = await contexto.TiposProyecto
                .Where(tipo => tipo.Nombre == "Obra")
                .Select(tipo => tipo.Id)
                .SingleAsync();

            var presupuestoAnterior = new Presupuesto
            {
                Codigo = "PRES-20261101-OBRA-A-0001",
                NumeroCorrelativo = 1,
                NombreProyecto = "Obra A",
                ClienteId = cliente.Id,
                ResponsableId = responsable.Id,
                TipoProyectoId = tipoProyectoId,
                PlazoVigenciaDias = 30,
                FechaCreacion = new DateTime(2026, 11, 1),
                Estado = EstadoPresupuesto.Borrador
            };

            var presupuestoReciente = new Presupuesto
            {
                Codigo = "PRES-20261103-OBRA-B-0002",
                NumeroCorrelativo = 2,
                NombreProyecto = "Obra B",
                ClienteId = cliente.Id,
                ResponsableId = responsable.Id,
                TipoProyectoId = tipoProyectoId,
                PlazoVigenciaDias = 30,
                FechaCreacion = new DateTime(2026, 11, 3),
                Estado = EstadoPresupuesto.Aprobado
            };

            contexto.Presupuestos.AddRange(
                presupuestoAnterior,
                presupuestoReciente);

            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task EjecutarAsync_DevuelvePresupuestosConDatosCorrectos()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            await AgregarDatosDePruebaAsync(contexto);

            // Dejamos de seguir las entidades que guardamos.
            // Así comprobamos que la consulta no las vuelve a rastrear.
            contexto.ChangeTracker.Clear();

            var consultarPresupuestos = new ConsultarPresupuestos(contexto);

            var resultado = await consultarPresupuestos.EjecutarAsync();

            // Deben aparecer los dos presupuestos.
            Assert.Equal(2, resultado.Count);

            // El más reciente aparece primero.
            Assert.Equal("Obra B", resultado[0].NombreProyecto);
            Assert.Equal("Obra A", resultado[1].NombreProyecto);

            // Verificamos los datos que necesita el listado.
            var presupuesto = resultado[0];

            Assert.Equal("PRES-20261103-OBRA-B-0002", presupuesto.Codigo);
            Assert.Equal("Empresa Solar", presupuesto.NombreCliente);
            Assert.Equal("Ana Pereira", presupuesto.NombreResponsable);
            Assert.Equal(new DateTime(2026, 11, 3), presupuesto.FechaCreacion);
            Assert.Equal(EstadoPresupuesto.Aprobado, presupuesto.Estado);

            // Los presupuestos históricos deben seguir apareciendo
            // aunque su cliente o responsable estén inactivos.
            Assert.Empty(contexto.ChangeTracker.Entries<Presupuesto>());
        }

        [Fact]
        public async Task EjecutarAsync_SinPresupuestos_DevuelveListaVacia()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consultarPresupuestos = new ConsultarPresupuestos(contexto);

            var resultado = await consultarPresupuestos.EjecutarAsync();

            Assert.Empty(resultado);
        }

        [Fact]
        public async Task Index_DevuelveVistaConListado()
        {
            var datos = await CrearBaseAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            await AgregarDatosDePruebaAsync(contexto);

            var registrarPresupuesto = new RegistrarPresupuesto(
                new RepositorioPresupuestos(contexto),
                new ConsultaReferenciasPresupuesto(contexto),
                new RepositorioUsuarios(contexto));

            var controller = new PresupuestosController(
                new ConsultarClientes(contexto),
                new ConsultarTiposProyectoParaSeleccion(contexto),
                registrarPresupuesto,
                new ConsultarPresupuestos(contexto))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Ejecutamos el método que muestra el listado.
            IActionResult resultado = await controller.Index();

            // Verificamos que devuelve una vista.
            var vista = Assert.IsType<ViewResult>(resultado);

            // La vista debe recibir los presupuestos consultados.
            var modelo = Assert.IsType<List<PresupuestoListadoDto>>(
                vista.Model);

            Assert.Equal(2, modelo.Count);
            Assert.Equal("Obra B", modelo[0].NombreProyecto);
            Assert.Equal("Obra A", modelo[1].NombreProyecto);
        }
    }
}
