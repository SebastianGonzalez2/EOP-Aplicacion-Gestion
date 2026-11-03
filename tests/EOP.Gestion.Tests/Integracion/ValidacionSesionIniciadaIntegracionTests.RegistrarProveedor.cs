using System.Net;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Integracion;

// Pruebas de S2-T12: quién puede abrir el alta de proveedores, pasando por toda la app (cookie
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

    // Todavía no existe el guardado (S2-T13). Usamos las tablas reales para comprobar que un POST
    // armado a mano, incluso de un rol con permiso, no deja ningún proveedor en la base.
    // Cuando S2-T13 agregue el POST de verdad, esta prueba se va a tener que reemplazar.
    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Operador)]
    public async Task PostAlAltaDeProveedores_NoExisteTodaviaYNoGuardaNada(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaRegistrarProveedor)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
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
            })
        };
        pedido.Headers.Add("Cookie", cookieDeSesion);

        var respuesta = await CrearCliente(fabrica).SendAsync(pedido);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);

        using var alcance = fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        Assert.Equal(0, await contexto.Set<Proveedor>().CountAsync());
        Assert.Equal(0, await contexto.Set<CuentaBancaria>().CountAsync());
    }
}
