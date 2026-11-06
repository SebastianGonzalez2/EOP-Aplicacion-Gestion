using System.Data.Common;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Autorizacion;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EOP.Gestion.Tests.Integracion;

// Pruebas de integración de la T11: ¿qué pasa con una sesión YA iniciada cuando
// el usuario deja de estar habilitado en EOP? (US-042: "Un usuario Inactivo no
// podrá acceder"; RF-69: "Solamente los usuarios habilitados ... deberán poder
// acceder al sistema"; RNF-4: el control se hace en el servidor.)
//
// La diferencia con las pruebas anteriores es CUÁNTO de la app real usamos:
//
// - SesionIntegracionTests (T08/T09) reemplaza toda la autenticación por un
//   esquema de prueba, así que la cookie real nunca entra en juego.
// - VinculacionInicioSesionTests (T10) agrega un esquema remoto aparte, con
//   otro nombre, que copia el evento OnTicketReceived.
//
// Acá necesitamos que corra OnValidatePrincipal, que vive en la cookie real, y
// que la cookie la acepte el esquema por defecto real (OpenIdConnect). Por eso:
//
// 1. Dejamos TODA la configuración de autenticación de Program.cs como está:
//    el esquema OpenIdConnect con sus eventos, la cookie con su OnValidatePrincipal
//    y la política "todo cerrado por defecto".
// 2. Solo cambiamos QUIÉN atiende el esquema OpenIdConnect: en vez del manejador
//    que habla con Microsoft, uno simulado que, al llegar a /signin-oidc, da por
//    autenticado a quien indiquen los encabezados del pedido. Usa las mismas
//    opciones (y por lo tanto los mismos eventos) que configuró Program.cs.
// 3. La base de datos es SQLite en memoria con las tablas reales y el
//    RepositorioUsuarios real. Así, "inactivar al usuario" es cambiar la fila
//    de verdad, igual que lo hará la futura pantalla de administración.
//
// Lo que esto NO prueba: que Microsoft Entra ID exija MFA. Eso lo decide la
// configuración del tenant (acceso condicional o valores predeterminados de
// seguridad) y se verifica a mano en Azure. Acá solo comprobamos lo que hace
// EOP con lo que Microsoft le devuelve.
//
// La clase es "partial" porque las pruebas de la T14, que usan esta misma fábrica,
// están en ValidacionSesionIniciadaIntegracionTests.ModificarRol.cs.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    private const string ObjectIdJuan = "11111111-1111-1111-1111-111111111111";
    private const string ObjectIdOtraIdentidad = "33333333-3333-3333-3333-333333333333";
    private const string CorreoJuan = "juan.perez@eop.com.uy";

    // Va en un encabezado HTTP, así que lo dejamos sin tildes.
    private const string NombreJuanEnMicrosoft = "Juan Perez Prueba";

    private const string CategoriaDeLogs = "ValidacionSesiones";

    // La excepción simulada de la base trae datos personales en su mensaje a
    // propósito: así comprobamos que no terminan ni en el log ni en la respuesta.
    private const string MensajeDeFallaConDatosSensibles =
        "Fallo simulado de la base para " + CorreoJuan + " con " + ObjectIdJuan;

    // Rutas del Controller que solo existe en las pruebas (T13).
    private const string RutaDeAccionesPorRol = "/AccionesDePruebaPorRol/";
    private const string RutaSoloAdministrador = RutaDeAccionesPorRol + nameof(AccionesDePruebaPorRolController.SoloAdministrador);
    private const string RutaSoloSupervisor = RutaDeAccionesPorRol + nameof(AccionesDePruebaPorRolController.SoloSupervisor);
    private const string RutaSoloOperador = RutaDeAccionesPorRol + nameof(AccionesDePruebaPorRolController.SoloOperador);
    private const string RutaAdministradorOSupervisor = RutaDeAccionesPorRol + nameof(AccionesDePruebaPorRolController.AdministradorOSupervisor);

    // ---------- 1. Usuario habilitado ----------

    [Fact]
    public async Task UsuarioHabilitado_IniciaSesionYPuedeUsarPantallasProtegidasEnPedidosSucesivos()
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);

        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        // Varias pantallas y varios pedidos: en cada uno se vuelve a validar la
        // sesión contra la base, y en todos tiene que seguir funcionando.
        foreach (string ruta in new[] { "/", "/Home/Privacy", "/" })
        {
            var respuesta = await PedirConCookieAsync(fabrica, ruta, cookieDeSesion);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.False(BorraLaCookieDeSesion(respuesta), $"La app borró la cookie de un usuario habilitado en {ruta}.");
        }

        var inicio = await PedirConCookieAsync(fabrica, "/", cookieDeSesion);
        Assert.Contains(NombreJuanEnMicrosoft, await inicio.Content.ReadAsStringAsync());

        Assert.DoesNotContain(fabrica.Logs.Entradas,
            e => e.Categoria == CategoriaDeLogs && e.Nivel >= LogLevel.Warning);
    }

    [Fact]
    public async Task PrimerAcceso_VinculaPorCorreoYLaSesionCreadaEsAceptadaEnElPedidoSiguiente()
    {
        // Une la T10 con la T11: en el login se vincula a Juan por su correo y,
        // en el pedido siguiente, la validación lo encuentra por ese Object ID.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: null);

        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        var respuesta = await PedirConCookieAsync(fabrica, "/", cookieDeSesion);

        Assert.Equal(ObjectIdJuan, (await fabrica.LeerUsuarioAsync(idJuan))?.EntraObjectId);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // ---------- 2. Usuario inactivado con la sesión abierta ----------

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Privacy")]
    public async Task UsuarioInactivadoDespuesDeIniciarSesion_SuSiguientePedidoProtegidoSeRechazaYSeBorraLaCookie(string rutaProtegida)
    {
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        // Antes de la inactivación, entra sin problemas.
        Assert.Equal(HttpStatusCode.OK, (await PedirConCookieAsync(fabrica, rutaProtegida, cookieDeSesion)).StatusCode);

        // Un Administrador lo inactiva (cambiamos la fila en la base).
        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = false);

        var respuesta = await PedirConCookieAsync(fabrica, rutaProtegida, cookieDeSesion);

        await AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(respuesta);
        Assert.True(BorraLaCookieDeSesion(respuesta), "La app no borró la cookie de un usuario inactivado.");

        var advertencia = Assert.Single(fabrica.Logs.Entradas,
            e => e.Categoria == CategoriaDeLogs && e.Nivel == LogLevel.Warning);
        AssertSinDatosSensibles(advertencia);
    }

    [Fact]
    public async Task UsuarioInactivadoQueVuelveAIniciarSesion_SigueSinObtenerUnaSesionNueva()
    {
        // Después del rechazo, en la vida real el navegador vuelve a Microsoft y
        // regresa a /signin-oidc. Ahí actúa la verificación de la T10: no hay cookie nueva.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = false);
        await PedirConCookieAsync(fabrica, "/", cookieDeSesion);

        var reintentoDeLogin = await HacerLoginSimuladoAsync(fabrica, ObjectIdJuan, CorreoJuan);

        Assert.Null(ExtraerCookieDeSesion(reintentoDeLogin));
        Assert.Equal(HttpStatusCode.Redirect, reintentoDeLogin.StatusCode);
        Assert.Equal(ManejadorFallosInicioSesion.RutaPantallaError, reintentoDeLogin.Headers.Location?.OriginalString);
    }

    // ---------- 3. La identidad vinculada deja de ser válida ----------

    [Theory]
    [InlineData("desvinculado")]
    [InlineData("vinculado a otra identidad")]
    [InlineData("registro eliminado")]
    public async Task IdentidadVinculadaQueDejaDeCorresponderAlUsuario_NoPuedeSeguirUsandoLaSesion(string cambioEnLaBase)
    {
        // La cookie sigue siendo auténtica (la firmó la app), pero el Object ID
        // que lleva adentro ya no corresponde a ningún usuario habilitado. El
        // usuario sigue Activo en todos los casos: lo que cambió es la vinculación.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        switch (cambioEnLaBase)
        {
            case "desvinculado":
                await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.EntraObjectId = null);
                break;
            case "vinculado a otra identidad":
                await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.EntraObjectId = ObjectIdOtraIdentidad);
                break;
            case "registro eliminado":
                // En EOP los usuarios no se eliminan (se inactivan), pero si la
                // fila no está, la sesión tampoco puede seguir.
                await fabrica.EliminarUsuarioAsync(idJuan);
                break;
        }

        var respuesta = await PedirConCookieAsync(fabrica, "/", cookieDeSesion);

        await AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(respuesta);
        Assert.True(BorraLaCookieDeSesion(respuesta));
    }

    // ---------- 4. Una cookie rechazada no sirve más ----------

    [Fact]
    public async Task CookieDeUnaSesionRechazada_ReutilizadaDespuesDelRechazo_SigueSinDarAcceso()
    {
        // El servidor pide borrar la cookie, pero no puede obligar al cliente a
        // hacerlo: alguien pudo haberla copiado antes. Por eso la validación
        // tiene que volver a rechazarla en CADA pedido, no solo una vez.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        string cookieCopiada = await IniciarSesionYObtenerCookieAsync(fabrica);
        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = false);

        var primerRechazo = await PedirConCookieAsync(fabrica, "/", cookieCopiada);
        Assert.Equal(HttpStatusCode.Unauthorized, primerRechazo.StatusCode);

        // Ignoramos el pedido de borrado y volvemos a mandar la misma cookie.
        foreach (string ruta in new[] { "/", "/Home/Privacy", "/Home/Index" })
        {
            var reintento = await PedirConCookieAsync(fabrica, ruta, cookieCopiada);

            await AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(reintento);
            Assert.True(BorraLaCookieDeSesion(reintento));
        }
    }

    [Fact]
    public async Task CookieRechazadaEnUnaPantallaPublica_LaPantallaSeMuestraComoAnonimoYLaCookieSeBorra()
    {
        // Las pantallas públicas ([AllowAnonymous]) se tienen que seguir viendo,
        // pero sin tratar al visitante como si todavía tuviera sesión.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = false);

        var respuesta = await PedirConCookieAsync(fabrica, ManejadorFallosInicioSesion.RutaPantallaError, cookieDeSesion);
        string html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(BorraLaCookieDeSesion(respuesta));
        Assert.DoesNotContain(NombreJuanEnMicrosoft, html);
        Assert.DoesNotContain("action=\"/Cuenta/CerrarSesion\"", html);
    }

    // ---------- 5. Falla de la base durante la validación ----------

    [Fact]
    public async Task FallaDeLaBaseDuranteLaValidacion_NoPermiteElAccesoNiExponeElErrorYLoRegistraSinDatosSensibles()
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        // A partir de ahora, toda consulta a la base falla (como si SQL Server
        // no respondiera). El usuario sigue Activo: no hay forma de confirmarlo.
        fabrica.FallasDeBase.SimularCaida = true;

        var respuesta = await PedirConCookieAsync(fabrica, "/", cookieDeSesion);
        string cuerpo = await respuesta.Content.ReadAsStringAsync();

        // Ni 200 (acceso) ni 500 (excepción escapada): se trata como sin sesión.
        await AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(respuesta);
        Assert.True(BorraLaCookieDeSesion(respuesta));
        Assert.DoesNotContain("Fallo simulado", cuerpo);
        Assert.DoesNotContain(nameof(SqliteException), cuerpo);

        var error = Assert.Single(fabrica.Logs.Entradas,
            e => e.Categoria == CategoriaDeLogs && e.Nivel == LogLevel.Error);
        Assert.Contains(nameof(SqliteException), error.Mensaje);
        AssertSinDatosSensibles(error);
    }

    // ---------- 6. Pedidos sin sesión o con una cookie que no es de la app ----------

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Index")]
    [InlineData("/Home/Privacy")]
    public async Task PedidoSinAutenticacion_NoAccedeYNiSiquieraConsultaLaBase(string rutaProtegida)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);

        // Si la validación consultara la base sin cookie, esta falla lo delataría con un error en el log.
        fabrica.FallasDeBase.SimularCaida = true;

        var respuesta = await CrearCliente(fabrica).GetAsync(rutaProtegida);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.DoesNotContain(fabrica.Logs.Entradas, e => e.Categoria == CategoriaDeLogs);
    }

    [Theory]
    [InlineData("valor-inventado")]
    [InlineData("CfDJ8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task CookieFalsificadaOAlterada_NoDaAccesoAunqueElUsuarioExistaYEsteActivo(string valorFalsificado)
    {
        // La cookie va cifrada y firmada por la app (Data Protection). Una
        // inventada o modificada no se puede abrir, así que el pedido es anónimo.
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        string nombreDeLaCookie = CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme;

        var respuesta = await PedirConCookieAsync(fabrica, "/", $"{nombreDeLaCookie}={valorFalsificado}");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task CookieAlteradaAPartirDeUnaReal_NoDaAcceso()
    {
        // Partimos de una cookie auténtica y le cambiamos un carácter del medio.
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);
        string cookieReal = await IniciarSesionYObtenerCookieAsync(fabrica);

        int posicion = cookieReal.Length / 2;
        char reemplazo = cookieReal[posicion] == 'A' ? 'B' : 'A';
        string cookieAlterada = cookieReal[..posicion] + reemplazo + cookieReal[(posicion + 1)..];

        var respuesta = await PedirConCookieAsync(fabrica, "/", cookieAlterada);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    // ---------- 7. El inicio de sesión de la T10 sigue funcionando ----------

    [Theory]
    [InlineData("inactivo")]
    [InlineData("no registrado")]
    public async Task LoginDeUsuarioNoHabilitado_ConLaBaseReal_SigueSinCrearSesionYVaALaPantallaDeError(string situacion)
    {
        using var fabrica = new FabricaConSesionReal();

        if (situacion == "inactivo")
        {
            await fabrica.GuardarUsuarioAsync(activo: false, entraObjectId: ObjectIdJuan);
        }

        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdJuan, CorreoJuan);

        Assert.Null(ExtraerCookieDeSesion(respuesta));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(ManejadorFallosInicioSesion.RutaPantallaError, respuesta.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task EntraIdInformaQueNoSeCompletoElSegundoFactor_EopNoCreaSesionYVaALaPantallaDeError()
    {
        // Simulamos la RESPUESTA de Microsoft cuando el usuario no completa el
        // MFA (Entra devuelve un error en vez de un usuario autenticado). Esto
        // prueba lo que hace EOP con esa respuesta, no que Entra ID exija MFA.
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);

        var pedido = new HttpRequestMessage(HttpMethod.Get, ManejadorOpenIdConnectSimulado.RutaRetorno);
        pedido.Headers.Add(ManejadorOpenIdConnectSimulado.EncabezadoErrorDeEntra, "interaction_required");
        var respuesta = await CrearCliente(fabrica).SendAsync(pedido);

        Assert.Null(ExtraerCookieDeSesion(respuesta));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(ManejadorFallosInicioSesion.RutaPantallaError, respuesta.Headers.Location?.OriginalString);
    }

    // ---------- 8. Autorización por rol (T13) ----------

    // US-044, RF-73 y RNF-4: el permiso lo decide el servidor en cada pedido, con
    // el rol que está guardado HOY en la tabla Usuarios. Estas pruebas pasan por
    // toda la app: cookie real, OnValidatePrincipal, base con las tablas reales y
    // el atributo [Authorize(Roles = ...)] de una acción (la del Controller de prueba).

    [Theory]
    [InlineData(RolUsuario.Administrador, RutaSoloAdministrador)]
    [InlineData(RolUsuario.Supervisor, RutaSoloSupervisor)]
    [InlineData(RolUsuario.Operador, RutaSoloOperador)]
    public async Task CadaRolGuardadoEnEop_EsReconocidoYSoloEntraALaAccionDeSuRol(RolUsuario rol, string rutaDeSuRol)
    {
        // Probamos también las acciones de los OTROS roles: así una app que
        // le diera todos los roles a cualquiera no pasaría esta prueba.
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        foreach (string ruta in new[] { RutaSoloAdministrador, RutaSoloSupervisor, RutaSoloOperador })
        {
            var respuesta = await PedirConCookieAsync(fabrica, ruta, cookieDeSesion);

            if (ruta == rutaDeSuRol)
            {
                await AssertAccionEjecutadaAsync(respuesta);
            }
            else
            {
                await AssertAccesoDenegadoPorRolAsync(respuesta);
            }
        }
    }

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    public async Task AdministradorYSupervisor_EntranALaAccionPermitidaParaAmbos(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieDeSesion);

        await AssertAccionEjecutadaAsync(respuesta);
    }

    [Fact]
    public async Task Operador_NoEntraALaAccionDeAdministradorYSupervisorAunquePidaLaUrlDirectamente()
    {
        // Es el pedido directo por URL del que habla RNF-4: no hay ningún botón
        // de por medio, así que el único que puede frenarlo es el servidor.
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieDeSesion);

        await AssertAccesoDenegadoPorRolAsync(respuesta);
    }

    [Fact]
    public async Task UsuarioSinPermiso_VeLaPantallaDeAccesoDenegadoYNoLaAccionProtegida()
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieDeSesion);
        await AssertAccesoDenegadoPorRolAsync(respuesta);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);

        // Seguimos la redirección, como haría el navegador.
        var pantalla = await PedirConCookieAsync(fabrica, respuesta.Headers.Location!.PathAndQuery, cookieDeSesion);
        string html = await pantalla.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, pantalla.StatusCode);
        Assert.Contains("Acceso denegado", html);
        Assert.DoesNotContain(AccionesDePruebaPorRolController.TextoDeAccionEjecutada, html);

        // Sin sesión, la pantalla tampoco se muestra: primero hay que identificarse.
        var sinSesion = await CrearCliente(fabrica).GetAsync("/Cuenta/AccesoDenegado");
        Assert.Equal(HttpStatusCode.Unauthorized, sinSesion.StatusCode);
    }

    [Fact]
    public async Task RolCambiadoEnLaBaseConLaSesionAbierta_LosPedidosSiguientesUsanElRolNuevo()
    {
        // Siempre mandamos LA MISMA cookie y nunca volvemos a iniciar sesión.
        // Probamos los dos sentidos: ganar un permiso y, sobre todo, perderlo.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Operador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        await AssertAccesoDenegadoPorRolAsync(await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieDeSesion));

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Rol = RolUsuario.Supervisor);
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieDeSesion));

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Rol = RolUsuario.Operador);
        await AssertAccesoDenegadoPorRolAsync(await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieDeSesion));
    }

    [Theory]
    [InlineData("inactivo")]
    [InlineData("inexistente")]
    public async Task UsuarioConRolPermitidoQueDejaDeEstarHabilitado_NoEntraConSuSesionAnterior(string situacion)
    {
        // Tener el rol correcto no alcanza: primero tiene que seguir siendo un
        // usuario habilitado. Por eso usamos un Administrador, que tiene permiso.
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Administrador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieDeSesion));

        if (situacion == "inactivo")
        {
            await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Activo = false);
        }
        else
        {
            await fabrica.EliminarUsuarioAsync(idJuan);
        }

        var respuesta = await PedirConCookieAsync(fabrica, RutaAdministradorOSupervisor, cookieDeSesion);

        await AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(respuesta);
        Assert.DoesNotContain(AccionesDePruebaPorRolController.TextoDeAccionEjecutada, await respuesta.Content.ReadAsStringAsync());
        Assert.True(BorraLaCookieDeSesion(respuesta));
    }

    // ---------- Ayudas ----------

    private static async Task AssertAccionEjecutadaAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(AccionesDePruebaPorRolController.TextoDeAccionEjecutada, await respuesta.Content.ReadAsStringAsync());
    }

    // Autenticado pero sin permiso: la acción no se ejecuta y ASP.NET Core lo manda
    // a la ruta de "acceso denegado". No es lo mismo que un rechazo de sesión (401):
    // el usuario sigue siendo válido, así que su cookie NO se borra.
    private static async Task AssertAccesoDenegadoPorRolAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        Assert.DoesNotContain(AccionesDePruebaPorRolController.TextoDeAccionEjecutada, await respuesta.Content.ReadAsStringAsync());
        Assert.False(BorraLaCookieDeSesion(respuesta), "La app borró la cookie de un usuario que solo no tenía permiso.");
    }

    // El pedido protegido no se atendió: el esquema simulado responde 401 donde
    // la app real mandaría a Microsoft a iniciar sesión. Lo importante es que no
    // es 200 (la pantalla no se mostró), ni 500 (no se escapó una excepción),
    // y que la respuesta no cuenta nada del usuario ni de su estado.
    private static async Task AssertAccesoRechazadoSinMostrarLaPantallaNiDatosDelUsuarioAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        string cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(NombreJuanEnMicrosoft, cuerpo);
        Assert.DoesNotContain(CorreoJuan, cuerpo);
        Assert.DoesNotContain(ObjectIdJuan, cuerpo);
        Assert.DoesNotContain("inactiv", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertSinDatosSensibles(EntradaDeLog entrada)
    {
        // "@" cubre cualquier correo, no solo los de estas pruebas.
        foreach (string datoSensible in new[] { "@", ObjectIdJuan, ObjectIdOtraIdentidad, NombreJuanEnMicrosoft, "Fallo simulado" })
        {
            Assert.DoesNotContain(datoSensible, entrada.Mensaje);
            Assert.DoesNotContain(datoSensible, entrada.Valores);
        }

        Assert.Null(entrada.Excepcion);
    }

    private static HttpClient CrearCliente(FabricaConSesionReal fabrica)
    {
        // Manejamos la cookie a mano (HandleCookies = false) para poder volver
        // a mandarla aunque el servidor haya pedido borrarla, como haría
        // alguien que la copió. Tampoco seguimos redirecciones solos.
        return fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static async Task<HttpResponseMessage> HacerLoginSimuladoAsync(
        FabricaConSesionReal fabrica, string entraObjectId, string correo)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Get, ManejadorOpenIdConnectSimulado.RutaRetorno);
        pedido.Headers.Add(ManejadorOpenIdConnectSimulado.EncabezadoOid, entraObjectId);
        pedido.Headers.Add(ManejadorOpenIdConnectSimulado.EncabezadoCorreo, correo);
        pedido.Headers.Add(ManejadorOpenIdConnectSimulado.EncabezadoNombre, NombreJuanEnMicrosoft);

        return await CrearCliente(fabrica).SendAsync(pedido);
    }

    // Hace el login completo y devuelve la cookie lista para mandarla en el
    // encabezado "Cookie" de los pedidos siguientes.
    private static async Task<string> IniciarSesionYObtenerCookieAsync(FabricaConSesionReal fabrica)
    {
        var respuesta = await HacerLoginSimuladoAsync(fabrica, ObjectIdJuan, CorreoJuan);

        // Si esto falla, el problema está en el login (T10), no en la validación.
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/", respuesta.Headers.Location?.OriginalString);

        string? cookie = ExtraerCookieDeSesion(respuesta);
        Assert.NotNull(cookie);
        return cookie;
    }

    private static async Task<HttpResponseMessage> PedirConCookieAsync(
        FabricaConSesionReal fabrica, string ruta, string cookie)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Get, ruta);
        pedido.Headers.Add("Cookie", cookie);
        return await CrearCliente(fabrica).SendAsync(pedido);
    }

    // La cookie de sesión se llama ".AspNetCore.Cookies" (o ".AspNetCore.CookiesC1",
    // ".AspNetCore.CookiesC2"... cuando es grande y se parte en pedazos). Juntamos
    // todos los pedazos en el formato del encabezado "Cookie": "nombre=valor; nombre=valor".
    private static string? ExtraerCookieDeSesion(HttpResponseMessage respuesta)
    {
        if (!respuesta.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        string nombre = CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme;
        var pedazos = cookies
            .Where(c => c.StartsWith(nombre, StringComparison.Ordinal) && !c.Contains("=;", StringComparison.Ordinal))
            .Select(c => c.Split(';')[0])
            .ToList();

        return pedazos.Count == 0 ? null : string.Join("; ", pedazos);
    }

    // Borrar una cookie es mandarla vacía y con vencimiento en el pasado
    // (1970). Así lo hace SignOutAsync.
    private static bool BorraLaCookieDeSesion(HttpResponseMessage respuesta)
    {
        if (!respuesta.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return false;
        }

        string nombre = CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme;
        return cookies.Any(c => c.StartsWith(nombre + "=;", StringComparison.Ordinal)
            && c.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
    }

    // Arma la app como la real, salvo:
    // - la base: SQLite en memoria con las tablas reales (en vez de SQL Server),
    //   y un interceptor que permite simular que la base se cae;
    // - quién atiende el esquema OpenIdConnect (el manejador simulado);
    // - un registro de logs en memoria, que se suma a los de la app.
    private sealed class FabricaConSesionReal : WebApplicationFactory<Program>
    {
        // Mientras la conexión siga abierta, la base en memoria existe. La
        // comparten todos los pedidos de la prueba, como una base real.
        private readonly SqliteConnection _conexion = new("Data Source=:memory:");
        private bool _baseCreada;

        public RegistroDeLogsDePrueba Logs { get; } = new();
        public InterceptorQueSimulaCaidasDeLaBase FallasDeBase { get; } = new();

        public FabricaConSesionReal()
        {
            _conexion.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Program.cs exige una cadena de conexión al arrancar; no se usa
            // porque abajo reemplazamos SQL Server por SQLite.
            builder.UseSetting(
                "ConnectionStrings:EopDb",
                "Server=(localdb)\\pruebas;Database=NoSeUsa;Trusted_Connection=True");

            builder.ConfigureTestServices(servicios =>
            {
                // Sacamos la configuración de SQL Server que registró
                // AgregarInfraestructura y ponemos SQLite. Si no sacáramos las
                // dos registraciones, EF Core vería dos proveedores y fallaría.
                servicios.RemoveAll<DbContextOptions<EopDbContext>>();
                servicios.RemoveAll<IDbContextOptionsConfiguration<EopDbContext>>();
                servicios.AddDbContext<EopDbContext>(opciones => opciones
                    .UseSqlite(_conexion)
                    .AddInterceptors(FallasDeBase));

                servicios.AddSingleton<ILoggerProvider>(Logs);

                // Le mostramos a MVC el Controller que solo existe en las pruebas.
                servicios.AddControllersWithViews()
                    .AddApplicationPart(typeof(AccionesDePruebaPorRolController).Assembly);

                // Mismo esquema, mismas opciones, mismos eventos: solo cambia la
                // clase que lo atiende.
                servicios.PostConfigure<AuthenticationOptions>(opciones =>
                {
                    var esquemaOpenIdConnect = opciones.Schemes.Single(
                        esquema => esquema.Name == OpenIdConnectDefaults.AuthenticationScheme);

                    esquemaOpenIdConnect.HandlerType = typeof(ManejadorOpenIdConnectSimulado);
                });
            });
        }

        public async Task<int> GuardarUsuarioAsync(bool activo, string? entraObjectId, RolUsuario rol = RolUsuario.Operador, string correo = CorreoJuan)
        {
            using var alcance = CrearAlcanceConBaseCreada();
            var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

            var usuario = new Usuario
            {
                NombreCompleto = "Juan Pérez",
                CorreoCorporativo = correo,
                Rol = rol,
                Activo = activo,
                EntraObjectId = entraObjectId
            };

            contexto.Usuarios.Add(usuario);
            await contexto.SaveChangesAsync();
            return usuario.Id;
        }

        public async Task ModificarUsuarioAsync(int usuarioId, Action<Usuario> cambio)
        {
            using var alcance = CrearAlcanceConBaseCreada();
            var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

            var usuario = await contexto.Usuarios.SingleAsync(u => u.Id == usuarioId);
            cambio(usuario);
            await contexto.SaveChangesAsync();
        }

        public async Task EliminarUsuarioAsync(int usuarioId)
        {
            using var alcance = CrearAlcanceConBaseCreada();
            var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

            await contexto.Usuarios.Where(u => u.Id == usuarioId).ExecuteDeleteAsync();
        }

        public async Task<Usuario?> LeerUsuarioAsync(int usuarioId)
        {
            using var alcance = CrearAlcanceConBaseCreada();
            var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

            return await contexto.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == usuarioId);
        }

        // La primera vez que se pide algo a la base, la creamos con la misma
        // configuración de EF Core que usa SQL Server.
        private IServiceScope CrearAlcanceConBaseCreada()
        {
            var alcance = Services.CreateScope();

            if (!_baseCreada)
            {
                alcance.ServiceProvider.GetRequiredService<EopDbContext>().Database.EnsureCreated();
                _baseCreada = true;
            }

            return alcance;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _conexion.Dispose();
            }
        }
    }

    // Un interceptor de EF Core es código que corre justo antes de que EF Core
    // mande un comando a la base. Lo usamos para simular que la base no
    // responde: cuando SimularCaida es true, la consulta falla con una
    // excepción del propio proveedor (SqliteException), igual que fallaría la real.
    private sealed class InterceptorQueSimulaCaidasDeLaBase : DbCommandInterceptor
    {
        public bool SimularCaida { get; set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            LanzarSiLaBaseEstaCaida();
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            LanzarSiLaBaseEstaCaida();
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void LanzarSiLaBaseEstaCaida()
        {
            if (SimularCaida)
            {
                // 10 = SQLITE_IOERR: un error de entrada/salida de la base.
                throw new SqliteException(MensajeDeFallaConDatosSensibles, 10);
            }
        }
    }

    // Hace el papel de Microsoft volviendo a la app en /signin-oidc (la
    // CallbackPath real del appsettings). Hereda de RemoteAuthenticationHandler,
    // igual que el manejador real de OpenID Connect, y usa SUS opciones: por eso
    // ASP.NET Core ejecuta los eventos que configuró Program.cs (OnTicketReceived,
    // OnRemoteFailure) y, si todo va bien, crea la cookie con el esquema real.
    //
    // Cuando hay que pedir login (un pedido protegido sin sesión válida), este
    // manejador responde 401 en lugar de redirigir a Microsoft.
    private sealed class ManejadorOpenIdConnectSimulado(
        IOptionsMonitor<OpenIdConnectOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder codificador)
        : RemoteAuthenticationHandler<OpenIdConnectOptions>(opciones, logger, codificador)
    {
        public const string RutaRetorno = "/signin-oidc";
        public const string EncabezadoOid = "X-Oid-Simulado";
        public const string EncabezadoCorreo = "X-Correo-Simulado";
        public const string EncabezadoNombre = "X-Nombre-Simulado";
        public const string EncabezadoErrorDeEntra = "X-Error-Entra-Simulado";

        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            // Microsoft no autenticó a la persona (por ejemplo, no completó el
            // segundo factor) y volvió con un código de error, como hace el
            // manejador real con los parámetros "error" y "error_description".
            if (Request.Headers.TryGetValue(EncabezadoErrorDeEntra, out var codigoDeError))
            {
                var falla = new AuthenticationFailureException("Microsoft Entra ID devolvió un error simulado.");
                falla.Data["error"] = codigoDeError.ToString();
                return Task.FromResult(HandleRequestResult.Fail(falla));
            }

            var claims = new List<Claim>
            {
                new("oid", Request.Headers[EncabezadoOid].ToString()),
                new("preferred_username", Request.Headers[EncabezadoCorreo].ToString()),
                new("name", Request.Headers[EncabezadoNombre].ToString())
            };

            var identidad = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            var ticket = new AuthenticationTicket(identidad, new AuthenticationProperties(), Scheme.Name);

            return Task.FromResult(HandleRequestResult.Success(ticket));
        }
    }
}
