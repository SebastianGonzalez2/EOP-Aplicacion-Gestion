
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EOP.Gestion.WebApp.ViewModels.Clientes;
using EOP.Gestion.Core.CasosDeUso.Clientes;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;

namespace EOP.Gestion.WebApp.Controllers
{
    [Authorize]// Pueden acceder a este controlador únicamente los usuarios autenticados.
    public class ClientesController : Controller
    {
        // Caso de uso encargado de validar y registrar clientes.
        private readonly RegistrarCliente _registrarCliente;

<<<<<<< HEAD
        // Servicio encargado de consultar y filtrar clientes.
        private readonly ConsultarClientes _consultarClientes;



        // Recibimos el caso de uso mediante inyección de dependencias.
        public ClientesController(RegistrarCliente registrarCliente, ConsultarClientes consultarClientes)
        {
            _registrarCliente = registrarCliente;
            _consultarClientes = consultarClientes;
=======
        // Nos da la lista de países para el selector del número de WhatsApp.
        private readonly IServicioTelefonos _servicioTelefonos;

        // El país que aparece elegido al abrir el formulario.
        private const string PaisPredeterminadoWhatsApp = "UY";

        // Recibimos el caso de uso mediante inyección de dependencias.
        public ClientesController(RegistrarCliente registrarCliente, IServicioTelefonos servicioTelefonos)
        {
            _registrarCliente = registrarCliente;
            _servicioTelefonos = servicioTelefonos;
>>>>>>> origin/main
        }

        // GET: muestra el listado de clientes.
        // Permite buscar por nombre o RUT y filtrar por estado.
        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda,bool? activo)
        {
            // Consultamos los clientes aplicando los filtros recibidos.
            var clientes = await _consultarClientes.EjecutarAsync(busqueda,activo,HttpContext.RequestAborted);

            // Conservamos los filtros para mostrarlos en la pantalla.
            ViewBag.Busqueda = busqueda;
            ViewBag.Activo = activo;

            // Enviamos los clientes a la vista.
            return View(clientes);
        }





        // GET: muestra el formulario de registro.
        [HttpGet]
        public IActionResult Registrar()
        {
            var modelo = new RegistrarClienteViewModel
            {
                PaisWhatsApp = PaisPredeterminadoWhatsApp
            };

            CargarPaisesWhatsApp(modelo);

            return View(modelo);
        }

        // POST: recibe los datos enviados desde el formulario.
        [HttpPost]
        [ValidateAntiForgeryToken] //ayuda a proteger contra ataques CSRF (Cross-Site Request Forgery)
        public async Task<IActionResult> Registrar(RegistrarClienteViewModel modelo)
        {
            // Comprobamos si ASP.NET Core detectó errores
            // al recibir los datos del formulario.
            if (!ModelState.IsValid)
            {
                CargarPaisesWhatsApp(modelo);

                return View(modelo);
            }

            // Convertimos los datos del formulario en una entidad Cliente.
            var cliente = new Cliente
            {
                Nombre = modelo.Nombre,
                Rut = modelo.Rut,
                CorreoContacto = modelo.CorreoContacto,
                CorreoFacturacion = modelo.CorreoFacturacion
            };

            // Ejecutamos el caso de uso que valida y guarda el cliente.
            // El número de WhatsApp no lo armamos acá: el Controller solo coordina,
            // y es el caso de uso el que deja el número internacional en el cliente.
            List<string> errores = await _registrarCliente.EjecutarAsync(cliente, modelo.PaisWhatsApp, modelo.NumeroNacionalWhatsApp, HttpContext.RequestAborted);

            // Si existen errores de negocio, los mostramos en el formulario.
            if (errores.Count > 0)
            {
                foreach (string error in errores)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                CargarPaisesWhatsApp(modelo);

                return View(modelo);
            }

            // Guardamos un mensaje para mostrar después del registro.
            TempData["MensajeExito"] = "El cliente se registró correctamente.";

            // Volvemos al formulario para registrar otro cliente.
            return RedirectToAction(nameof(Registrar));
        }

        // Las opciones del desplegable no viajan en el POST (el navegador solo manda la elegida),
        // así que hay que volver a cargarlas cada vez que se muestra el formulario.
        private void CargarPaisesWhatsApp(RegistrarClienteViewModel modelo)
        {
            modelo.PaisesWhatsApp = _servicioTelefonos.ObtenerPaises()
                .Select(pais => new SelectListItem(pais.Descripcion, pais.CodigoIso))
                .ToList();
        }
    }
}
