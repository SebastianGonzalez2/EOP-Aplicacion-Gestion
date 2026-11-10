
using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Integracion;

// Pruebas de S1-T17: el alta de usuarios pasando por toda la app (cookie real, OnValidatePrincipal,
// [Authorize], token antifalsificación, Controller, caso de uso, repositorio real y vista). El login
// de Microsoft es el simulado de esta fábrica: acá no hay ningún inicio de sesión real con Entra ID.
//
// La base es SQLite: el índice único bajo concurrencia se prueba en RegistrarUsuarioSqlServerTests.
public partial class ValidacionSesionIniciadaIntegracionTests
{
    private const string RutaRegistrarUsuario = "/Usuarios/Registrar";
    private const string RutaListadoUsuarios = "/Usuarios";
    private const string CorreoNuevo = "nuevo.usuario@eop.com.uy";
    private const string ObjectIdUsuarioNuevo = "66666666-6666-6666-6666-666666666666";
    private const string MensajeUsuarioRegistrado = "El usuario se registró correctamente.";
    private const string MensajeCorreoRegistrado = "El correo corporativo ya se encuentra registrado.";

    // ---------- Acceso al formulario ----------

    [Fact]
    public async Task Administrador_AbreElFormularioConLosTresDatosYSinSelectorDeEstado()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaRegistrarUsuario, cookieAdministrador);
        string html = await LeerHtmlDecodificadoAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("name=\"NombreCompleto\"", html);
        Assert.Contains("name=\"CorreoCorporativo\"", html);
        Assert.Contains("name=\"__RequestVerificationToken\"", html);
        Assert.Equal(["", "Administrador", "Supervisor", "Operador"], ObtenerOpcionesDeRol(html));
        Assert.DoesNotContain("name=\"Activo\"", html);
        Assert.DoesNotContain("name=\"Estado\"", html);
        Assert.DoesNotContain("EntraObjectId", html);
    }

    [Theory]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task SupervisorYOperador_NoPuedenAbrirElFormularioNiPorUrlDirecta(RolUsuario rol)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await PedirConCookieAsync(fabrica, RutaRegistrarUsuario, cookieDeSesion);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        Assert.DoesNotContain("name=\"CorreoCorporativo\"", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PedidoSinSesion_NoPuedeAbrirElFormulario()
    {
        using var fabrica = new FabricaConSesionReal();

        var respuesta = await CrearCliente(fabrica).GetAsync(RutaRegistrarUsuario);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(RolUsuario.Administrador, true)]
    [InlineData(RolUsuario.Supervisor, false)]
    [InlineData(RolUsuario.Operador, false)]
    public async Task ElEnlaceAlListadoDeLaBarra_SoloLoVeElAdministrador(RolUsuario rol, bool debeVerlo)
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        string html = await (await PedirConCookieAsync(fabrica, "/", cookieDeSesion)).Content.ReadAsStringAsync();

        // El menú ahora apunta al listado de usuarios y no al formulario de registro.
        Assert.Equal(debeVerlo, html.Contains($"href=\"{RutaListadoUsuarios}\""));
    }

    // ---------- Quién puede registrar ----------

    [Theory]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task SupervisorYOperador_ConSesionYTokenValidos_NoPuedenRegistrar(RolUsuario rol)
    {
        // El formulario no se les muestra, así que el token lo sacan del botón "Cerrar sesión"
        // de la página de inicio: es el pedido armado a mano del que habla RNF-4.
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: rol);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);

        var respuesta = await EnviarAltaAsync(fabrica, cookieDeSesion, DatosDeAltaValidos(), rutaDelToken: "/");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        Assert.Null(await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo));
    }

    [Fact]
    public async Task PostSinSesion_SeRechazaYNoGuardaNada()
    {
        using var fabrica = new FabricaConSesionReal();
        await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan);

        var respuesta = await CrearCliente(fabrica).PostAsync(RutaRegistrarUsuario, new FormUrlEncodedContent(DatosDeAltaValidos()));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Null(await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo));
    }

    [Fact]
    public async Task AdministradorDegradadoConLaSesionAbierta_PierdeElPermisoParaRegistrar()
    {
        using var fabrica = new FabricaConSesionReal();
        int idJuan = await fabrica.GuardarUsuarioAsync(activo: true, entraObjectId: ObjectIdJuan, rol: RolUsuario.Administrador);
        string cookieDeSesion = await IniciarSesionYObtenerCookieAsync(fabrica);
        Assert.Equal(HttpStatusCode.OK, (await PedirConCookieAsync(fabrica, RutaRegistrarUsuario, cookieDeSesion)).StatusCode);

        await fabrica.ModificarUsuarioAsync(idJuan, usuario => usuario.Rol = RolUsuario.Operador);

        var respuesta = await EnviarAltaAsync(fabrica, cookieDeSesion, DatosDeAltaValidos(), rutaDelToken: "/");

        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.AbsolutePath);
        Assert.Null(await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo));
    }

    // ---------- Protección antifalsificación y verbos HTTP ----------

    [Fact]
    public async Task AdministradorSinToken_SeRechazaYNoGuardaNada()
    {
        // Es lo que mandaría otra página que quiere aprovechar la sesión del Administrador (CSRF).
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaRegistrarUsuario)
        {
            Content = new FormUrlEncodedContent(DatosDeAltaValidos())
        };
        pedido.Headers.Add("Cookie", cookieAdministrador);

        var respuesta = await CrearCliente(fabrica).SendAsync(pedido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Null(await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo));
    }

    [Fact]
    public async Task AdministradorConTokenAlterado_SeRechazaYNoGuardaNada()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);

        var respuesta = await EnviarAltaAsync(fabrica, cookieAdministrador, DatosDeAltaValidos(), alterarToken: true);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Null(await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo));
    }

    [Fact]
    public async Task PedidoGetConLosDatosEnLaUrl_NoGuardaNada()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);

        await PedirConCookieAsync(
            fabrica,
            $"{RutaRegistrarUsuario}?NombreCompleto=Nuevo&CorreoCorporativo={CorreoNuevo}&Rol=Administrador",
            cookieAdministrador);

        Assert.Null(await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo));
    }

    // ---------- Alta correcta ----------

    [Theory]
    [InlineData("Administrador", RolUsuario.Administrador)]
    [InlineData("Supervisor", RolUsuario.Supervisor)]
    [InlineData("Operador", RolUsuario.Operador)]
    public async Task DatosValidos_GuardanElUsuarioActivoConSuRolYSinVincular(string rolEnviado, RolUsuario rolEsperado)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        var datos = DatosDeAltaValidos();
        datos["Rol"] = rolEnviado;

        var respuesta = await EnviarAltaAsync(fabrica, cookieAdministrador, datos);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        Usuario? guardado = await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo);
        Assert.NotNull(guardado);
        Assert.Equal("Nuevo Usuario", guardado.NombreCompleto);
        Assert.Equal(rolEsperado, guardado.Rol);
        Assert.True(guardado.Activo);
        Assert.Null(guardado.EntraObjectId);
    }

    [Fact]
    public async Task DespuesDelAlta_RedirigeAUnaPantallaQueExiste_YActualizarlaNoGuardaDeNuevo()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);

        var respuesta = await EnviarAltaAsync(fabrica, cookieAdministrador, DatosDeAltaValidos());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RutaRegistrarUsuario, respuesta.Headers.Location?.OriginalString);

        // El mensaje viaja en la cookie de TempData, igual que en el navegador.
        string cookieTempData = respuesta.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith(".AspNetCore.Mvc.CookieTempDataProvider", StringComparison.Ordinal))
            .Split(';')[0];

        var pantalla = await PedirConCookieAsync(fabrica, RutaRegistrarUsuario, $"{cookieAdministrador}; {cookieTempData}");
        string html = await LeerHtmlDecodificadoAsync(pantalla);

        Assert.Equal(HttpStatusCode.OK, pantalla.StatusCode);
        Assert.Contains(MensajeUsuarioRegistrado, html);
        Assert.DoesNotContain(CorreoNuevo, html);

        // Actualizar la página repite este GET, no el POST: ni otro usuario ni el mensaje otra vez.
        var actualizacion = await PedirConCookieAsync(fabrica, RutaRegistrarUsuario, cookieAdministrador);

        Assert.DoesNotContain(MensajeUsuarioRegistrado, await LeerHtmlDecodificadoAsync(actualizacion));
        Assert.Equal(1, await ContarUsuariosConCorreoAsync(fabrica, CorreoNuevo));
    }

    [Fact]
    public async Task UsuarioRegistrado_VinculaSuIdentidadEnElPrimerAccesoSimulado_YEntraConElRolAsignado()
    {
        // Recorrido completo de US-043 con el login simulado. No prueba Microsoft Entra ID ni MFA.
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        await EnviarAltaAsync(fabrica, cookieAdministrador, DatosDeAltaValidos());

        string cookieNuevo = await IniciarSesionComoAsync(fabrica, ObjectIdUsuarioNuevo, CorreoNuevo);

        Assert.Equal(ObjectIdUsuarioNuevo, (await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo))?.EntraObjectId);
        await AssertAccionEjecutadaAsync(await PedirConCookieAsync(fabrica, RutaSoloSupervisor, cookieNuevo));
        await AssertAccesoDenegadoPorRolAsync(await PedirConCookieAsync(fabrica, RutaSoloAdministrador, cookieNuevo));
    }

    // ---------- Datos inválidos ----------

    [Theory]
    [InlineData("NombreCompleto", "", "El nombre completo es obligatorio.")]
    [InlineData("NombreCompleto", "   ", "El nombre completo es obligatorio.")]
    [InlineData("CorreoCorporativo", "", "El correo corporativo es obligatorio.")]
    [InlineData("CorreoCorporativo", "   ", "El correo corporativo es obligatorio.")]
    [InlineData("CorreoCorporativo", "sin-arroba", "El correo corporativo no tiene un formato válido.")]
    [InlineData("CorreoCorporativo", "ana perez@eop.com.uy", "El correo corporativo no tiene un formato válido.")]
    [InlineData("CorreoCorporativo", "Ana <ana@eop.com.uy>", "El correo corporativo no tiene un formato válido.")]
    [InlineData("Rol", "", "Seleccioná un rol.")]
    public async Task DatoObligatorioVacioOInvalido_MuestraElMotivoYNoGuardaNada(string campo, string valor, string mensaje)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        var datos = DatosDeAltaValidos();
        datos[campo] = valor;

        var respuesta = await EnviarAltaAsync(fabrica, cookieAdministrador, datos);
        string html = await LeerHtmlDecodificadoAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(mensaje, html);
        Assert.DoesNotContain("field is required", html);
        Assert.Equal(1, await ContarUsuariosAsync(fabrica));
    }

    [Theory]
    [InlineData("NombreCompleto", 151, "El nombre completo no puede superar los 150 caracteres.")]
    [InlineData("CorreoCorporativo", 255, "El correo corporativo no puede superar los 254 caracteres.")]
    public async Task ValorMasLargoQueLaColumna_MuestraElMotivoEnVezDeUnErrorDeLaBase(string campo, int largo, string mensaje)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        var datos = DatosDeAltaValidos();
        datos[campo] = campo == "CorreoCorporativo"
            ? new string('a', largo - "@eop.com.uy".Length) + "@eop.com.uy"
            : new string('a', largo);

        var respuesta = await EnviarAltaAsync(fabrica, cookieAdministrador, datos);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(mensaje, await LeerHtmlDecodificadoAsync(respuesta));
        Assert.Equal(1, await ContarUsuariosAsync(fabrica));
    }

    [Theory]
    [InlineData("Gerente")]
    [InlineData("administrador")]
    [InlineData("99")]
    [InlineData("0")]
    [InlineData("2")]
    [InlineData("Operador ")]
    [InlineData("Operador\n")]
    [InlineData("Administrador,Supervisor")]
    [InlineData("1,2")]
    public async Task RolManipulado_SeRechazaYNoGuardaNada(string rolEnviado)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        var datos = DatosDeAltaValidos();
        datos["Rol"] = rolEnviado;

        var respuesta = await EnviarAltaAsync(fabrica, cookieAdministrador, datos);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Seleccioná un rol válido.", await LeerHtmlDecodificadoAsync(respuesta));
        Assert.Null(await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo));
    }

    [Fact]
    public async Task CamposAgregadosAMano_SeIgnoran_YElUsuarioIgualQuedaActivoYSinVincular()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        var datos = DatosDeAltaValidos();
        datos["Activo"] = "false";
        datos["EntraObjectId"] = ObjectIdJuan;
        datos["Id"] = "999";

        await EnviarAltaAsync(fabrica, cookieAdministrador, datos);

        Usuario? guardado = await LeerUsuarioPorCorreoAsync(fabrica, CorreoNuevo);
        Assert.NotNull(guardado);
        Assert.True(guardado.Activo);
        Assert.Null(guardado.EntraObjectId);
        Assert.NotEqual(999, guardado.Id);
    }

    [Fact]
    public async Task DatosInvalidos_ConservanLoEscritoParaCorregirlo()
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        var datos = DatosDeAltaValidos();
        datos["CorreoCorporativo"] = "sin-arroba";

        string html = await LeerHtmlDecodificadoAsync(await EnviarAltaAsync(fabrica, cookieAdministrador, datos));

        Assert.Contains("value=\"Nuevo Usuario\"", html);
        Assert.Contains("value=\"sin-arroba\"", html);
        Assert.Contains("<option value=\"Supervisor\" selected=\"selected\">", html);
    }

    // ---------- Duplicados ----------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CorreoDeOtroUsuario_InformaElMotivoYNoCreaNiModificaNada(bool otroUsuarioActivo)
    {
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);
        int idExistente = await fabrica.GuardarUsuarioAsync(activo: otroUsuarioActivo, entraObjectId: null, rol: RolUsuario.Operador, correo: CorreoNuevo);

        var respuesta = await EnviarAltaAsync(fabrica, cookieAdministrador, DatosDeAltaValidos());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(MensajeCorreoRegistrado, await LeerHtmlDecodificadoAsync(respuesta));
        Assert.Equal(1, await ContarUsuariosConCorreoAsync(fabrica, CorreoNuevo));

        Usuario? existente = await fabrica.LeerUsuarioAsync(idExistente);
        Assert.NotNull(existente);
        Assert.Equal("Juan Pérez", existente.NombreCompleto);
        Assert.Equal(RolUsuario.Operador, existente.Rol);
        Assert.Equal(otroUsuarioActivo, existente.Activo);
    }

    [Fact]
    public async Task ElMismoFormularioEnviadoDosVeces_SoloGuardaUnaVez()
    {
        // Por ejemplo, un doble clic con el JavaScript apagado: los dos POST llevan datos y token válidos.
        using var fabrica = new FabricaConSesionReal();
        string cookieAdministrador = await PrepararAdministradorConSesionAsync(fabrica);

        var primera = await EnviarAltaAsync(fabrica, cookieAdministrador, DatosDeAltaValidos());
        var segunda = await EnviarAltaAsync(fabrica, cookieAdministrador, DatosDeAltaValidos());

        Assert.Equal(HttpStatusCode.Redirect, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Contains(MensajeCorreoRegistrado, await LeerHtmlDecodificadoAsync(segunda));
        Assert.Equal(1, await ContarUsuariosConCorreoAsync(fabrica, CorreoNuevo));
    }

    // ---------- Ayudas ----------

    private static Dictionary<string, string> DatosDeAltaValidos() => new()
    {
        ["NombreCompleto"] = "Nuevo Usuario",
        ["CorreoCorporativo"] = CorreoNuevo,
        ["Rol"] = "Supervisor"
    };

    // Abre una página para conseguir el token antifalsificación, como haría el navegador, y manda
    // el POST con ese token. El formulario solo lo abre un Administrador, así que para los
    // demás roles el token se saca de otra página (rutaDelToken).
    private static async Task<HttpResponseMessage> EnviarAltaAsync(
        FabricaConSesionReal fabrica,
        string cookieDeSesion,
        Dictionary<string, string> datos,
        string rutaDelToken = RutaRegistrarUsuario,
        bool alterarToken = false)
    {
        var pagina = await PedirConCookieAsync(fabrica, rutaDelToken, cookieDeSesion);
        string html = await pagina.Content.ReadAsStringAsync();

        Match campoDelToken = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(campoDelToken.Success, $"{rutaDelToken} no incluyó el token antifalsificación.");

        string cookieAntifalsificacion = pagina.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith(NombreDeCookieAntifalsificacion, StringComparison.Ordinal))
            .Split(';')[0];

        string token = campoDelToken.Groups[1].Value;

        var formulario = new Dictionary<string, string>(datos)
        {
            ["__RequestVerificationToken"] = alterarToken ? "X" + token[1..] : token
        };

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaRegistrarUsuario)
        {
            Content = new FormUrlEncodedContent(formulario)
        };
        pedido.Headers.Add("Cookie", $"{cookieDeSesion}; {cookieAntifalsificacion}");

        return await CrearCliente(fabrica).SendAsync(pedido);
    }

    private static async Task<string> LeerHtmlDecodificadoAsync(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    private static List<string> ObtenerOpcionesDeRol(string html)
    {
        string selector = Regex.Match(html, "<select[^>]*name=\"Rol\".*?</select>", RegexOptions.Singleline).Value;

        return Regex.Matches(selector, "<option[^>]*value=\"([^\"]*)\"")
            .Select(opcion => opcion.Groups[1].Value)
            .ToList();
    }

    private static async Task<Usuario?> LeerUsuarioPorCorreoAsync(FabricaConSesionReal fabrica, string correo)
    {
        using var alcance = fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        return await contexto.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.CorreoCorporativo == correo);
    }

    private static async Task<int> ContarUsuariosConCorreoAsync(FabricaConSesionReal fabrica, string correo)
    {
        using var alcance = fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        return await contexto.Usuarios.CountAsync(u => u.CorreoCorporativo == correo);
    }

    private static async Task<int> ContarUsuariosAsync(FabricaConSesionReal fabrica)
    {
        using var alcance = fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<EopDbContext>();

        return await contexto.Usuarios.CountAsync();
    }
}
