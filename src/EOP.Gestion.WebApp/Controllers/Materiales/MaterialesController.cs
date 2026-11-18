
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.WebApp.ViewModels.Materiales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Infrastructure.Consultas.Materiales;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.DTOs.Materiales;

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

        // Caso de uso que valida y guarda la edición de un material existente (US-071).
        private readonly EditarMaterial _editarMaterial;

        public MaterialesController(ConsultarProveedoresParaSeleccion consultarProveedores, RegistrarMaterial registrarMaterial, ConsultarMateriales consultarMateriales,
                                    AgregarProveedorAMaterial agregarProveedorAMaterial, EditarMaterial editarMaterial)
        {
            _consultarProveedores = consultarProveedores;
            _registrarMaterial = registrarMaterial;
            _consultarMateriales = consultarMateriales;
            _agregarProveedorAMaterial = agregarProveedorAMaterial;
            _editarMaterial = editarMaterial;
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



        // GET: muestra el formulario con los datos actuales del material (US-071).
        // Solamente Supervisor y Administrador pueden editar: el Operador solo consulta.
        [HttpGet]
        [Authorize(Roles = "Supervisor,Administrador")]
        public async Task<IActionResult> Editar(int materialId)
        {
            MaterialEdicionDto? material = await _consultarMateriales.ObtenerParaEdicionAsync(materialId, HttpContext.RequestAborted);

            // Si el material no existe, devolvemos un error 404.
            if (material == null)
            {
                return NotFound();
            }

            EditarMaterialViewModel modelo = new EditarMaterialViewModel
            {
                MaterialId = material.Id,
                Nombre = material.Nombre,
                Unidad = material.Unidad
            };

            foreach (ReferenciaEdicionDto referencia in material.Referencias)
            {
                modelo.Referencias.Add(new EditarReferenciaPrecioViewModel
                {
                    RelacionId = referencia.RelacionId,
                    ProveedorNombre = referencia.ProveedorNombre,
                    FechaUltimaActualizacion = referencia.FechaUltimaActualizacion,
                    PrecioUnitario = referencia.PrecioUnitario,
                    Moneda = referencia.Moneda.ToString()
                });
            }

            return View(modelo);
        }

        // POST: guarda los cambios del material y de sus referencias de precio (US-071).
        // El [Authorize] va también acá: ocultar el botón no alcanza (RNF-4).
        [HttpPost]
        [Authorize(Roles = "Supervisor,Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarMaterialViewModel modelo)
        {
            // El nombre de los proveedores y las fechas no viajan en el formulario (son BindNever).
            // Los volvemos a buscar para poder mostrar de nuevo la pantalla si hay errores.
            MaterialEdicionDto? material = await _consultarMateriales.ObtenerParaEdicionAsync(modelo.MaterialId, HttpContext.RequestAborted);

            if (material == null)
            {
                return NotFound();
            }

            CompletarDatosDeSoloLectura(modelo, material);

            // Comprobamos las validaciones definidas en el ViewModel.
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Pasamos el formulario al formato que espera el caso de uso. Una moneda que no sea
            // UYU ni USD queda en null, y el caso de uso la rechaza con su mensaje.
            List<CambioReferenciaPrecio> cambios = new List<CambioReferenciaPrecio>();

            foreach (EditarReferenciaPrecioViewModel referencia in modelo.Referencias)
            {
                Moneda? moneda = null;

                if (referencia.Moneda == "UYU")
                {
                    moneda = Moneda.UYU;
                }
                else if (referencia.Moneda == "USD")
                {
                    moneda = Moneda.USD;
                }

                cambios.Add(new CambioReferenciaPrecio
                {
                    RelacionId = referencia.RelacionId,
                    PrecioUnitario = referencia.PrecioUnitario,
                    Moneda = moneda
                });
            }

            List<string> errores = await _editarMaterial.EjecutarAsync(
                modelo.MaterialId,
                modelo.Nombre,
                modelo.Unidad,
                cambios,
                HttpContext.RequestAborted);

            if (errores.Count > 0)
            {
                foreach (string error in errores)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(modelo);
            }

            TempData["MensajeExito"] = "Material actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Pone en cada fila del formulario el proveedor y la fecha que figuran en la base.
        // Se cruzan por el Id de la relación, no por la posición, para que un formulario
        // manipulado no pueda mostrar el proveedor equivocado.
        private static void CompletarDatosDeSoloLectura(EditarMaterialViewModel modelo, MaterialEdicionDto material)
        {
            foreach (EditarReferenciaPrecioViewModel referencia in modelo.Referencias)
            {
                ReferenciaEdicionDto? referenciaGuardada = material.Referencias.FirstOrDefault(r => r.RelacionId == referencia.RelacionId);

                if (referenciaGuardada != null)
                {
                    referencia.ProveedorNombre = referenciaGuardada.ProveedorNombre;
                    referencia.FechaUltimaActualizacion = referenciaGuardada.FechaUltimaActualizacion;
                }
            }
        }



    }
}
