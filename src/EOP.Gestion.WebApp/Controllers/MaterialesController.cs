
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.WebApp.ViewModels.Materiales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Infrastructure.Consultas.Materiales;
using EOP.Gestion.Core.Entidades.Catalogo;

namespace EOP.Gestion.WebApp.Controllers
{
    // Los tres roles pueden consultar materiales.
    // Las operaciones restringidas tendrán su propia autorización.
    [Authorize]
    public class MaterialesController : Controller
    {
        private readonly ConsultarProveedoresParaSeleccion _consultarProveedores;
        private readonly RegistrarMaterial _registrarMaterial;

        // Servicio que consulta los materiales y aplica los filtros del catálogo.
        private readonly ConsultarMateriales _consultarMateriales;

        // Recibimos el servicio que consulta los proveedores existentes.
        // Caso de uso que valida y guarda una nueva relación material-proveedor.
        private readonly AgregarProveedorAMaterial _agregarProveedorAMaterial;

        public MaterialesController(ConsultarProveedoresParaSeleccion consultarProveedores, RegistrarMaterial registrarMaterial, ConsultarMateriales consultarMateriales,
                                    AgregarProveedorAMaterial agregarProveedorAMaterial)
        {
            _consultarProveedores = consultarProveedores;
            _registrarMaterial = registrarMaterial;
            _consultarMateriales = consultarMateriales;
            _agregarProveedorAMaterial = agregarProveedorAMaterial;
        }



        // GET: muestra el catálogo de materiales.
        // Permite buscar por nombre y filtrar por unidad y proveedor.
        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda,UnidadMedida? unidad,int? proveedorId)
        {
            // Consultamos los materiales aplicando los filtros recibidos.
            var materiales = await _consultarMateriales.EjecutarAsync(busqueda,unidad,proveedorId,HttpContext.RequestAborted);

            // Obtenemos los proveedores existentes para el filtro.
            var proveedores = await _consultarProveedores.EjecutarAsync(HttpContext.RequestAborted);

            // Preparamos las opciones del desplegable de proveedores.
            var opcionesProveedores = proveedores.Select(proveedor =>new SelectListItem
                {
                    Text = proveedor.Nombre,
                    Value = proveedor.Id.ToString()
                }).ToList();

            // Conservamos los filtros seleccionados para mostrarlos en la vista.
            ViewBag.Busqueda = busqueda;
            ViewBag.Unidad = unidad;
            ViewBag.ProveedorId = proveedorId;
            ViewBag.Proveedores = opcionesProveedores;

            // Enviamos los materiales a la vista.
            return View(materiales);
        }






        // GET: muestra el formulario de registro de materiales.
        // Solamente Supervisor y Administrador pueden registrar.
        [HttpGet]
        [Authorize(Roles = "Supervisor,Administrador")]
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


        [HttpPost]
        [Authorize(Roles = "Supervisor,Administrador")]
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

            
            return RedirectToAction(nameof(Index));
        }




        // GET: muestra el formulario para agregar un proveedor
        // a un material que ya existe en el catálogo.
        [HttpGet]
        [Authorize(Roles = "Supervisor,Administrador")]
        public async Task<IActionResult> AgregarProveedor(int materialId)
        {
            // Buscamos el material seleccionado por su Id.
            var material = await _consultarMateriales.ObtenerPorIdAsync(materialId,HttpContext.RequestAborted);

            // Si el material no existe, devolvemos un error 404.
            if (material == null)
            {
                return NotFound();
            }

            // Preparamos el ViewModel con los datos del material.
            AgregarProveedorMaterialViewModel modelo = new AgregarProveedorMaterialViewModel
            {
                MaterialId = material.Id,
                NombreMaterial = material.Nombre
            };

            // Cargamos los proveedores registrados para el desplegable.
            await CargarProveedores(modelo, HttpContext.RequestAborted);

            // Enviamos el ViewModel a la vista AgregarProveedor.cshtml.
            return View(modelo);
        }

        // Obtiene los proveedores registrados y prepara las opciones del desplegable.
        private async Task CargarProveedores(AgregarProveedorMaterialViewModel modelo, CancellationToken cancellationToken)
        {
            var proveedores = await _consultarProveedores.EjecutarAsync(cancellationToken);

            modelo.Proveedores = proveedores.Select(proveedor => new SelectListItem
            {
                Text = proveedor.Nombre,
                Value = proveedor.Id.ToString()
            }).ToList();
        }

        // POST: guarda la nueva relación material-proveedor (US-010B).
        // El [Authorize] va en esta acción también: ocultar el botón no alcanza (RNF-4),
        // un Operador podría mandar el POST a mano y el servidor debe rechazarlo.
        [HttpPost]
        [Authorize(Roles = "Supervisor,Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarProveedor(AgregarProveedorMaterialViewModel modelo)
        {
            // El nombre del material no viaja en el formulario (es BindNever),
            // así que lo volvemos a buscar. Si el material no existe, no hay nada que mostrar.
            var material = await _consultarMateriales.ObtenerPorIdAsync(modelo.MaterialId, HttpContext.RequestAborted);

            if (material == null)
            {
                return NotFound();
            }

            modelo.NombreMaterial = material.Nombre;

            // Las opciones del desplegable tampoco viajan, hay que cargarlas de nuevo.
            await CargarProveedores(modelo, HttpContext.RequestAborted);

            // Comprobamos las validaciones definidas en el ViewModel.
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Convertimos el texto del formulario al enum Moneda.
            // El ViewModel ya garantiza que sea UYU o USD, pero lo verificamos igual
            // porque Enum.Parse lanzaría una excepción con cualquier otro texto.
            if (modelo.Moneda != "UYU" && modelo.Moneda != "USD")
            {
                ModelState.AddModelError(nameof(modelo.Moneda), "La moneda debe ser UYU o USD.");

                return View(modelo);
            }

            Moneda moneda = Enum.Parse<Moneda>(modelo.Moneda);

            // El caso de uso valida (proveedor existente, precio, duplicado) y guarda.
            List<string> errores = await _agregarProveedorAMaterial.EjecutarAsync(
                modelo.MaterialId,
                modelo.ProveedorId,
                modelo.PrecioUnitario,
                moneda,
                HttpContext.RequestAborted);

            if (errores.Count > 0)
            {
                foreach (string error in errores)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(modelo);
            }

            TempData["MensajeExito"] = "Proveedor agregado correctamente al material.";

            return RedirectToAction(nameof(Index));
        }



    }
}
