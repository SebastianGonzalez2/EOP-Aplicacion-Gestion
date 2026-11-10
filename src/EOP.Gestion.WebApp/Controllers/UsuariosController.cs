using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.WebApp.ViewModels.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EOP.Gestion.Infrastructure.Consultas.Usuarios;

namespace EOP.Gestion.WebApp.Controllers
{
    // RF-72: solamente el Administrador puede acceder a la administración de usuarios.
    // El rol lo compara ASP.NET Core con el que tiene el usuario HOY en la tabla Usuarios,
    // porque OnValidatePrincipal lo vuelve a cargar en cada pedido (T13).
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        // Después de modificar un rol, volvemos al listado de usuarios (US-045).
        // Ahí mostramos los mensajes de éxito o error guardados en TempData.
        private const string RutaListadoUsuarios = "~/Usuarios";

        private readonly ModificarRolUsuario _modificarRolUsuario;

        private readonly RegistrarUsuario _registrarUsuario;

        private readonly ConsultarUsuarios _consultarUsuarios;

        public UsuariosController(ModificarRolUsuario modificarRolUsuario, RegistrarUsuario registrarUsuario, ConsultarUsuarios consultarUsuarios)
        {
            _modificarRolUsuario = modificarRolUsuario;
            _registrarUsuario = registrarUsuario;
            _consultarUsuarios = consultarUsuarios;
        }



        // GET: /Usuarios
        // Muestra el listado administrativo de todos los usuarios registrados.
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            // Consultamos los usuarios sin aplicar filtros.
            var usuarios = await _consultarUsuarios.EjecutarAsync(HttpContext.RequestAborted);

            // Enviamos los usuarios a la vista para mostrarlos en una tabla.
            return View(usuarios);
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

            // Después de registrar un usuario, volvemos al formulario mediante un GET.
            // Así, si el Administrador actualiza la página, no vuelve a enviar el POST.
            // Mantenemos el comportamiento del registro (US-043).
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
