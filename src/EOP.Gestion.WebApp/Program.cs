
using EOP.Gestion.Infrastructure;
using EOP.Gestion.WebApp.Autorizacion;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using EOP.Gestion.Core.CasosDeUso.Clientes;
using EOP.Gestion.Core.CasosDeUso.Proveedores;
using EOP.Gestion.Core.CasosDeUso.Usuarios;
using System.Security.Claims;
using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;

using EOP.Gestion.Infrastructure.Consultas.Usuarios;


var builder = WebApplication.CreateBuilder(args);


// ================================
// REGISTRO DE SERVICIOS
// ================================

// Activamos MVC: cada pedido lo atiende un Controller, que devuelve una vista Razor.
// Por ejemplo, /Home/Index lo atiende HomeController y muestra Views/Home/Index.cshtml.
builder.Services.AddControllersWithViews();

// Sumamos lo de la capa Infrastructure: la base de datos (EopDbContext con EF Core)
// y los repositorios. Le pasamos la configuración porque de ahí saca la cadena de
// conexión (appsettings, variables de entorno o User Secrets).
builder.Services.AgregarInfraestructura(builder.Configuration);


//CASO DE USO

// Registramos el caso de uso que valida los datos del cliente.
builder.Services.AddScoped<ValidarDatosCliente>();


// Registramos el caso de uso que valida y guarda
// los nuevos clientes en la base de datos.
builder.Services.AddScoped<RegistrarCliente>();

// Registramos el reloj del sistema. Los casos de uso piden la hora a este reloj
// (y no a DateTime.UtcNow) para que las pruebas puedan fijar la fecha que quieran.
builder.Services.AddSingleton(TimeProvider.System);

// Registramos el caso de uso encargado de validar
// y guardar los presupuestos nuevos.
builder.Services.AddScoped<RegistrarPresupuesto>();
builder.Services.AddScoped<AgregarConceptoManual>();
builder.Services.AddScoped<SeleccionarMaterialDeConcepto>();

// Registramos el caso de uso que valida los datos del proveedor.
builder.Services.AddScoped<ValidarDatosProveedor>();

// Registramos el caso de uso que valida y guarda
// los nuevos proveedores con sus cuentas bancarias.
builder.Services.AddScoped<RegistrarProveedor>();

// Registramos el caso de uso que valida y guarda
// los materiales con su primera relación material-proveedor.
builder.Services.AddScoped<RegistrarMaterial>();

// Registramos el caso de uso que agrega un proveedor
// a un material que ya existe en el catálogo.
builder.Services.AddScoped<AgregarProveedorAMaterial>();

// Registramos el caso de uso que modifica un material existente
// y los precios de sus relaciones con proveedores.
builder.Services.AddScoped<EditarMaterial>();

// Registramos el caso de uso para identificar o vincular usuarios con Entra ID.
builder.Services.AddScoped<IdentificarOVincularUsuario>();

// verifica si un usuario sigue habilitado en EOP cuando utiliza una sesión que ya estaba iniciada.
builder.Services.AddScoped<VerificarUsuarioHabilitado>();

// Registramos el caso de uso con el que el Administrador le cambia el rol a un usuario.
builder.Services.AddScoped<ModificarRolUsuario>();

// Registramos el caso de uso con el que el Administrador activa o inactiva a un usuario.
builder.Services.AddScoped<CambiarEstadoUsuario>();

// Registramos el caso de uso con el que el Administrador da de alta un usuario.
builder.Services.AddScoped<RegistrarUsuario>();

// Registramos la consulta que obtiene los proveedores
// existentes para mostrarlos en el listado.
builder.Services.AddScoped<ConsultarProveedores>();


// Registramos la consulta que obtiene todos los usuarios
// para mostrarlos en el listado administrativo.
builder.Services.AddScoped<ConsultarUsuarios>();


// Registramos la consulta que obtiene los presupuestos
// existentes para mostrarlos en el listado.
builder.Services.AddScoped<ConsultarPresupuestos>();

// Registramos la consulta que obtiene la información
// de un presupuesto seleccionado.
builder.Services.AddScoped<ConsultarPresupuestoDetalle>();

// ================================
// AUTENTICACIÓN CON MICROSOFT ENTRA ID
// ================================

// AddAuthentication() registra el sistema de autenticación
// que utilizará ASP.NET Core para identificar a los usuarios.
//
// OpenIdConnectDefaults.AuthenticationScheme indica que
// utilizaremos OpenID Connect como esquema de autenticación
// para iniciar sesión mediante un proveedor externo.
//
// En nuestro caso, ese proveedor será Microsoft Entra ID.
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"));

