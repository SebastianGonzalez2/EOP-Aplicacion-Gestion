using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.WebApp.ViewModels.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EOP.Gestion.WebApp.Controllers
{
    // RF-72: solamente el Administrador puede acceder a la administración de usuarios.
    // El rol lo compara ASP.NET Core con el que tiene el usuario HOY en la tabla Usuarios,
    // porque OnValidatePrincipal lo vuelve a cargar en cada pedido (T13).
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        // Después de cada intento volvemos al listado de usuarios, donde se muestra el resultado.
        // Ese listado llega con US-045: hasta entonces, esta redirección termina en un 404.
        // No usamos RedirectToAction porque falla si la acción todavía no existe.
        private const string RutaListadoUsuarios = "~/Usuarios";

        private readonly ModificarRolUsuario _modificarRolUsuario;

        private readonly RegistrarUsuario _registrarUsuario;

        public UsuariosController(ModificarRolUsuario modificarRolUsuario, RegistrarUsuario registrarUsuario)
        {
            _modificarRolUsuario = modificarRolUsuario;
            _registrarUsuario = registrarUsuario;
        }

        // GET: /Usuarios/Registrar
        // Muestra el formulario de alta vacío. No existe registro público: el único que puede
        // abrirlo es un Administrador con sesión (US-043).
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Registrar()
        {
            return View(new RegistrarUsuarioViewModel());
        }

        // POST: /Usuarios/Registrar
        // Recibe el formulario.
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarUsuarioViewModel modelo)
        {
            // RNF-10: validamos en el servidor aunque el navegador ya lo haya hecho,
            // porque cualquiera puede armar un pedido a mano.
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Después de la validación sabemos que el texto es el nombre de uno de los roles.
            RolUsuario rol = Enum.Parse<RolUsuario>(modelo.Rol!);

            List<string> errores = await _registrarUsuario.EjecutarAsync(modelo.NombreCompleto, modelo.CorreoCorporativo, rol, HttpContext.RequestAborted);

            if (errores.Count > 0)
            {
                foreach (string error in errores)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(modelo);
            }

            TempData["MensajeExito"] = "El usuario se registró correctamente. Quedó Activo y se va a vincular con su cuenta de Microsoft en su primer inicio de sesión.";

            // Redirigimos en vez de devolver la vista para que el navegador termine en un GET.
            // Si no, actualizar la página volvería a mandar este POST. Vamos al formulario y no al
            // listado de ModificarRol porque ese listado todavía no existe (US-045) y daría un 404.
            return RedirectToAction(nameof(Registrar));
        }

        // POST: /Usuarios/ModificarRol
        // Un POST y con token antifalsificación porque cambia datos: así, otra página
        // no puede aprovechar la sesión de un Administrador para hacerlo en su nombre (CSRF).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModificarRol(ModificarRolViewModel modelo)
        {
            // RNF-10: validamos en el servidor aunque el formulario solo ofrezca opciones válidas,
            // porque cualquiera puede armar un pedido a mano.
            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "No se pudo modificar el rol: los datos enviados no son válidos.";

                return LocalRedirect(RutaListadoUsuarios);
            }

            // Después de la validación sabemos que el texto es el nombre de uno de los roles.
            RolUsuario nuevoRol = Enum.Parse<RolUsuario>(modelo.NuevoRol!);

            ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(modelo.UsuarioId,nuevoRol,HttpContext.RequestAborted);

            switch (resultado)
            {
                case ResultadoModificarRol.RolModificado:
                    TempData["MensajeExito"] = "El rol del usuario se modificó correctamente.";
                    break;

                case ResultadoModificarRol.RolNoPermitido:
                    TempData["MensajeError"] = "No se pudo modificar el rol: el rol seleccionado no es válido.";
                    break;

                case ResultadoModificarRol.UsuarioNoEncontrado:
                    TempData["MensajeError"] = "No se pudo modificar el rol: el usuario no existe.";
                    break;

                case ResultadoModificarRol.UltimoAdministradorActivo:
                    TempData["MensajeError"] = "No se puede modificar el rol: es el último Administrador activo del sistema.";
                    break;
            }

            return LocalRedirect(RutaListadoUsuarios);
        }
    }
}
