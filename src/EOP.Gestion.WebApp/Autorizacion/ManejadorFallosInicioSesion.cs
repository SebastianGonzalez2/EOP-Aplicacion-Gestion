using Microsoft.AspNetCore.Authentication;

namespace EOP.Gestion.WebApp.Autorizacion
{
    // ¿Qué pasa si el inicio de sesión sale mal del lado de Microsoft?
    // Por ejemplo: el usuario cancela, no completa el MFA, o Entra ID devuelve un error.
    //
    // Si no hacemos nada, la librería lanza una excepción y el usuario ve la
    // página de error genérica, como si la app se hubiera roto. Esta clase hace
    // dos cosas más prolijas:
    //   1. Deja registrado qué pasó, para poder investigarlo (RNF-12).
    //   2. Manda al usuario a una pantalla que le explica que el login no se
    //      completó y le deja intentarlo de nuevo.
    //
    // La pusimos en una clase aparte, y no directamente en Program.cs, para
    // poder probarla con una prueba unitaria.
    public static class ManejadorFallosInicioSesion
    {
        public const string RutaPantallaError = "/Cuenta/ErrorInicioSesion";

        public static Task ManejarAsync(RemoteFailureContext contexto, ILogger logger)
        {
            // Cuando Entra ID rechaza el login, manda un código corto que dice el
            // motivo, por ejemplo "access_denied". La librería lo guarda en
            // Failure.Data["error"].
            //
            // Registramos solo ese código y el tipo de error. A propósito NO
            // registramos el mensaje completo ni la descripción: pueden traer
            // datos que no queremos en los logs (RNF-12 pide no guardar tokens
            // ni datos personales).
            var codigoError = contexto.Failure?.Data["error"] as string ?? "sin código";

            logger.LogWarning(
                "No se pudo completar el inicio de sesión con Microsoft Entra ID. " +
                "Tipo de error: {TipoError}. Código: {CodigoError}. Id de solicitud: {IdSolicitud}",
                contexto.Failure?.GetType().FullName,
                codigoError,
                contexto.HttpContext.TraceIdentifier);

            // PathBase es el "prefijo" de la app si algún día la publicamos dentro
            // de una subcarpeta (ej: midominio.com/eop). Así la redirección sigue
            // funcionando.
            contexto.Response.Redirect(contexto.Request.PathBase + RutaPantallaError);

            // HandleResponse le avisa a la librería: "tranquila, ya respondimos
            // nosotros". Sin esto, igual lanzaría la excepción.
            contexto.HandleResponse();

            return Task.CompletedTask;
        }
    }
}