// Configuramos cómo nuestra aplicación solicita
// la autenticación a Microsoft Entra ID.
builder.Services.PostConfigure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
    {
        // Solicitamos un código de autorización en lugar
        // de recibir directamente un token de identidad.
        //
        // Microsoft devolverá un código que nuestra
        // aplicación intercambiará de forma segura
        // para completar el proceso de autenticación.
        options.ResponseType = "code";

        // Habilitamos PKCE, un mecanismo de seguridad
        // que protege el intercambio del código de
        // autorización frente a determinados ataques.
        options.UsePkce = true;


        //Un evento es una oportunidad que nos da un programa o una biblioteca para ejecutar nuestro propio código cuando sucede algo determinado
        //Aca estaruamos diciendo que :
        //ASP.NET Core, cuando recibas el ticket de autenticación procedente de Microsoft, ejecutá también este código que escribimos nosotros.

        /*Juan ingresó sus credenciales en Microsoft.
        Microsoft completó la autenticación.
        Microsoft devolvió la información de Juan a nuestra aplicación.
        ASP.NET Core recibió el ticket de autenticación y está por ejecutar OnTicketReceived.Estamos acá.*/

        var eventoAnteriorTicket = options.Events.OnTicketReceived;
        //Guardamos una referencia a una fución que ya estaba configurada para ejecutarse cuando se recibe el ticket de autenticación. 
        //Porque nosotros despues le asignamos nuestra propia función, pero queremos que la función original se siga ejecutando antes de la nuestra.

        //datosDelInicioDeSesion es un objeto que ASP.NET Core nos pasa con información sobre la autenticación que acaba de ocurrir.
        //Contiene datos como la identidad del usuario, el token recibido y otros detalles relevantes.
        options.Events.OnTicketReceived = async datosDelInicioDeSesion =>
        {
            //Creamos nuestra propia funcion que ASP.NET Core ejecutará cuando reciba el ticket de autenticación de Microsoft.

            // Conservamos el comportamiento que ya configuró Microsoft.
            await eventoAnteriorTicket(datosDelInicioDeSesion);

            if (datosDelInicioDeSesion.Result != null)
            {
                return;
            }

            // Con este registrador dejamos constancia de los accesos rechazados (RNF-12).
            // Nunca registramos correos, Object IDs, tokens ni mensajes de excepciones:
            // solo el motivo general y el Id de la solicitud, que sirve para encontrar
            // el resto de los logs de ese mismo pedido.
            var registradorDeAccesos = datosDelInicioDeSesion.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("VinculacionUsuarios");

            string idSolicitud = datosDelInicioDeSesion.HttpContext.TraceIdentifier;

            // Corta el inicio de sesión: manda al usuario a la pantalla de error y le
            // avisa a ASP.NET Core que ya respondimos nosotros (HandleResponse).
            // Así ASP.NET Core NO crea la cookie de sesión.
            void RechazarInicioDeSesion()
            {
                datosDelInicioDeSesion.Response.Redirect(
                    datosDelInicioDeSesion.Request.PathBase +
                    ManejadorFallosInicioSesion.RutaPantallaError);

                datosDelInicioDeSesion.HandleResponse();
            }

            //Traemos los datos del usuario autenticado por Microsoft Entra ID (identificador externo, correo)
            var identidad = datosDelInicioDeSesion.Principal;

            string? entraObjectId = identidad?.GetObjectId();

            // Sin identidad o sin Object ID no tenemos forma de saber quién es.
            if (identidad == null || string.IsNullOrWhiteSpace(entraObjectId))
            {
                registradorDeAccesos.LogWarning(
                    "Inicio de sesión rechazado: Microsoft Entra ID no envió una identidad válida. Id de solicitud: {IdSolicitud}",
                    idSolicitud);

                RechazarInicioDeSesion();
                return;
            }

            string? correoCorporativo = identidad.FindFirst("preferred_username")?.Value ?? identidad.FindFirst(ClaimTypes.Email)?.Value;

            // Buscamos al usuario de EOP o intentamos su primera vinculación.
            try
            {
                // Obtenemos nuestro caso de uso mediante inyección de dependencias.
                // Va dentro del try: si fallara al crearse, también cortamos el login.
                var IdentificarOVincularUsuario = datosDelInicioDeSesion.HttpContext.RequestServices.GetRequiredService<IdentificarOVincularUsuario>();

                var usuarioEop = await IdentificarOVincularUsuario
                    .ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(
                        entraObjectId,
                        correoCorporativo,
                        datosDelInicioDeSesion.HttpContext.RequestAborted);

                if (usuarioEop == null)
                {
                    // Usamos el mismo mensaje si no existe, si está inactivo o si ya está
                    // vinculado a otra cuenta: el log no necesita más detalle que este.
                    registradorDeAccesos.LogWarning(
                        "Inicio de sesión rechazado: no se encontró un usuario habilitado de EOP. Id de solicitud: {IdSolicitud}",
                        idSolicitud);

                    RechazarInicioDeSesion();
                }
            }
            catch (OperationCanceledException) when (datosDelInicioDeSesion.HttpContext.RequestAborted.IsCancellationRequested)
            {
                // El usuario cerró la pestaña o se cortó la conexión a mitad del login.
                // No es una falla de la base de datos, así que no lo registramos como error.
                // Igual cortamos el login: nunca creamos una sesión sin haber identificado al usuario.
                registradorDeAccesos.LogInformation(
                    "Inicio de sesión interrumpido: la solicitud fue cancelada antes de identificar al usuario. Id de solicitud: {IdSolicitud}",
                    idSolicitud);

                RechazarInicioDeSesion();
            }
            catch (Exception errorAlIdentificarUsuario)
            {
                // Por ejemplo: SQL Server no responde. Registramos solo el tipo de error,
                // no su mensaje, porque puede traer datos personales (SQL Server, por
                // ejemplo, incluye el valor repetido cuando se viola un índice único).
                registradorDeAccesos.LogError(
                    "No se pudo identificar al usuario de EOP. Tipo de error: {TipoError}. Id de solicitud: {IdSolicitud}",
                    errorAlIdentificarUsuario.GetType().Name,
                    idSolicitud);

                // No permitimos continuar el inicio de sesión.
                RechazarInicioDeSesion();
            }
        };


        // Los "eventos" son puntos donde la librería nos deja meter código
        // propio en distintos momentos del login.
        //
        // OnRemoteFailure se dispara cuando el login falla del lado de
        // Microsoft: el usuario canceló, no completó el MFA, etc.
        // En ese caso, en vez de mostrar un error genérico, registramos
        // lo que pasó y lo mandamos a una pantalla que se lo explica.
        //
        // Guardamos el evento que ya hubiera (por si la librería tenía algo
        // configurado) y lo ejecutamos primero, para no pisarlo.
        var eventoAnterior = options.Events.OnRemoteFailure;

        options.Events.OnRemoteFailure = async contexto =>
        {
            await eventoAnterior(contexto);

            // Si el evento anterior ya respondió, no hacemos nada más.
            if (contexto.Result?.Handled == true)
            {
                return;
            }

            var logger = contexto.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(ManejadorFallosInicioSesion));

            await ManejadorFallosInicioSesion.ManejarAsync(contexto, logger);
        };
    });


