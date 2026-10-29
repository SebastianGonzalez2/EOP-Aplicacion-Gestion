using EOP.Gestion.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EOP.Gestion.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // La página de error tiene que poder mostrarse siempre, incluso a alguien
        // sin sesión. Si no, un error durante el login terminaría en otra
        // redirección al login en lugar de mostrar el aviso.
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var idSolicitud = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var excepcion = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;

            if (excepcion is not null)
            {
                _logger.LogError(
                    "Error inesperado de tipo {TipoExcepcion}. Id de solicitud: {IdSolicitud}",
                    excepcion.GetType().FullName,
                    idSolicitud);
            }

            return View(new ErrorViewModel { RequestId = idSolicitud });
        }
    }
}
