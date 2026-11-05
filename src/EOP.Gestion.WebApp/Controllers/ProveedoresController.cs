using EOP.Gestion.Core.CasosDeUso.Proveedores;
using EOP.Gestion.Core.DTOs.Clientes;
using EOP.Gestion.Core.Entidades.Proveedores;
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
        // El país que aparece elegido al abrir el formulario.
        private const string PaisPredeterminadoTelefono = "UY";

        // Nos da la lista de países para el selector del teléfono. Es el mismo servicio
        // que usa el formulario de Clientes, así la lista está en un solo lugar.
        private readonly IServicioTelefonos _servicioTelefonos;

        // Caso de uso que valida y guarda el proveedor con sus cuentas.
        private readonly RegistrarProveedor _registrarProveedor;

        public ProveedoresController(IServicioTelefonos servicioTelefonos, RegistrarProveedor registrarProveedor)
        {
            _servicioTelefonos = servicioTelefonos;
            _registrarProveedor = registrarProveedor;
        }

        // GET: /Proveedores/Registrar
        // Muestra el formulario de alta vacío.
        //
        // El permiso va en la acción y no en toda la clase porque el Operador también va a usar
        // esta sección cuando llegue el listado (US-074). Por eso, el POST de S2-T13 va a
        // necesitar este mismo atributo: no lo hereda de acá.
        //
        // RF-23: el alta es solo para Administrador y Supervisor. Con la coma alcanza con tener uno
        // de los dos.
        [HttpGet]
        [Authorize(Roles = "Administrador,Supervisor")]
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

        // POST: /Proveedores/Registrar
        // Recibe el formulario y coordina el alta. Las reglas de negocio no están acá: son del
        // caso de uso. Este método solo traduce entre el formulario y Core.
        //
        // ValidateAntiForgeryToken rechaza los pedidos que no salieron de nuestro formulario, y
        // el permiso se repite porque un POST directo no pasa por el GET (RNF-4).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> Registrar(RegistrarProveedorViewModel modelo)
        {
            // Si el formulario ya viene mal (faltan datos, o un banco que no existe), ni siquiera
            // armamos el Proveedor.
            if (!ModelState.IsValid)
            {
                return MostrarFormularioConErrores(modelo);
            }

            Proveedor proveedor = CrearProveedor(modelo);

            List<string> errores = await _registrarProveedor.EjecutarAsync(proveedor, modelo.PaisTelefono, modelo.NumeroNacionalTelefono, HttpContext.RequestAborted);

            if (errores.Count > 0)
            {
                foreach (string error in errores)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return MostrarFormularioConErrores(modelo);
            }

            TempData["MensajeExito"] = "El proveedor se registró correctamente.";

            // Post/Redirect/Get: en vez de devolver la pantalla desde este POST, mandamos al
            // navegador a pedirla con un GET. Si después el usuario recarga, repite ese GET y no
            // vuelve a enviar el alta. TempData sobrevive justo hasta ese pedido siguiente.
            return RedirectToAction(nameof(Registrar));
        }

        // Pasa los datos del formulario a la entidad, cuenta por cuenta. Agregarlas a
        // Proveedor.CuentasBancarias es lo que después le dice a EF que son de este proveedor.
        //
        // Usamos .Value en Banco y Moneda sabiendo que no son null: acá solo se llega con el
        // ModelState válido y los dos son [Required]. Lo elegimos antes que GetValueOrDefault()
        // porque, si esa garantía se rompiera algún día, .Value lanza una excepción, mientras que
        // el otro convertiría el dato faltante en un 0 sin avisar.
        private static Proveedor CrearProveedor(RegistrarProveedorViewModel modelo)
        {
            var proveedor = new Proveedor
            {
                Nombre = modelo.Nombre,
                Rut = modelo.Rut,
                CorreoContacto = modelo.CorreoContacto,
                CorreoFacturacion = modelo.CorreoFacturacion
            };

            foreach (CuentaBancariaViewModel cuenta in modelo.CuentasBancarias)
            {
                proveedor.CuentasBancarias.Add(new CuentaBancaria
                {
                    Banco = cuenta.Banco!.Value,
                    NumeroCuenta = cuenta.NumeroCuenta,
                    Titular = cuenta.Titular,
                    Moneda = cuenta.Moneda!.Value
                });
            }

            return proveedor;
        }

        // Vuelve a mostrar el formulario con lo que escribió el usuario. Los valores y las cuentas
        // ya vienen en el modelo y en ModelState, así que solo falta lo que no viaja en el POST.
        private IActionResult MostrarFormularioConErrores(RegistrarProveedorViewModel modelo)
        {
            // Un POST sin cuentas no se puede corregir si la pantalla no tiene ningún bloque de
            // cuenta: el script copia el primero para agregar otros, y no habría de dónde copiar.
            if (modelo.CuentasBancarias.Count == 0)
            {
                modelo.CuentasBancarias.Add(new CuentaBancariaViewModel());
            }

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
