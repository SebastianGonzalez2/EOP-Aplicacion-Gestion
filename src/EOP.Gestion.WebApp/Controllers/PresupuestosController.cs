
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.WebApp.ViewModels.Presupuestos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EOP.Gestion.WebApp.Controllers
{
    // Los tres roles pueden crear presupuestos (US-002).
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public class PresupuestosController : Controller
    {
        private readonly ConsultarClientes _consultarClientes;
        private readonly ConsultarTiposProyectoParaSeleccion _consultarTiposProyecto;

        // Recibimos los servicios que consultan los clientes
        // y los tipos de proyecto disponibles.
        public PresupuestosController(ConsultarClientes consultarClientes,ConsultarTiposProyectoParaSeleccion consultarTiposProyecto)
        {
            _consultarClientes = consultarClientes;
            _consultarTiposProyecto = consultarTiposProyecto;
        }

        // GET: muestra el formulario para crear un presupuesto.
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            CrearPresupuestoViewModel modelo = new CrearPresupuestoViewModel();

            // Cargamos los clientes y los tipos de proyecto
            // que aparecerán en los desplegables.
            await CargarOpciones(modelo, HttpContext.RequestAborted);

            return View(modelo);
        }

        // Obtiene las opciones que necesita el formulario.
        private async Task CargarOpciones(CrearPresupuestoViewModel modelo,CancellationToken cancellationToken)
        {
            // Consultamos solamente los clientes activos.
            // Un cliente inactivo no puede utilizarse
            // para crear un presupuesto nuevo.
            var clientes = await _consultarClientes.EjecutarAsync(null,true,cancellationToken);

            // Convertimos los clientes en opciones del desplegable.
            modelo.Clientes = clientes.Select(cliente => new SelectListItem
            {
                Text = cliente.Nombre,
                Value = cliente.Id.ToString()
            }).ToList();

            // Obtenemos los tipos de proyecto registrados.
            var tiposProyecto = await _consultarTiposProyecto.EjecutarAsync(cancellationToken);

            // Preparamos las opciones del segundo desplegable.
            modelo.TiposProyecto = tiposProyecto.Select(tipo => new SelectListItem
            {
                Text = tipo.Nombre,
                Value = tipo.Id.ToString()
            }).ToList();
        }
    }
}
