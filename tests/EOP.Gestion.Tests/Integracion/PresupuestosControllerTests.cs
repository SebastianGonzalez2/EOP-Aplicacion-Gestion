
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;
using EOP.Gestion.WebApp.Controllers;
using EOP.Gestion.WebApp.ViewModels.Presupuestos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion
{
    public class PresupuestosControllerTests
    {
        [Fact]
        public async Task CrearGet_CargaClientesActivosYTiposProyecto()
        {
            // Creamos una base de datos temporal SQLite en memoria.
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            // Registramos un cliente activo y otro inactivo.
            var clienteActivo = new Cliente
            {
                Nombre = "Cliente activo",
                Rut = "215521750017",
                CorreoContacto = "activo@ejemplo.com",
                NumeroWhatsApp = "+59899123456",
                Activo = true
            };

            var clienteInactivo = new Cliente
            {
                Nombre = "Cliente inactivo",
                Rut = "215521750025",
                CorreoContacto = "inactivo@ejemplo.com",
                NumeroWhatsApp = "+59899234567",
                Activo = false
            };

            contexto.Clientes.AddRange(clienteActivo, clienteInactivo);
            await contexto.SaveChangesAsync();

            // Preparamos los servicios que necesita el Controller.
            var consultarClientes = new ConsultarClientes(contexto);

            var consultarTipos = new ConsultarTiposProyectoParaSeleccion(contexto);

            // Preparamos el caso de uso que ahora necesita el Controller.
            var registrarPresupuesto = new RegistrarPresupuesto(
                new RepositorioPresupuestos(contexto),
                new ConsultaReferenciasPresupuesto(contexto),
                new RepositorioUsuarios(contexto));

            var controller = new PresupuestosController(
                consultarClientes,
                consultarTipos,
                registrarPresupuesto)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Ejecutamos el GET que muestra el formulario.
            IActionResult resultado = await controller.Crear();

            // Verificamos que devuelve una vista con el ViewModel correcto.
            ViewResult vista = Assert.IsType<ViewResult>(resultado);

            CrearPresupuestoViewModel modelo =
                Assert.IsType<CrearPresupuestoViewModel>(vista.Model);

            // Debe aparecer solamente el cliente activo.
            var clienteSeleccionable = Assert.Single(modelo.Clientes);

            Assert.Equal("Cliente activo", clienteSeleccionable.Text);
            Assert.Equal(clienteActivo.Id.ToString(), clienteSeleccionable.Value);

            // El cliente inactivo no puede aparecer.
            Assert.DoesNotContain(
                modelo.Clientes,
                cliente => cliente.Value == clienteInactivo.Id.ToString());

            // Comprobamos los dos tipos de proyecto provisorios
            // que actualmente están configurados en la base.
            Assert.Collection(modelo.TiposProyecto,
                primero =>
                {
                    Assert.Equal("Eficiencia energética", primero.Text);
                },
                segundo =>
                {
                    Assert.Equal("Obra", segundo.Text);
                });
        }
    }
}
