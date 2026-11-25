using System.Text.Json;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.WebApp.ViewModels.Cuenta;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

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
        // S3-T15: valores posibles del botón elegido en la advertencia de cierre de sesión.
        public const string DecisionGuardar = "guardar";
        public const string DecisionDescartar = "descartar";

        private const string ClaveResultadosCierreSesion = "ResultadosCierreSesion";

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
        //
        // S3-T15 (US-003, US-042, RF-05, RF-69): antes de cerrar la sesión revisamos si el
        // usuario dejó cambios sin guardar en algún presupuesto, esté en la pantalla que esté.
        // - Sin cambios pendientes: se cierra la sesión como siempre.
        // - Con cambios y sin una decisión: no cerramos. Lo mandamos a la pantalla
        //   CambiosPendientes para que elija. Normalmente el usuario ya eligió en el diálogo
        //   del menú; este camino cubre el caso de que el navegador no ejecute JavaScript.
        // - decision = "guardar": guardamos todo y cerramos solo si todo salió bien.
        // - decision = "descartar": descartamos todo (ya lo confirmó dos veces) y cerramos.
        //
        // Las dependencias llegan con [FromServices] (directo a esta acción) porque solo las
        // usan estas dos acciones; el resto del Controller no las necesita.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesion(
            string? decision,
            List<BorradorEsperadoViewModel>? borradores,
            [FromServices] IRepositorioUsuarios usuarios,
            [FromServices] ResolverCambiosPendientesAlCerrarSesion resolverCambiosPendientes,
            [FromServices] ILogger<CuentaController> logger,
            CancellationToken cancellationToken)
        {
            Usuario? usuario = await ObtenerUsuarioAsync(usuarios, cancellationToken);

            // Los borradores se guardan con el id del usuario de EOP. Si no lo encontramos, no
            // puede tener cambios pendientes propios: cerramos la sesión normalmente.
            if (usuario == null)
            {
                return CerrarSesionEnEopYEntraId();
            }

            // Armamos la lista de lo que el usuario vio en la advertencia.
            var borradoresEsperados = new List<BorradorEsperadoDto>();
            if (borradores != null)
            {
                foreach (BorradorEsperadoViewModel borrador in borradores)
                {
                    borradoresEsperados.Add(new BorradorEsperadoDto(
                        borrador.PresupuestoId, borrador.BorradorId, borrador.Version));
                }
            }

            try
            {
                if (string.Equals(decision, DecisionGuardar, StringComparison.Ordinal))
                {
                    ResultadoCierreSesionConPendientes resultadoGuardado = await resolverCambiosPendientes.GuardarTodoAsync(
                        usuario, borradoresEsperados, cancellationToken);

                    return ContinuarCierreSesion(
                        resultadoGuardado,
                        "No se cerró la sesión porque algunos cambios no se pudieron guardar. Revisá el detalle de cada presupuesto.");
                }

                if (string.Equals(decision, DecisionDescartar, StringComparison.Ordinal))
                {
                    ResultadoCierreSesionConPendientes resultadoDescarte = await resolverCambiosPendientes.DescartarTodoAsync(
                        usuario, borradoresEsperados, cancellationToken);

                    return ContinuarCierreSesion(
                        resultadoDescarte,
                        "No se cerró la sesión porque algunos cambios no se pudieron descartar. Revisá el detalle de cada presupuesto.");
                }

                // Sin decisión: solo cerramos si no hay nada pendiente.
                bool puedeCerrar = await resolverCambiosPendientes.PrepararCierreSinPendientesAsync(
                    usuario.Id, cancellationToken);

                if (puedeCerrar)
                {
                    return CerrarSesionEnEopYEntraId();
                }

                return RedirectToAction(nameof(CambiosPendientes));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception excepcion)
            {
                // RNF-12: registramos el tipo de error, sin datos personales. La sesión sigue
                // abierta y los presupuestos que no se llegaron a guardar conservan sus cambios.
                logger.LogError(
                    "No se pudieron resolver los cambios pendientes al cerrar sesión. Tipo de error: {TipoError}. Id de solicitud: {IdSolicitud}",
                    excepcion.GetType().Name,
                    HttpContext.TraceIdentifier);

                TempData["MensajeError"] =
                    "No se cerró la sesión por un error inesperado. Los presupuestos de la lista conservan sus cambios sin guardar.";

                return RedirectToAction(nameof(CambiosPendientes));
            }
        }

        // GET /Cuenta/CambiosPendientes
        //
        // S3-T15: pantalla con la misma advertencia del diálogo de cierre de sesión. Muestra los
        // presupuestos que siguen con cambios sin guardar y, si hubo un intento anterior que no
        // terminó bien, el resultado de cada presupuesto. Queda cubierta por la política de
        // respaldo: solo la ve alguien con sesión iniciada.
        [HttpGet]
        public async Task<IActionResult> CambiosPendientes(
            bool confirmarDescarte,
            [FromServices] IRepositorioUsuarios usuarios,
            [FromServices] ConsultarCambiosPendientesDelUsuario consultarPendientes,
            CancellationToken cancellationToken)
        {
            Usuario? usuario = await ObtenerUsuarioAsync(usuarios, cancellationToken);
            if (usuario == null)
            {
                return Forbid();
            }

            List<CambioPendienteDto> pendientes = await consultarPendientes.EjecutarAsync(usuario.Id, cancellationToken);

            var resultados = new List<ResultadoPresupuestoAlCerrarSesion>();
            if (TempData[ClaveResultadosCierreSesion] is string resultadosJson)
            {
                List<ResultadoPresupuestoAlCerrarSesion>? resultadosLeidos =
                    JsonSerializer.Deserialize<List<ResultadoPresupuestoAlCerrarSesion>>(resultadosJson);

                if (resultadosLeidos != null)
                {
                    resultados = resultadosLeidos;
                }
            }

            var pantalla = new CambiosPendientesCierreSesionViewModel
            {
                Pendientes = pendientes,
                EnModal = false,
                ConfirmandoDescarte = confirmarDescarte && pendientes.Count > 0,
                Resultados = resultados
            };

            return View(pantalla);
        }

        // Decide qué hacer después de intentar guardar o descartar todo.
        private IActionResult ContinuarCierreSesion(ResultadoCierreSesionConPendientes resultado, string mensajeSiFallo)
        {
            if (resultado.PuedeCerrarSesion)
            {
                return CerrarSesionEnEopYEntraId();
            }

            // No cerramos: le mostramos al usuario qué pasó con cada presupuesto. Los resultados
            // viajan en TempData (como texto JSON) porque usamos Post/Redirect/Get.
            if (!string.IsNullOrWhiteSpace(resultado.MensajeGeneral))
            {
                TempData["MensajeError"] = resultado.MensajeGeneral;
            }
            else
            {
                TempData["MensajeError"] = mensajeSiFallo;
            }

            TempData[ClaveResultadosCierreSesion] = JsonSerializer.Serialize(resultado.Resultados);
            return RedirectToAction(nameof(CambiosPendientes));
        }

        private async Task<Usuario?> ObtenerUsuarioAsync(IRepositorioUsuarios usuarios, CancellationToken cancellationToken)
        {
            string? identidad = User.GetObjectId();
            if (string.IsNullOrWhiteSpace(identidad))
            {
                return null;
            }

            return await usuarios.ObtenerPorIdentidadExternaAsync(identidad, cancellationToken);
        }

        private IActionResult CerrarSesionEnEopYEntraId()
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
        // Acá llega el usuario cuando el login no se completó:
        // - falló del lado de Microsoft (canceló, no completó el MFA, etc.).
        //   Lo manda ManejadorFallosInicioSesion.
        // - EOP no lo reconoce como usuario habilitado, o falló la base de datos.
        //   Lo manda el evento OnTicketReceived de Program.cs.
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

        // GET /Cuenta/AccesoDenegado
        //
        // Acá lo manda ASP.NET Core cuando el usuario inició sesión, pero su rol
        // no le permite hacer lo que pidió (ver AccessDeniedPath en Program.cs).
        // Para ese momento la acción protegida ya fue frenada: esta pantalla
        // solo le explica lo que pasó.
        //
        // A diferencia de las otras pantallas de error, no lleva [AllowAnonymous]:
        // la ve alguien con sesión, así que queda cubierta por la política de respaldo.
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            // 403 es el código HTTP de "sé quién sos, pero no tenés permiso".
            Response.StatusCode = StatusCodes.Status403Forbidden;

            return View();
        }
    }
}
