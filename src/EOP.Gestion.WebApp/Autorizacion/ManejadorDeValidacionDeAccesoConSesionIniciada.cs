
using EOP.Gestion.Core.CasosDeUso.Usuarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace EOP.Gestion.WebApp.Autorizacion;

public static class ManejadorDeValidacionDeAccesoConSesionIniciada
{
    public static async Task VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(CookieValidatePrincipalContext contextoValidacionCookie){
        
        // Obtenemos un registrador para dejar constancia de los accesos
        // rechazados sin almacenar información personal de los usuarios.
        var registradorDeValidacionSesiones = contextoValidacionCookie.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("ValidacionSesiones");

        string identificadorSolicitud = contextoValidacionCookie.HttpContext.TraceIdentifier;
        try
        {
            // Recuperamos el identificador de Microsoft que viene
            // asociado a la sesión que estamos comprobando.
            string? identificadorExternoDeMicrosoft = contextoValidacionCookie.Principal?.GetObjectId();

            // Obtenemos el caso de uso encargado de comprobar si el
            // usuario todavía está habilitado para acceder a EOP.
            var verificadorDeUsuarioHabilitado =contextoValidacionCookie.HttpContext.RequestServices
                    .GetRequiredService<VerificarUsuarioHabilitado>();

            // Obtenemos el usuario activo desde nuestra base de datos.
            var usuario = await verificadorDeUsuarioHabilitado.ObtenerUsuarioHabilitadoAsync(identificadorExternoDeMicrosoft,
                                                        contextoValidacionCookie.HttpContext.RequestAborted);

            // Si el usuario existe y su identidad es válida:
            if (usuario != null && contextoValidacionCookie.Principal?.Identity is ClaimsIdentity identidad){

                // Quitamos cualquier rol anterior de la sesión.
                foreach (var otraIdentidad in contextoValidacionCookie.Principal.Identities)
                {
                    foreach (var rolAnterior in otraIdentidad.FindAll(otraIdentidad.RoleClaimType).ToList())
                    {
                        otraIdentidad.RemoveClaim(rolAnterior);
                    }
                }

                // Agregamos el rol ACTUAL guardado en EOP.
                identidad.AddClaim(
                    new Claim(identidad.RoleClaimType, usuario.Rol.ToString()));

                // El usuario sigue habilitado.
                // Permitimos que conserve su sesión.
                return;
            }

            registradorDeValidacionSesiones.LogWarning(
                "Sesión rechazada: usuario no habilitado en EOP. Id de solicitud: {IdSolicitud}",
                identificadorSolicitud);
        }
        catch (OperationCanceledException)
            when (contextoValidacionCookie.HttpContext.RequestAborted.IsCancellationRequested)
        {
            // El navegador canceló la solicitud mientras se comprobaba
            // el estado del usuario. No registramos un error de base de datos.
            contextoValidacionCookie.RejectPrincipal();
            return;
        }
        catch (Exception errorAlVerificarUsuario)
        {
            // Si no podemos comprobar el estado del usuario, no permitimos
            // que continúe accediendo. No registramos datos sensibles.
            registradorDeValidacionSesiones.LogError(
                "Error al verificar si el usuario sigue habilitado. Tipo: {TipoError}. Id de solicitud: {IdSolicitud}",
                errorAlVerificarUsuario.GetType().Name,
                identificadorSolicitud);
        }

        // Rechazamos la identidad de esta sesión.
        contextoValidacionCookie.RejectPrincipal();

        // Eliminamos la cookie local de autenticación de EOP.
        await contextoValidacionCookie.HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
