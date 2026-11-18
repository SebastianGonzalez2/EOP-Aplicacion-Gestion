
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.WebApp.ViewModels.Presupuestos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using Microsoft.Identity.Web;
using EOP.Gestion.Infrastructure.Consultas.Usuarios;


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

        // Consulta para obtener la información de un presupuesto.
        private readonly ConsultarPresupuestoDetalle _consultarPresupuestoDetalle;

        // Consultamos los usuarios para ofrecerlos
        // como responsables en el filtro de presupuestos.
        private readonly ConsultarUsuarios _consultarUsuarios;


        // Recibimos los servicios que consultan los clientes
        // y los tipos de proyecto disponibles.
        public PresupuestosController(ConsultarClientes consultarClientes,ConsultarTiposProyectoParaSeleccion consultarTiposProyecto,
            RegistrarPresupuesto registrarPresupuesto, ConsultarPresupuestos consultarPresupuestos, ConsultarUsuarios consultarUsuarios,
            ConsultarPresupuestoDetalle consultarPresupuestoDetalle)
        {
            _consultarClientes = consultarClientes;
            _consultarTiposProyecto = consultarTiposProyecto;
            _registrarPresupuesto = registrarPresupuesto;
            _consultarPresupuestos = consultarPresupuestos;
            _consultarUsuarios = consultarUsuarios;
            _consultarPresupuestoDetalle = consultarPresupuestoDetalle;
        }


        // GET: /Presupuestos
        // Muestra el listado aplicando los filtros seleccionados (US-001).
        [HttpGet]
        [Authorize(Roles = "Operador,Supervisor,Administrador")]
        public async Task<IActionResult> Index(string? codigo = null,EstadoPresupuesto? estado = null,int? clienteId = null,DateTime? fecha = null,
            int? responsableId = null)
        {
            // Consultamos los presupuestos aplicando los filtros.
            var presupuestos = await _consultarPresupuestos.EjecutarAsync(HttpContext.RequestAborted,
                codigo,estado,clienteId,fecha,responsableId);

            // Obtenemos todos los clientes, incluidos los inactivos,
            // porque también podemos buscar presupuestos históricos.
            var clientes = await _consultarClientes.EjecutarAsync(null, null, HttpContext.RequestAborted);

            // Preparamos las opciones del desplegable de clientes.
            ViewBag.Clientes = clientes.Select(cliente => new SelectListItem
            {
                Text = cliente.Nombre,
                Value = cliente.Id.ToString()
            }).ToList();

            // Obtenemos los usuarios para poder filtrar
            // por el responsable de cada presupuesto.
            var usuarios = await _consultarUsuarios.EjecutarAsync(HttpContext.RequestAborted);

            // Incluimos usuarios inactivos porque pueden seguir
            // siendo responsables de presupuestos anteriores.
            ViewBag.Responsables = usuarios.Select(usuario => new SelectListItem
            {
                Text = usuario.NombreCompleto,
                Value = usuario.Id.ToString()
            }).ToList();

            // Guardamos los filtros seleccionados para que
            // la vista pueda mantenerlos después de buscar.
            ViewBag.Codigo = codigo;
            ViewBag.Estado = estado;
            ViewBag.ClienteId = clienteId;
            ViewBag.Fecha = fecha;
            ViewBag.ResponsableId = responsableId;

            // Mostramos el listado con los filtros aplicados.
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


        // GET: /Presupuestos/Detalle/5
        // Muestra la información de un presupuesto seleccionado (US-001).
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            // Buscamos el presupuesto utilizando el ID recibido.
            var presupuesto = await _consultarPresupuestoDetalle.EjecutarAsync(id,HttpContext.RequestAborted);

            // Si no existe, devolvemos una respuesta 404.
            if (presupuesto == null)
            {
                return NotFound();
            }

            // Enviamos los datos a la vista Detalle.cshtml.
            return View(presupuesto);
        }


    }
}
