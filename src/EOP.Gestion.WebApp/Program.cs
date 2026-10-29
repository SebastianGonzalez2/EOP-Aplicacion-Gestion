
using EOP.Gestion.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
// Por ejemplo, posteriormente podremos restringir
// determinadas acciones según los permisos del usuario.
//
// IMPORTANTE:
// Esto no significa que todas las pantallas ya estén protegidas.
// Todavía debemos definir y aplicar las reglas de acceso
// correspondientes en las siguientes tareas.
app.UseAuthorization();

app.MapStaticAssets();


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