// ================================
// COOKIE DE SESIÓN
// ================================

// Cuando Entra ID nos confirma quién es el usuario, la app guarda esa
// identidad en una cookie. Es como la pulsera de un festival: el navegador
// la muestra en cada clic y así el usuario no tiene que loguearse de nuevo
// en cada pantalla.
//
// La cookie la crea Microsoft.Identity.Web (lo configuramos en la T08).
// Acá solamente ajustamos cómo se guarda, para que sea segura.
builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        // HttpOnly: el JavaScript de la página no puede leer la cookie.
        // Si algún día se llega a colar un script malicioso, no la puede robar.
        options.Cookie.HttpOnly = true;

        // Always: el navegador solo manda la cookie por HTTPS (RNF-9).
        // Ojo: por eso, para probar el login, hay que levantar la app con el
        // perfil "https" y no con el "http".
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        // A dónde va un usuario con sesión cuando su rol no le permite una acción.
        // Si no lo indicamos, ASP.NET Core usa /Account/AccessDenied, que en EOP
        // no existe, y el usuario vería un error 404.
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";

        // No cambiamos cuánto dura la sesión. Los requerimientos no lo dicen,
        // así que preferimos no inventar un valor y dejar el de ASP.NET Core.


        // Conservamos el evento de validación que ya estaba configurado.
        // No queremos reemplazar las verificaciones anteriores del framework.
        var validacionDeCookieConfiguradaAnteriormente = options.Events.OnValidatePrincipal;

        options.Events.OnValidatePrincipal = async contextoValidacionCookie =>
        {
            // Primero ejecutamos la validación que ya existía.
            await validacionDeCookieConfiguradaAnteriormente(contextoValidacionCookie);

            // Si la validación anterior rechazó la identidad,
            // no necesitamos continuar.
            if (contextoValidacionCookie.Principal == null)
            {
                return;
            }

            // Comprobamos si el usuario sigue habilitado en EOP.
            // Si no lo está, el manejador rechazará su sesión.
            await ManejadorDeValidacionDeAccesoConSesionIniciada.VerificarQueUsuarioSigueHabilitadoORechazarSesionAsync(contextoValidacionCookie);
        };
    });


// ================================
// TODO CERRADO POR DEFECTO
// ================================

