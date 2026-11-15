
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.Infrastructure.Consultas.Usuarios;
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
    public class PresupuestosDetalleControllerTests
    {
        // Preparamos el Controller con una base temporal SQLite.
        private static PresupuestosController CrearController(EopDbContext contexto)
        {
            var registrarPresupuesto = new RegistrarPresupuesto(
                new RepositorioPresupuestos(contexto),
                new ConsultaReferenciasPresupuesto(contexto),
                new RepositorioUsuarios(contexto),
                TimeProvider.System);

            return new PresupuestosController(
                new ConsultarClientes(contexto),
                new ConsultarTiposProyectoParaSeleccion(contexto),
                registrarPresupuesto,
                new ConsultarPresupuestos(contexto),
                new ConsultarUsuarios(contexto),
                new ConsultarPresupuestoDetalle(contexto))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task Detalle_PresupuestoExistente_DevuelveVistaConDatos()
        {
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            // Registramos los datos necesarios para el presupuesto.
            var cliente = new Cliente
            {
                Nombre = "Empresa Solar",
                Rut = "215521750017",
                CorreoContacto = "cliente@ejemplo.com",
                NumeroWhatsApp = "+59899123456"
            };

            var responsable = new Usuario
            {
                NombreCompleto = "Ana Pereira",
                CorreoCorporativo = "ana@eop.com.uy",
                Rol = RolUsuario.Operador,
                Activo = true
            };

            contexto.Clientes.Add(cliente);
            contexto.Usuarios.Add(responsable);
            await contexto.SaveChangesAsync();

            var tipoProyecto = await contexto.TiposProyecto
                .SingleAsync(tipo => tipo.Nombre == "Obra");

            var presupuesto = new Presupuesto
            {
                Codigo = "PRES-20261101-OBRA-SOLAR-0001",
                NumeroCorrelativo = 1,
                NombreProyecto = "Obra Solar",
                ClienteId = cliente.Id,
                ResponsableId = responsable.Id,
                TipoProyectoId = tipoProyecto.Id,
                PlazoVigenciaDias = 30,
                FechaCreacion = new DateTime(2026, 11, 1),
                Estado = EstadoPresupuesto.Borrador
            };

            contexto.Presupuestos.Add(presupuesto);
            await contexto.SaveChangesAsync();

            var controller = CrearController(contexto);

            // Ejecutamos la acción Detalle con el ID del presupuesto.
            IActionResult resultado = await controller.Detalle(presupuesto.Id);

            // Comprobamos que devuelve una vista.
            var vista = Assert.IsType<ViewResult>(resultado);

            // Comprobamos que contiene el presupuesto correcto.
            var modelo = Assert.IsType<PresupuestoDetalleDto>(vista.Model);

            Assert.Equal(presupuesto.Id, modelo.Id);
            Assert.Equal("Obra Solar", modelo.NombreProyecto);
            Assert.Equal("Empresa Solar", modelo.NombreCliente);
            Assert.Equal("Ana Pereira", modelo.NombreResponsable);
        }

        [Fact]
        public async Task Detalle_PresupuestoInexistente_DevuelveNotFound()
        {
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            var controller = CrearController(contexto);

            // Intentamos abrir un presupuesto inexistente.
            IActionResult resultado = await controller.Detalle(999999);

            // El Controller debe responder con un error 404.
            Assert.IsType<NotFoundResult>(resultado);
        }
    }
}
