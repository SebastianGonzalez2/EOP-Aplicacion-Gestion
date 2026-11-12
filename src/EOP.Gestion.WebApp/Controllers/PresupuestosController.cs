
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.WebApp.ViewModels.Presupuestos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using Microsoft.Identity.Web;


namespace EOP.Gestion.WebApp.Controllers
{
    // Los tres roles pueden crear presupuestos (US-002).
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public class PresupuestosController : Controller
    {
        private readonly ConsultarClientes _consultarClientes;
        private readonly ConsultarTiposProyectoParaSeleccion _consultarTiposProyecto;

        private readonly RegistrarPresupuesto _registrarPresupuesto;

        // Consulta que obtiene los presupuestos para el listado.
        private readonly ConsultarPresupuestos _consultarPresupuestos;

        // Recibimos los servicios que consultan los clientes
        // y los tipos de proyecto disponibles.
        public PresupuestosController(ConsultarClientes consultarClientes,ConsultarTiposProyectoParaSeleccion consultarTiposProyecto, RegistrarPresupuesto registrarPresupuesto, ConsultarPresupuestos consultarPresupuestos)
        {
            _consultarClientes = consultarClientes;
            _consultarTiposProyecto = consultarTiposProyecto;
            _registrarPresupuesto = registrarPresupuesto;
            _consultarPresupuestos = consultarPresupuestos;
        }


        // GET: /Presupuestos
        // Muestra el listado de presupuestos registrados (US-001).
        [HttpGet]
        [Authorize(Roles = "Operador,Supervisor,Administrador")]
        public async Task<IActionResult> Index()
        {
            // Consultamos todos los presupuestos registrados.
            // Los filtros se incorporarán en la tarea S2-T27.
            var presupuestos = await _consultarPresupuestos.EjecutarAsync(HttpContext.RequestAborted);

            // Enviamos los datos a la vista Index.cshtml.
            return View(presupuestos);
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


        // POST: recibe los datos enviados desde el formulario.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearPresupuestoViewModel modelo)
        {
            // Primero comprobamos las validaciones del ViewModel.
            if (!ModelState.IsValid)
            {
                // Si hay errores, volvemos a cargar los desplegables.
                await CargarOpciones(modelo, HttpContext.RequestAborted);

                return View(modelo);
            }

            // Preparamos la entidad con los datos del formulario.
            // El Responsable, estado y código los establece el sistema.
            Presupuesto presupuesto = new Presupuesto
            {
                NombreProyecto = modelo.NombreProyecto,
                ClienteId = modelo.ClienteId!.Value,
                TipoProyectoId = modelo.TipoProyectoId!.Value,
                PlazoVigenciaDias = modelo.PlazoVigenciaDias!.Value
            };

            // Identificamos al usuario autenticado mediante Microsoft.
            string? entraObjectId = User.GetObjectId();

            // Ejecutamos el caso de uso que valida y guarda.
            List<string> errores = await _registrarPresupuesto.EjecutarAsync(presupuesto,entraObjectId,HttpContext.RequestAborted);

            // Si existen errores, los mostramos en el formulario.
            if (errores.Count > 0)
            {
                foreach (string error in errores)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                await CargarOpciones(modelo, HttpContext.RequestAborted);

                return View(modelo);
            }

            // Mostramos el código del presupuesto registrado.
            TempData["MensajeExito"] ="El presupuesto se creó correctamente. Código: " +presupuesto.Codigo;

            return RedirectToAction(nameof(Crear));
        }
    }
}
