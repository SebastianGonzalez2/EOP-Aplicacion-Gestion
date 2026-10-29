
using EOP.Gestion.Infrastructure;
using EOP.Gestion.WebApp.Autorizacion;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using EOP.Gestion.Core.CasosDeUso.Clientes;

var builder = WebApplication.CreateBuilder(args);


// ================================
// REGISTRO DE SERVICIOS
// ================================

// Registramos los servicios necesarios para utilizar ASP.NET Core MVC.
// Esto permite trabajar con Controllers y Views (vistas Razor).
// Por ejemplo, cuando un usuario accede a una URL,
// un Controller procesa la solicitud y devuelve una View.
builder.Services.AddControllersWithViews();


// Registramos los servicios de nuestra capa Infrastructure.
// AgregarInfraestructura() es un método que creamos nosotros
// para centralizar las configuraciones técnicas del proyecto.
//
// Actualmente permite configurar servicios como EopDbContext,
// que utiliza Entity Framework Core para comunicarse con SQL Server.
//
// builder.Configuration contiene la configuración de la aplicación,
// incluyendo appsettings.json, variables de entorno y User Secrets.
builder.Services.AgregarInfraestructura(builder.Configuration);

// Registramos el caso de uso que valida los datos del cliente.
builder.Services.AddScoped<ValidarDatosCliente>();


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

        // No cambiamos cuánto dura la sesión. Los requerimientos no lo dicen,
        // así que preferimos no inventar un valor y dejar el de ASP.NET Core.
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
// Esto solo verifica que el usuario esté autenticado en Entra ID.
// Chequear que exista en EOP y que esté Activo queda para la T11.
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
// Los permisos según el rol (Administrador, Supervisor, Operador)
// se agregan en tareas posteriores.
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
