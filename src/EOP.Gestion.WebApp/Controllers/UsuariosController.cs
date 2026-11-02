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
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    public class UsuariosController : Controller
    {
        // Después de cada intento volvemos al listado de usuarios, donde se muestra el resultado.
        // Ese listado llega con US-045: hasta entonces, esta redirección termina en un 404.
        // No usamos RedirectToAction porque falla si la acción todavía no existe.
        private const string RutaListadoUsuarios = "~/Usuarios";

        private readonly ModificarRolUsuario _modificarRolUsuario;

        public UsuariosController(ModificarRolUsuario modificarRolUsuario)
        {
            _modificarRolUsuario = modificarRolUsuario;
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

            ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(
                modelo.UsuarioId,
                modelo.NuevoRol!.Value, // Después de la validación sabemos que [Required] se cumplió.
                HttpContext.RequestAborted);

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
