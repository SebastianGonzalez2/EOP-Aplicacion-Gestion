using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EOP.Gestion.WebApp.Controllers
{
    // Este Controller maneja todo lo que tiene que ver con la sesión del usuario:
    // entrar y salir.
    //
    // No tiene reglas de negocio. Lo único que hace es pedirle a ASP.NET Core
    // que arranque el login con Microsoft. Todo el trabajo pesado (hablar con
    // Entra ID, validar el token, crear la cookie) lo hace Microsoft.Identity.Web.
    public class CuentaController : Controller
    {
        // GET /Cuenta/IniciarSesion?urlRetorno=/Home/Index
        //
        // Ojo: no hace falta pasar por acá para loguearse. Si alguien entra a
        // cualquier pantalla sin sesión, la política de respaldo lo manda solo
        // a Microsoft. Esta acción es para cuando el usuario toca el enlace
        // "Iniciar sesión" del menú.
        //
        // Lleva [AllowAnonymous] porque, justamente, la usa alguien que todavía
        // no inició sesión. Si estuviera cerrada, nunca podría llegar.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult IniciarSesion(string? urlRetorno = null)
        {
            // urlRetorno es "a qué pantalla volver después del login".
            //
            // Solo aceptamos direcciones de nuestra propia app. Si no, alguien
            // podría mandar un enlace como
            //   /Cuenta/IniciarSesion?urlRetorno=https://sitio-falso.com
            // y usar nuestro login real para terminar en una página trucha.
            // A eso se le llama "open redirect".
            //
            // Si la dirección no es nuestra (o no viene), volvemos al inicio.
            // Url.Content("~/") arma la dirección del inicio de la app.
            var destino = Url.IsLocalUrl(urlRetorno) ? urlRetorno! : Url.Content("~/");

            // Si ya tiene sesión, no tiene sentido mandarlo a Microsoft otra vez.
            // LocalRedirect además vuelve a comprobar que la dirección sea nuestra.
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(destino);
            }

            // Challenge significa "pedile al usuario que se identifique".
            // Le decimos que use el esquema OpenIdConnect, o sea, Microsoft Entra ID.
            //
            // Esto devuelve una redirección al login de Microsoft (usuario,
            // contraseña y MFA). Cuando el usuario termina, Microsoft lo devuelve
            // a /signin-oidc, la librería crea la cookie de sesión y por último
            // lo manda a RedirectUri.
            var propiedades = new AuthenticationProperties { RedirectUri = destino };

            return Challenge(propiedades, OpenIdConnectDefaults.AuthenticationScheme);
        }

        // POST /Cuenta/CerrarSesion
        //
        // ¿Por qué POST y no un enlace común (GET)? Porque con GET cualquier otra
        // página podría cerrarte la sesión sin que te des cuenta, por ejemplo
        // poniendo <img src="https://eop/Cuenta/CerrarSesion">. Ese truco se
        // llama CSRF.
        //
        // [ValidateAntiForgeryToken] completa la protección: el formulario del
        // menú manda un código secreto (el "token antifalsificación") que solo
        // conoce nuestra propia página. Si el pedido llega sin ese código,
        // ASP.NET Core lo rechaza antes de entrar al método.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CerrarSesion()
        {
            // Cuando termina todo, el usuario cae en la pantalla "Sesión cerrada".
            var propiedades = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(SesionCerrada), "Cuenta")
            };

            // Acordate que hay DOS sesiones, y por eso pasamos dos esquemas:
            //
            // - Cookies: borra NUESTRA cookie. Desde ese momento la app ya no
            //   reconoce al usuario.
            // - OpenIdConnect: le pide a Microsoft que cierre SU sesión también.
            //   Si no lo hiciéramos, al tocar "Iniciar sesión" Microsoft te
            //   reconocería y te dejaría entrar sin pedirte nada, y parecería
            //   que el cierre de sesión no funcionó.
            //
            // El recorrido es: borramos la cookie, vamos a Microsoft, Microsoft
            // cierra su sesión y nos devuelve a /signout-callback-oidc (lo atiende
            // la librería, igual que /signin-oidc), y de ahí pasamos a RedirectUri.
            return SignOut(
                propiedades,
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        // GET /Cuenta/SesionCerrada
        //
        // Confirma que la sesión terminó. Es pública ([AllowAnonymous]) porque
        // la ve alguien que, justamente, acaba de salir.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SesionCerrada()
        {
            return View();
        }

        // GET /Cuenta/ErrorInicioSesion
        //
        // Acá llega el usuario cuando el login falló del lado de Microsoft
        // (canceló, no completó el MFA, etc.). Lo manda ManejadorFallosInicioSesion.
        //
        // Es pública porque el usuario, justamente, no pudo iniciar sesión.
        // El mensaje es genérico a propósito: el detalle técnico queda en el log,
        // no en la pantalla.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ErrorInicioSesion()
        {
            return View();
        }
    }
}
