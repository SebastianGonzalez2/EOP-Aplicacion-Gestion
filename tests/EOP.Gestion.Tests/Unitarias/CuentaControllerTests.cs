using System.Reflection;
using System.Security.Claims;
using EOP.Gestion.WebApp.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace EOP.Gestion.Tests.Unitarias;

// Estas pruebas no hablan con Microsoft: solo revisan qué le pide el Controller
// a ASP.NET Core. Por ejemplo, que pida el login con Entra ID y que no acepte
// volver a una dirección de otro sitio.
public class CuentaControllerTests
{
    [Fact]
    public void IniciarSesion_SinSesion_PideAutenticacionConEntraId()
    {
        var controller = CrearController(UsuarioAnonimo());

        var resultado = Assert.IsType<ChallengeResult>(controller.IniciarSesion("/Home/Privacy"));

        Assert.Equal([OpenIdConnectDefaults.AuthenticationScheme], resultado.AuthenticationSchemes);
        Assert.Equal("/Home/Privacy", resultado.Properties?.RedirectUri);
    }

    [Theory]
    [InlineData("https://sitio-falso.com")]
    [InlineData("//sitio-falso.com")]
    [InlineData("/\\sitio-falso.com")]
    [InlineData("https:sitio-falso.com")]
    [InlineData(null)]
    public void IniciarSesion_ConUrlRetornoExternaOVacia_VuelveAlInicio(string? urlRetorno)
    {
        var controller = CrearController(UsuarioAnonimo());

        var resultado = Assert.IsType<ChallengeResult>(controller.IniciarSesion(urlRetorno));

        Assert.Equal("/", resultado.Properties?.RedirectUri);
    }

    [Fact]
    public void IniciarSesion_ConSesionActiva_RedirigeSinPedirLoginDeNuevo()
    {
        var controller = CrearController(UsuarioAutenticado());

        var resultado = Assert.IsType<LocalRedirectResult>(controller.IniciarSesion("/Home/Privacy"));

        Assert.Equal("/Home/Privacy", resultado.Url);
    }

    [Fact]
    public void CerrarSesion_CierraLaSesionLocalYLaDeEntraId()
    {
        var controller = CrearController(UsuarioAutenticado());
        controller.Url = new UrlHelperDePrueba();

        var resultado = Assert.IsType<SignOutResult>(controller.CerrarSesion());

        Assert.Equal(
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme],
            resultado.AuthenticationSchemes);
        Assert.Equal("/Cuenta/SesionCerrada", resultado.Properties?.RedirectUri);
    }

    // Si CerrarSesion aceptara GET o no pidiera el token, otra página podría
    // cerrarle la sesión al usuario (CSRF).
    [Fact]
    public void CerrarSesion_SoloAceptaPostConTokenAntifalsificacion()
    {
        var metodo = typeof(CuentaController).GetMethod(nameof(CuentaController.CerrarSesion))!;

        Assert.NotNull(metodo.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(metodo.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Null(metodo.GetCustomAttribute<HttpGetAttribute>());
    }

    // Si alguien saca el [AllowAnonymous] por error, el usuario sin sesión
    // ya no podría llegar a estas acciones. Esta prueba lo avisa.
    [Theory]
    [InlineData(typeof(CuentaController), nameof(CuentaController.IniciarSesion))]
    [InlineData(typeof(CuentaController), nameof(CuentaController.SesionCerrada))]
    [InlineData(typeof(CuentaController), nameof(CuentaController.ErrorInicioSesion))]
    [InlineData(typeof(HomeController), nameof(HomeController.Error))]
    public void Accion_PermiteAccesoSinSesion(Type controller, string accion)
    {
        var metodo = controller.GetMethod(accion)!;

        Assert.NotNull(metodo.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    private static CuentaController CrearController(ClaimsPrincipal usuario)
    {
        var contexto = new DefaultHttpContext { User = usuario };
        var contextoAccion = new ActionContext(contexto, new RouteData(), new ControllerActionDescriptor());

        return new CuentaController
        {
            ControllerContext = new ControllerContext(contextoAccion),
            Url = new UrlHelper(contextoAccion)
        };
    }

    private static ClaimsPrincipal UsuarioAnonimo() => new(new ClaimsIdentity());

    // Una identidad con "tipo de autenticación" cuenta como autenticada.
    private static ClaimsPrincipal UsuarioAutenticado() =>
        new(new ClaimsIdentity([new Claim("name", "Usuario de Prueba")], "Prueba"));

    // Url.Action necesita las rutas de la app completa para armar direcciones.
    // En una prueba unitaria no las tenemos, así que lo reemplazamos por uno
    // que arma "/Controller/Accion" sin más.
    private sealed class UrlHelperDePrueba : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();

        public string? Action(UrlActionContext actionContext) =>
            $"/{actionContext.Controller}/{actionContext.Action}";

        public string Content(string? contentPath) => contentPath ?? string.Empty;

        public bool IsLocalUrl(string? url) => url?.StartsWith('/') == true && !url.StartsWith("//");

        public string? Link(string? routeName, object? values) => null;

        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }
}
