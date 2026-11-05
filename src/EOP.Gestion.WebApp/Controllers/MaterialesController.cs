
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.WebApp.ViewModels.Materiales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;


namespace EOP.Gestion.WebApp.Controllers
{
    // Solamente Supervisor y Administrador pueden registrar materiales.
    [Authorize(Roles = "Supervisor,Administrador")]
    public class MaterialesController : Controller
    {
        private readonly ConsultarProveedoresParaSeleccion _consultarProveedores;
        private readonly RegistrarMaterial _registrarMaterial;

        // Recibimos el servicio que consulta los proveedores existentes.
        public MaterialesController(ConsultarProveedoresParaSeleccion consultarProveedores, RegistrarMaterial registrarMaterial)
        {
            _consultarProveedores = consultarProveedores;
            _registrarMaterial = registrarMaterial;
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

            // Comprobamos que la moneda recibida sea válida.
            if (modelo.Moneda != "UYU" && modelo.Moneda != "USD")
            {
                ModelState.AddModelError(nameof(modelo.Moneda),"La moneda debe ser UYU o USD.");

                return View(modelo);
            }

            // Convertimos el texto del formulario al enum Moneda.
            Moneda moneda = Enum.Parse<Moneda>(modelo.Moneda);

            // Ejecutamos el caso de uso que valida y guarda el material
            // junto con su primera relación material-proveedor.
            List<string> errores = await _registrarMaterial.EjecutarAsync(
                modelo.Nombre,
                modelo.Unidad,
                modelo.ProveedorId,
                modelo.PrecioUnitario,
                moneda,
                HttpContext.RequestAborted);

            // Si encontramos errores, los mostramos en el formulario.
            if (errores.Count > 0)
            {
                foreach (string error in errores)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(modelo);
            }

            // El registro fue correcto.
            TempData["MensajeExito"] = "Material registrado correctamente.";

            // Redirigimos para evitar repetir el registro al actualizar.
            return RedirectToAction(nameof(Registrar));
        }






    }
}
