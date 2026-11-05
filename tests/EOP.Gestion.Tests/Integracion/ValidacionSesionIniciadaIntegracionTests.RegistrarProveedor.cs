using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Integracion;

// Pruebas de S2-T12 y S2-T13: quién puede abrir y enviar el alta de proveedores, pasando por toda la app (cookie
// real, OnValidatePrincipal, [Authorize] y base con las tablas reales). Viven en esta clase
// porque usan la misma fábrica con sesión real que las pruebas de permisos de T13 y T14.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    private const string RutaRegistrarProveedor = "/Proveedores/Registrar";
    private const string TextoDelFormularioDeProveedor = "Datos del proveedor";

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    public async Task AdministradorYSupervisor_PuedenAbrirElAltaDeProveedores(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaRegistrarProveedor, cookieDeSesion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(TextoDelFormularioDeProveedor, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Operador_NoPuedeAbrirElAltaDeProveedoresNiPorUrlDirecta()
    {
        // US-009, "proteger alta frente a Operador". Pedimos la URL sin pasar por ningún
        // enlace, así que el único que lo puede frenar es el servidor (RNF-4).
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaRegistrarProveedor, cookieDeSesion);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        Assert.DoesNotContain(TextoDelFormularioDeProveedor, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PedidoSinSesion_NoPuedeAbrirElAltaDeProveedores()
    {
        using var fabrica = new FabricaConSesionReal();

        var respuesta = await CrearCliente(fabrica).GetAsync(RutaRegistrarProveedor);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task SupervisorQuePasaAOperadorConLaSesionAbierta_PierdeElAccesoAlAlta()
    {
        // El permiso sale del rol guardado HOY en la base, no del que tenía al iniciar sesión.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Supervisor);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var antes = await PedirConCookieAsync(fabrica, RutaRegistrarProveedor, cookieDeSesion);
        Assert.Equal(HttpStatusCode.OK, antes.StatusCode);

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Rol = RolUsuario.Operador);

        var despues = await PedirConCookieAsync(fabrica, RutaRegistrarProveedor, cookieDeSesion);
        Assert.Equal(HttpStatusCode.Redirect, despues.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", despues.Headers.Location?.AbsolutePath);
    }

    // ---------- El POST del alta (S2-T13) ----------

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    public async Task AdministradorYSupervisor_PuedenRegistrarUnProveedor(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        ProteccionAntifalsificacion proteccion = await ObtenerProteccionAntifalsificacionAsync(fabrica, RutaRegistrarProveedor, cookieDeSesion);

        var respuesta = await EnviarAltaDeProveedorAsync(fabrica, cookieDeSesion, proteccion);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RutaRegistrarProveedor, respuesta.Headers.Location?.OriginalString);
        await ComprobarCantidadesDeProveedoresAsync(fabrica, proveedoresEsperados: 1, cuentasEsperadas: 1);
    }

    // US-009, "proteger alta frente a Operador". Al Operador no se le muestra el formulario, pero el
    // token antifalsificación lo puede sacar de cualquier pantalla (el botón "Cerrar sesión" tiene
    // uno). Así comprobamos que el pedido no se frena por el token: lo frena el rol.
    [Fact]
    public async Task Operador_NoPuedeRegistrarUnProveedorNiConUnPostDirectoConTokenValido()
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        ProteccionAntifalsificacion proteccion = await ObtenerProteccionAntifalsificacionAsync(fabrica, "/", cookieDeSesion);

        var respuesta = await EnviarAltaDeProveedorAsync(fabrica, cookieDeSesion, proteccion);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        await ComprobarCantidadesDeProveedoresAsync(fabrica, proveedoresEsperados: 0, cuentasEsperadas: 0);
    }

    // El rol que vale es el de hoy: tener la pantalla abierta de antes, con un token válido, no alcanza.
    [Fact]
    public async Task SupervisorQuePasaAOperadorConElFormularioAbierto_NoPuedeEnviarElAlta()
    {
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Supervisor);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        ProteccionAntifalsificacion proteccion = await ObtenerProteccionAntifalsificacionAsync(fabrica, RutaRegistrarProveedor, cookieDeSesion);

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Rol = RolUsuario.Operador);

        var respuesta = await EnviarAltaDeProveedorAsync(fabrica, cookieDeSesion, proteccion);

        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        await ComprobarCantidadesDeProveedoresAsync(fabrica, proveedoresEsperados: 0, cuentasEsperadas: 0);
    }

    [Fact]
    public async Task Administrador_SinTokenAntifalsificacion_NoPuedeRegistrar()
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Administrador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await EnviarAltaDeProveedorAsync(fabrica, cookieDeSesion, proteccion: null);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await ComprobarCantidadesDeProveedoresAsync(fabrica, proveedoresEsperados: 0, cuentasEsperadas: 0);
    }

    [Fact]
    public async Task PedidoSinSesion_NoPuedeRegistrarUnProveedor()
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Administrador);

        var respuesta = await EnviarAltaDeProveedorAsync(fabrica, cookieDeSesion: null, proteccion: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        await ComprobarCantidadesDeProveedoresAsync(fabrica, proveedoresEsperados: 0, cuentasEsperadas: 0);
    }

    // La protección antifalsificación son dos mitades que se tienen que corresponder: una viaja en
    // un campo del formulario y la otra en una cookie. Como en estas pruebas las cookies se manejan
    // a mano, las guardamos las dos para mandarlas juntas en el POST.
    private sealed record ProteccionAntifalsificacion(string Token, string Cookie);

    private static async Task<ProteccionAntifalsificacion> ObtenerProteccionAntifalsificacionAsync(
        FabricaConSesionReal fabrica, string ruta, string cookieDeSesion)
    {
        var respuesta = await PedirConCookieAsync(fabrica, ruta, cookieDeSesion);
        string html = await respuesta.Content.ReadAsStringAsync();

        string token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);

        string cookie = respuesta.Headers.GetValues("Set-Cookie")
            .First(valor => valor.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))
            .Split(';')[0];

        return new ProteccionAntifalsificacion(token, cookie);
    }

    private static async Task<HttpResponseMessage> EnviarAltaDeProveedorAsync(
        FabricaConSesionReal fabrica, string? cookieDeSesion, ProteccionAntifalsificacion? proteccion)
    {
        var datos = new Dictionary<string, string>
        {
            ["Nombre"] = "Proveedor de prueba",
            ["Rut"] = "215521750017",
            ["PaisTelefono"] = "UY",
            ["NumeroNacionalTelefono"] = "099123456",
            ["CorreoContacto"] = "proveedor@ejemplo.com",
            ["CuentasBancarias[0].Banco"] = "1",
            ["CuentasBancarias[0].NumeroCuenta"] = "000111",
            ["CuentasBancarias[0].Titular"] = "Titular de prueba",
            ["CuentasBancarias[0].Moneda"] = "1"
        };

        var cookies = new List<string>();

        if (cookieDeSesion != null)
        {
            cookies.Add(cookieDeSesion);
        }

        if (proteccion != null)
        {
            datos["__RequestVerificationToken"] = proteccion.Token;
            cookies.Add(proteccion.Cookie);
        }

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaRegistrarProveedor)
        {
            Content = new FormUrlEncodedContent(datos)
        };

        if (cookies.Count > 0)
        {
            pedido.Headers.Add("Cookie", string.Join("; ", cookies));
        }

        return await CrearCliente(fabrica).SendAsync(pedido);
    }

    private static async Task ComprobarCantidadesDeProveedoresAsync(FabricaConSesionReal fabrica, int proveedoresEsperados, int cuentasEsperadas)
    {
        using var alcance = fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        Assert.Equal(proveedoresEsperados, await contexto.Set<Proveedor>().CountAsync());
        Assert.Equal(cuentasEsperadas, await contexto.Set<CuentaBancaria>().CountAsync());
    }
}
