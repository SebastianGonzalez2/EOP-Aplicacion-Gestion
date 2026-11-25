using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.WebApp.ViewModels.Cuenta;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace EOP.Gestion.WebApp.ViewComponents;

// S3-T15 (US-003, US-042): arma el diálogo "¿Cerrar sesión con cambios sin guardar?".
//
// Un ViewComponent es como una vista parcial que además puede ejecutar código propio (acá,
// consultar los borradores del usuario). Lo invoca el _Layout en TODAS las pantallas con
// sesión, porque el usuario puede intentar cerrar sesión desde cualquier módulo.
//
// - Si el usuario no tiene cambios pendientes, no dibuja nada y el botón "Cerrar sesión"
//   funciona como siempre.
// - Si tiene, dibuja el diálogo (oculto). El script cambios-pendientes.js lo abre al tocar
//   "Cerrar sesión" y también lo usa como señal para la advertencia nativa del navegador.
//
// Esto es solo comodidad: el control real está en CuentaController.CerrarSesion, que vuelve a
// revisar los pendientes en el servidor antes de cerrar.
public class AdvertenciaCierreSesionViewComponent : ViewComponent
{
    private readonly IRepositorioUsuarios _usuarios;
    private readonly ConsultarCambiosPendientesDelUsuario _consultarPendientes;
    private readonly ILogger<AdvertenciaCierreSesionViewComponent> _logger;

    public AdvertenciaCierreSesionViewComponent(
        IRepositorioUsuarios usuarios,
        ConsultarCambiosPendientesDelUsuario consultarPendientes,
        ILogger<AdvertenciaCierreSesionViewComponent> logger)
    {
        _usuarios = usuarios;
        _consultarPendientes = consultarPendientes;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal.Identity == null || !UserClaimsPrincipal.Identity.IsAuthenticated)
        {
            return Content(string.Empty);
        }

        CancellationToken cancellationToken = HttpContext.RequestAborted;

        try
        {
            string? identidad = UserClaimsPrincipal.GetObjectId();
            if (string.IsNullOrWhiteSpace(identidad))
            {
                return Content(string.Empty);
            }

            Usuario? usuario = await _usuarios.ObtenerPorIdentidadExternaAsync(identidad, cancellationToken);
            if (usuario == null)
            {
                return Content(string.Empty);
            }

            List<CambioPendienteDto> pendientes = await _consultarPendientes.EjecutarAsync(usuario.Id, cancellationToken);
            if (pendientes.Count == 0)
            {
                return Content(string.Empty);
            }

            var advertencia = new CambiosPendientesCierreSesionViewModel
            {
                Pendientes = pendientes,
                EnModal = true
            };

            return View(advertencia);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Content(string.Empty);
        }
        catch (Exception excepcion)
        {
            // Si algo falla acá no rompemos la pantalla entera (RNF-13): sin el diálogo, el
            // cierre de sesión igual pasa por el control del servidor, que no cierra si hay
            // cambios pendientes y lleva al usuario a la pantalla de decisión.
            _logger.LogError(
                "No se pudo preparar la advertencia de cambios pendientes. Tipo de error: {TipoError}. Id de solicitud: {IdSolicitud}",
                excepcion.GetType().Name,
                HttpContext.TraceIdentifier);

            return Content(string.Empty);
        }
    }
}