// La "política de respaldo" (FallbackPolicy) es la regla que se aplica a
// cualquier pantalla que no diga nada sobre permisos.
//
// Le decimos que exija un usuario autenticado. O sea: por defecto, toda la
// app está cerrada, y solo abrimos a mano (con [AllowAnonymous]) las pocas
// pantallas que tienen que verse sin sesión, como la página de error.
//
// ¿Por qué así y no al revés? Porque si mañana alguien crea un Controller
// nuevo y se olvida de protegerlo, igual queda protegido. Es más seguro
// equivocarse "cerrando" que "abriendo". Y como esto lo controla el
// servidor, no alcanza con esconder un botón para saltearlo (RNF-4).
//
// Si alguien entra sin sesión, ASP.NET Core lo manda solo a Microsoft
// para que inicie sesión, y después lo devuelve a la página que quería ver.
//
// Esto solo pide que el usuario haya iniciado sesión con Entra ID.
// Que además exista en EOP y siga Activo lo revisa la cookie en cada
// pedido (OnValidatePrincipal, más arriba).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});


// ================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// ================================

// Hasta este momento estuvimos registrando servicios
// y preparando la configuración de nuestra aplicación.
//
// Build() construye la aplicación utilizando esos servicios
// y deja preparado el objeto app para configurar cómo
// se procesarán las solicitudes HTTP.
var app = builder.Build();


// ================================
// CONFIGURACIÓN DEL ENTORNO
// ================================

// Comprobamos si la aplicación NO está ejecutándose
// en el entorno de desarrollo.
//
// Esto permite aplicar ciertas configuraciones de seguridad
// y manejo de errores cuando la aplicación esté en producción.
if (!app.Environment.IsDevelopment())
{
    // Si ocurre un error inesperado, ASP.NET Core utiliza
    // la ruta /Home/Error para mostrar una página de error.
    //
    // Evitamos mostrar detalles técnicos internos al usuario,
    // como excepciones o información sensible.
    app.UseExceptionHandler("/Home/Error");

    // HSTS indica al navegador que debe utilizar HTTPS
    // para comunicarse con nuestra aplicación durante
    // el período establecido por esta configuración.
    //
    // Ayuda a evitar conexiones HTTP inseguras.
    app.UseHsts();
}


// ================================
// CONFIGURACIÓN DE SOLICITUDES HTTP
// ================================

// Redirige las solicitudes HTTP hacia HTTPS.
//
// HTTPS cifra la comunicación entre el navegador y el servidor.
// Esto es especialmente importante porque nuestro sistema
// manejará información empresarial y sesiones de usuarios.
app.UseHttpsRedirection();


// Habilita el sistema de enrutamiento de ASP.NET Core.
//
// El enrutamiento analiza la URL solicitada y determina
// qué Controller y qué Action deben atender la petición.
//
// Por ejemplo:
// /Home/Index
//
// Controller: HomeController
// Action: Index
app.UseRouting();


// ================================
// AUTENTICACIÓN Y AUTORIZACIÓN
// ================================

// Activa el middleware de autenticación.
//
// Un middleware es un componente que participa en el
// procesamiento de las solicitudes HTTP.
//
// UseAuthentication() comprueba si la solicitud contiene
// información de una sesión autenticada válida.
//
// Si existe, ASP.NET Core identifica al usuario y
// establece su identidad en HttpContext.User.
//
// IMPORTANTE:
// Este middleware debe ejecutarse antes de UseAuthorization(),
// porque primero necesitamos saber quién es el usuario.
app.UseAuthentication();


// Activa el middleware de autorización.
//
// La autorización determina si el usuario identificado
// tiene permiso para acceder a una funcionalidad o recurso.
//
// Acá es donde se aplica la política de respaldo que definimos
// más arriba: si el usuario no está autenticado, lo manda a
// iniciar sesión con Microsoft.
//
// También acá se aplican los permisos según el rol (Administrador,
// Supervisor, Operador) de las acciones con [Authorize(Roles = "...")].
// Se comparan con el rol actual del usuario, que OnValidatePrincipal
// (más arriba) carga desde la tabla Usuarios en cada pedido. Si su rol
// no alcanza, la acción no se ejecuta y va a la pantalla de acceso denegado.
app.UseAuthorization();

// Los archivos estáticos (CSS de Bootstrap, JavaScript, imágenes)
// se pueden descargar sin sesión. Si no, la política de respaldo
// también los bloquearía y las pantallas públicas (por ejemplo,
// la de error) se verían sin estilos.
//
// No hay nada privado acá: es solo lo que está en wwwroot.
// Los documentos de los proyectos van a Blob Storage, no a esta carpeta.
app.MapStaticAssets().AllowAnonymous();


// ================================
// CONFIGURACIÓN DE RUTAS MVC
// ================================

// Registramos la ruta convencional de nuestra aplicación MVC.
//
// Esta ruta permite que ASP.NET Core determine qué Controller
// y qué Action ejecutar según la dirección solicitada.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


// Inicia la aplicación y la deja preparada
// para recibir solicitudes desde los navegadores.
app.Run();
