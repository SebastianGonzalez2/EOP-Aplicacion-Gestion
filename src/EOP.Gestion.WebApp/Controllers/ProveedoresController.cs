using EOP.Gestion.Core.DTOs.Clientes;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.WebApp.ViewModels.Proveedores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EOP.Gestion.WebApp.Controllers
{
    [Authorize] // Para entrar a la sección Proveedores hay que tener sesión, sea cual sea el rol.
    public class ProveedoresController : Controller
    {
        // RF-23: el alta es solo para Administrador y Supervisor. Con la coma alcanza con tener uno
        // de los dos. Usamos nameof para que un rol mal escrito no compile, en vez de dejar
        // a todos afuera sin ningún aviso.
        private const string RolesQuePuedenRegistrar = nameof(RolUsuario.Administrador) + "," + nameof(RolUsuario.Supervisor);

        // El país que aparece elegido al abrir el formulario.
        private const string PaisPredeterminadoTelefono = "UY";

        // Nos da la lista de países para el selector del teléfono. Es el mismo servicio
        // que usa el formulario de Clientes, así la lista está en un solo lugar.
        private readonly IServicioTelefonos _servicioTelefonos;

        public ProveedoresController(IServicioTelefonos servicioTelefonos)
        {
            _servicioTelefonos = servicioTelefonos;
        }

        // GET: /Proveedores/Registrar
        // Muestra el formulario de alta vacío.
        //
        // El permiso va en la acción y no en toda la clase porque el Operador también va a usar
        // esta sección cuando llegue el listado (US-074). Por eso, el POST de S2-T13 va a
        // necesitar este mismo atributo: no lo hereda de acá.
        //
        // Todavía no hay POST a propósito: guardar le corresponde a S2-T13, y una acción
        // que no guarda de verdad haría creer que el proveedor quedó registrado.
        [HttpGet]
        [Authorize(Roles = RolesQuePuedenRegistrar)]
        public IActionResult Registrar()
        {
            var modelo = new RegistrarProveedorViewModel
            {
                PaisTelefono = PaisPredeterminadoTelefono
            };

            // RF-23: todo proveedor necesita al menos una cuenta, así que el
            // formulario ya arranca con los campos de la primera.
            modelo.CuentasBancarias.Add(new CuentaBancariaViewModel());

            CargarPaisesTelefono(modelo);

            return View(modelo);
        }

        // Está en un método aparte porque las opciones del desplegable no viajan en el POST (el
        // navegador solo manda la elegida). Cuando S2-T13 tenga que volver a mostrar el formulario
        // con errores, va a tener que cargarlas de nuevo.
        private void CargarPaisesTelefono(RegistrarProveedorViewModel modelo)
        {
            foreach (PaisTelefonico pais in _servicioTelefonos.ObtenerPaises())
            {
                modelo.PaisesTelefono.Add(new SelectListItem(pais.Descripcion, pais.CodigoIso));
            }
        }
    }
}
