
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.WebApp.ViewModels.Materiales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EOP.Gestion.WebApp.Controllers
{
    // Solamente Supervisor y Administrador pueden registrar materiales.
    [Authorize(Roles = nameof(RolUsuario.Supervisor) + "," + nameof(RolUsuario.Administrador))]
    public class MaterialesController : Controller
    {
        private readonly ConsultarProveedoresParaSeleccion _consultarProveedores;

        // Recibimos el servicio que consulta los proveedores existentes.
        public MaterialesController(ConsultarProveedoresParaSeleccion consultarProveedores)
        {
            _consultarProveedores = consultarProveedores;
        }

        // GET: muestra el formulario de registro de materiales.
        [HttpGet]
        public async Task<IActionResult> Registrar()
        {
            RegistrarMaterialViewModel modelo = new RegistrarMaterialViewModel();

            // Cargamos los proveedores para mostrarlos en el desplegable.
            await CargarProveedores(modelo, HttpContext.RequestAborted);

            return View(modelo);
        }

        // Obtiene los proveedores y prepara las opciones del desplegable.
        private async Task CargarProveedores(RegistrarMaterialViewModel modelo,CancellationToken cancellationToken)
        {
            var proveedores = await _consultarProveedores.EjecutarAsync(cancellationToken);

            modelo.Proveedores = proveedores.Select(proveedor => new SelectListItem{Text = proveedor.Nombre,Value = proveedor.Id.ToString()}).ToList();
        }


        // POST: recibe los datos enviados desde el formulario.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarMaterialViewModel modelo)
        {
            // Volvemos a cargar los proveedores porque la lista de opciones
            // no se envía desde el formulario.
            await CargarProveedores(modelo, HttpContext.RequestAborted);

            // Comprobamos las validaciones definidas en el ViewModel.
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Aunque el desplegable muestre proveedores existentes,
            // alguien podría enviar manualmente un Id inventado.
            // Por eso verificamos que el Id exista entre los proveedores
            // que acabamos de consultar en la base de datos.
            int proveedorId = modelo.ProveedorId.GetValueOrDefault();

            bool proveedorExiste = modelo.Proveedores.Any(
                proveedor => proveedor.Value == proveedorId.ToString());

            if (!proveedorExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.ProveedorId),
                    "El proveedor seleccionado no existe.");

                return View(modelo);
            }

            // Todavía falta la relación MaterialProveedor y el caso de uso
            // que guardará ambos registros juntos. No guardamos solamente
            // el Material porque incumpliríamos la US-041.
            ModelState.AddModelError(
                string.Empty,
                "El registro todavía no está disponible. Falta implementar el guardado conjunto del material y su primera relación con un proveedor.");

            return View(modelo);
        }






    }
}
