using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S3-T15 (US-003, US-042, RF-005, RF-069): pruebas de punta a punta contra SQL Server real de la
// navegación libre con cambios sin guardar, el indicador, el descarte voluntario y el cierre de
// sesión con uno o varios presupuestos pendientes.
public class GestionCambiosPendientesSqlServerTests : IAsyncLifetime
{
    private const string TextoIndicador = "Tenés cambios sin guardar.";
    private const string MarcaDialogoCierre = "data-cambios-pendientes-usuario";
    private const string CodigoPrimero = "PRES-20261125-PRIMERO-0001";
    private const string CodigoSegundo = "PRES-20261125-SEGUNDO-0002";

    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    private int _primerPresupuestoId;
    private int _segundoPresupuestoId;
    private int _proveedorId;

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();

        var cliente = new Cliente
        {
            Nombre = "Cliente Pendientes S.A.",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };
        var responsableInicial = new Usuario
        {
            NombreCompleto = "Responsable inicial",
            CorreoCorporativo = "inicial-pendientes@eop.com.uy",
            Rol = RolUsuario.Operador,
            Activo = true
        };
        var tipoProyecto = new TipoProyecto { Nombre = "Iluminación" };
        var proveedor = DatosDePruebaProveedores.CrearProveedor("Proveedor Pendientes", "210000000019");
        contexto.AddRange(cliente, responsableInicial, tipoProyecto, proveedor);
        await contexto.SaveChangesAsync();

        var primero = CrearPresupuesto(CodigoPrimero, 1, "Proyecto Primero", cliente.Id, tipoProyecto.Id, responsableInicial.Id);
        var segundo = CrearPresupuesto(CodigoSegundo, 2, "Proyecto Segundo", cliente.Id, tipoProyecto.Id, responsableInicial.Id);
        contexto.Presupuestos.AddRange(primero, segundo);
        await contexto.SaveChangesAsync();

        _primerPresupuestoId = primero.Id;
        _segundoPresupuestoId = segundo.Id;
        _proveedorId = proveedor.Id;
    }

    public async Task DisposeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    private static Presupuesto CrearPresupuesto(string codigo, int correlativo, string nombre, int clienteId, int tipoId, int responsableId) => new()
    {
        Codigo = codigo,
        NumeroCorrelativo = correlativo,
        NombreProyecto = nombre,
        ClienteId = clienteId,
        TipoProyectoId = tipoId,
        PlazoVigenciaDias = 30,
        ResponsableId = responsableId,
        Estado = EstadoPresupuesto.Borrador,
        FechaCreacion = new DateTime(2026, 11, 25, 12, 0, 0)
    };

    private EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(_cadenaConexion).Options);

    // ---------- Ayudas ----------

    private sealed record SesionDePrueba(FabricaConEntraSimulado.Navegador Navegador, Dictionary<string, object> Claims, int UsuarioId);

    private async Task<SesionDePrueba> IniciarSesionAsync(
        FabricaConEntraSimulado fabrica, RolUsuario rol, bool responsableDeAmbos = true, FabricaConEntraSimulado.Navegador? navegadorExistente = null)
    {
        string objectId = Guid.NewGuid().ToString();
        string correo = $"{Guid.NewGuid():N}@eop.com.uy";
        int usuarioId = await fabrica.GuardarUsuarioAsync(correo, objectId, rol);

        if (responsableDeAmbos)
        {
            using EopDbContext contexto = CrearContexto();
            await contexto.Presupuestos
                .Where(p => p.Id == _primerPresupuestoId || p.Id == _segundoPresupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.ResponsableId, usuarioId));
        }

        var claims = new Dictionary<string, object>
        {
            ["sub"] = objectId,
            ["oid"] = objectId,
            ["name"] = "Usuario de prueba",
            ["preferred_username"] = correo
        };

        var navegador = navegadorExistente ?? fabrica.CrearNavegador();
        await navegador.IniciarSesionAsync(claims);
        Assert.True(navegador.TieneSesion);
        return new SesionDePrueba(navegador, claims, usuarioId);
    }

    private static async Task<string> LeerPaginaAsync(FabricaConEntraSimulado.Navegador navegador, string ruta)
    {
        HttpResponseMessage respuesta = await navegador.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
    }

    private static string Token(string html) =>
        Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static string CampoOculto(string html, string nombre) =>
        Regex.Match(html, $"name=\"{nombre}\" value=\"([^\"]*)\"").Groups[1].Value;

    private string RutaConceptos(int presupuestoId) => $"/ConceptosPresupuesto/Index/{presupuestoId}";

    private async Task<HttpResponseMessage> AgregarManoDeObraAsync(
        FabricaConEntraSimulado.Navegador navegador, int presupuestoId, string descripcion, string? htmlCapturado = null)
    {
        string html = htmlCapturado ?? await LeerPaginaAsync(navegador, RutaConceptos(presupuestoId));
        string esperaba = CampoOculto(html, "esperabaCambiosPendientes");
        string version = CampoOculto(html, "versionBorrador");

        return await navegador.PostAsync($"/ConceptosPresupuesto/Agregar/{presupuestoId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html),
            ["esperabaCambiosPendientes"] = string.IsNullOrEmpty(esperaba) ? "false" : esperaba,
            ["borradorId"] = CampoOculto(html, "borradorId"),
            ["versionBorrador"] = string.IsNullOrEmpty(version) ? "0" : version,
            ["huellaEstadoBase"] = CampoOculto(html, "huellaEstadoBase"),
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = descripcion,
            ["Entrada.Importe"] = "1500",
            ["Entrada.Moneda"] = "UYU"
        });
    }

    private async Task<HttpResponseMessage> GuardarBorradorAsync(FabricaConEntraSimulado.Navegador navegador, int presupuestoId)
    {
        string html = await LeerPaginaAsync(navegador, RutaConceptos(presupuestoId));
        return await navegador.PostAsync($"/ConceptosPresupuesto/GuardarBorrador/{presupuestoId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html),
            ["esperabaCambiosPendientes"] = CampoOculto(html, "esperabaCambiosPendientes"),
            ["borradorId"] = CampoOculto(html, "borradorId"),
            ["versionBorrador"] = CampoOculto(html, "versionBorrador"),
            ["huellaEstadoBase"] = CampoOculto(html, "huellaEstadoBase")
        });
    }

    private static Task<HttpResponseMessage> DescartarAsync(FabricaConEntraSimulado.Navegador navegador, int presupuestoId, string htmlCapturado) =>
        navegador.PostAsync($"/ConceptosPresupuesto/DescartarCambios/{presupuestoId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(htmlCapturado),
            ["borradorId"] = CampoOculto(htmlCapturado, "borradorId"),
            ["versionBorrador"] = CampoOculto(htmlCapturado, "versionBorrador")
        });

    // Arma el envío del formulario de la advertencia de cierre de sesión tal como lo mandaría el
    // navegador: el token y, por cada presupuesto, el identificador y la versión del borrador.
    private static Dictionary<string, string> FormularioDecisionCierre(string html, string? decision)
    {
        var formulario = new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(html) };

        string formularioDecision = Regex.Match(html, "<form[^>]*data-formulario-decision-cierre.*?</form>", RegexOptions.Singleline).Value;
        Assert.NotEmpty(formularioDecision);

        foreach (Match campo in Regex.Matches(formularioDecision, "name=\"(borradores\\[\\d+\\]\\.\\w+)\" value=\"([^\"]*)\""))
        {
            formulario[campo.Groups[1].Value] = campo.Groups[2].Value;
        }

        if (decision != null)
        {
            formulario["decision"] = decision;
        }

        return formulario;
    }

    private static void AssertCerroSesionEnEopYEntra(HttpResponseMessage respuesta, FabricaConEntraSimulado.Navegador navegador)
    {
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("login.microsoftonline.com", respuesta.Headers.Location!.Host);
        Assert.False(navegador.TieneSesion);
    }

    private static void AssertNoCerroSesion(HttpResponseMessage respuesta, FabricaConEntraSimulado.Navegador navegador)
    {
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/CambiosPendientes", respuesta.Headers.Location?.OriginalString);
        Assert.True(navegador.TieneSesion);
    }

    private async Task<List<string>> ConceptosGuardadosAsync(int presupuestoId)
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.ConceptosPresupuesto.AsNoTracking()
            .Where(c => c.PresupuestoId == presupuestoId)
            .OrderBy(c => c.Id)
            .Select(c => c.Concepto!)
            .ToListAsync();
    }

    // ---------- Navegación libre ----------

    [FactConSqlServer]
    public async Task NavegarPorOtrosModulosYRegistrarUnMaterial_ConservaLosCambiosSinGuardarlosEnLaBase()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var sesion = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor);
        var navegador = sesion.Navegador;

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Instalación pendiente")).StatusCode);

        // Recorremos otros módulos: ninguno obliga a guardar o descartar (responden 200 sin
        // redirigir) y en todos está el diálogo de cierre de sesión listo por si se lo necesita.
        foreach (string ruta in new[] { "/", "/Materiales", "/Materiales/Registrar", "/Proveedores", "/Proveedores/Registrar", "/Clientes", "/Presupuestos", RutaConceptos(_segundoPresupuestoId) })
        {
            string pagina = await LeerPaginaAsync(navegador, ruta);
            Assert.Contains(MarcaDialogoCierre, pagina);
            Assert.Contains(CodigoPrimero, pagina);
            Assert.Contains("cambios-pendientes.js", pagina);
        }

        // UI-presupuestos: la dirección vieja del detalle tampoco frena nada; solo lleva a la
        // pantalla unificada del presupuesto.
        HttpResponseMessage detalle = await navegador.GetAsync($"/Presupuestos/Detalle/{_primerPresupuestoId}");
        Assert.Equal(HttpStatusCode.Redirect, detalle.StatusCode);
        Assert.Equal(RutaConceptos(_primerPresupuestoId), detalle.Headers.Location?.OriginalString);

        // Registramos un material nuevo en el catálogo en medio de la elaboración.
        string formularioMaterial = await LeerPaginaAsync(navegador, "/Materiales/Registrar");
        HttpResponseMessage registro = await navegador.PostAsync("/Materiales/Registrar", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(formularioMaterial),
            ["Nombre"] = "Luminaria LED nueva",
            ["Unidad"] = UnidadMedida.Unidad.ToString(),
            ["PrecioUnitario"] = "150",
            ["Moneda"] = "UYU",
            ["ProveedorId"] = _proveedorId.ToString()
        });
        Assert.Equal(HttpStatusCode.Redirect, registro.StatusCode);

        // Nada del borrador llegó a SQL Server.
        Assert.Empty(await ConceptosGuardadosAsync(_primerPresupuestoId));

        // Al volver (y al recargar) el avance sigue ahí, con el indicador y el botón de descarte.
        for (int recarga = 0; recarga < 2; recarga++)
        {
            string conceptos = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
            Assert.Contains("Instalación pendiente", conceptos);
            Assert.Contains(TextoIndicador, conceptos);
            Assert.Contains("Tus cambios se conservan temporalmente. Guardá el borrador para conservarlos definitivamente.", conceptos);
            Assert.Contains("data-bs-target=\"#confirmarDescartarCambios\"", conceptos);
            Assert.Contains(">Guardar borrador<", conceptos);
        }

        // Y el material recién registrado ya se puede buscar y agregar al mismo borrador.
        // UI-presupuestos: la búsqueda la hace el selector del modal, sin recargar la pantalla.
        string busqueda = await LeerPaginaAsync(navegador,
            $"/ConceptosPresupuesto/CatalogoMateriales/{_primerPresupuestoId}?busqueda=Luminaria");
        Assert.Contains("Luminaria LED nueva", busqueda);
        Assert.Contains("Instalación pendiente", await LeerPaginaAsync(navegador,
            $"{RutaConceptos(_primerPresupuestoId)}?esperabaCambiosPendientes=true"));
    }

    [FactConSqlServer]
    public async Task VariosPresupuestos_ConservanCadaUnoSuAvanceSinMezclarse()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;

        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Solo del primero");
        await AgregarManoDeObraAsync(navegador, _segundoPresupuestoId, "Solo del segundo");

        string primero = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        string segundo = await LeerPaginaAsync(navegador, RutaConceptos(_segundoPresupuestoId));

        Assert.Contains("Solo del primero", primero);
        Assert.DoesNotContain("Solo del segundo", primero);
        Assert.Contains("Solo del segundo", segundo);
        Assert.DoesNotContain("Solo del primero", segundo);

        // La advertencia de cierre de sesión lista los dos presupuestos.
        string inicio = await LeerPaginaAsync(navegador, "/");
        Assert.Contains(CodigoPrimero, inicio);
        Assert.Contains(CodigoSegundo, inicio);
        Assert.Contains("Tenés cambios sin guardar en 2 presupuestos", inicio);
    }

    [FactConSqlServer]
    public async Task OtroUsuario_NoVeNiRecuperaLosCambiosTemporalesAjenosNiSiquieraEnElMismoNavegador()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var sesionA = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor);
        await AgregarManoDeObraAsync(sesionA.Navegador, _primerPresupuestoId, "Cambio privado de A");

        // Otro usuario en otro navegador.
        var sesionB = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor, responsableDeAmbos: false);
        string vistaB = await LeerPaginaAsync(sesionB.Navegador, RutaConceptos(_primerPresupuestoId));
        Assert.DoesNotContain("Cambio privado de A", vistaB);
        Assert.DoesNotContain(TextoIndicador, vistaB);
        Assert.DoesNotContain(MarcaDialogoCierre, vistaB);

        // Otra identidad que inicia sesión en el MISMO navegador: vence la autenticación de A pero
        // queda su cookie de sesión temporal (.EOP.Borrador), que la nueva identidad comparte.
        foreach (string cookieDeAutenticacion in sesionA.Navegador.CookiesActuales.Split("; ")
                     .Select(cookie => cookie.Split('=')[0])
                     .Where(nombre => nombre.StartsWith(".AspNetCore.Cookies", StringComparison.Ordinal)))
        {
            sesionA.Navegador.EliminarCookie(cookieDeAutenticacion);
        }

        Assert.Contains(".EOP.Borrador", sesionA.Navegador.CookiesActuales);
        var sesionC = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor, responsableDeAmbos: false, navegadorExistente: sesionA.Navegador);
        string vistaC = await LeerPaginaAsync(sesionC.Navegador, RutaConceptos(_primerPresupuestoId));
        Assert.DoesNotContain("Cambio privado de A", vistaC);
        Assert.DoesNotContain(MarcaDialogoCierre, vistaC);

        // Ni siquiera descartando a ciegas con el identificador de A se tocan sus cambios.
        string vistaA = await LeerPaginaAsync(sesionA.Navegador, RutaConceptos(_primerPresupuestoId));
        HttpResponseMessage descarteAjeno = await sesionB.Navegador.PostAsync($"/ConceptosPresupuesto/DescartarCambios/{_primerPresupuestoId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(vistaB),
            ["borradorId"] = CampoOculto(vistaA, "borradorId"),
            ["versionBorrador"] = CampoOculto(vistaA, "versionBorrador")
        });
        Assert.Equal(HttpStatusCode.Redirect, descarteAjeno.StatusCode);
        Assert.Contains(DescartarCambiosPresupuesto.MensajeSinCambios, await LeerPaginaAsync(sesionB.Navegador, RutaConceptos(_primerPresupuestoId)));

        // Y el cierre de sesión de B no ve pendientes: cierra normalmente.
        string inicioB = await LeerPaginaAsync(sesionB.Navegador, "/");
        HttpResponseMessage cierreB = await sesionB.Navegador.PostAsync("/Cuenta/CerrarSesion", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(inicioB)
        });
        AssertCerroSesionEnEopYEntra(cierreB, sesionB.Navegador);
    }

    // ---------- Indicador, guardado y fallo ----------

    [FactConSqlServer]
    public async Task Indicador_DesapareceTrasGuardarYSeMantieneSiElGuardadoFalla()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;

        // Sin cambios no hay indicador ni botón de descarte.
        string inicial = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.DoesNotContain(TextoIndicador, inicial);
        Assert.DoesNotContain("#confirmarDescartarCambios", inicial);

        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Primer guardado");
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador, _primerPresupuestoId)).StatusCode);

        string trasGuardar = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.DoesNotContain(TextoIndicador, trasGuardar);
        Assert.DoesNotContain(MarcaDialogoCierre, trasGuardar);
        Assert.Equal(["Primer guardado"], await ConceptosGuardadosAsync(_primerPresupuestoId));

        // Nuevo cambio pendiente y, mientras tanto, otro usuario guarda el mismo presupuesto.
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Cambio que choca");
        var otro = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor, responsableDeAmbos: false);
        await AgregarManoDeObraAsync(otro.Navegador, _primerPresupuestoId, "Guardado por otro");
        await GuardarBorradorAsync(otro.Navegador, _primerPresupuestoId);

        // El guardado falla por conflicto: se informa, los cambios siguen y el indicador también.
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador, _primerPresupuestoId)).StatusCode);
        string trasFallo = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.Contains(MensajesBorradorPresupuesto.PresupuestoModificadoConPendientesConservados, trasFallo);
        Assert.Contains("Cambio que choca", trasFallo);
        Assert.Contains(TextoIndicador, trasFallo);
        Assert.DoesNotContain("Cambio que choca", await ConceptosGuardadosAsync(_primerPresupuestoId));
    }

    [FactConSqlServer]
    public async Task FormularioDevueltoConErrores_QuedaMarcadoComoDatosNoEnviados()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;

        // UI-presupuestos: «Agregar concepto» es un modal. Arranca cerrado y sin datos, y trae su
        // propio paso de confirmación para no perder lo escrito (sin abrir otro modal encima).
        string limpio = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.Contains("data-datos-no-enviados=\"false\"", limpio);
        Assert.Contains("data-abrir-al-cargar=\"false\"", limpio);
        Assert.Contains("data-paso-modal=\"confirmar-salida\"", limpio);
        Assert.Contains("Tenés datos escritos que todavía no agregaste", limpio);

        HttpResponseMessage rechazado = await navegador.PostAsync($"/ConceptosPresupuesto/Agregar/{_primerPresupuestoId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(limpio),
            ["esperabaCambiosPendientes"] = "false",
            ["versionBorrador"] = "0",
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Importe mal escrito",
            ["Entrada.Importe"] = "no-es-un-numero",
            ["Entrada.Moneda"] = "UYU"
        });

        Assert.Equal(HttpStatusCode.OK, rechazado.StatusCode);
        string html = WebUtility.HtmlDecode(await rechazado.Content.ReadAsStringAsync());
        Assert.Contains("data-datos-no-enviados=\"true\"", html);
        Assert.DoesNotContain(TextoIndicador, html);

        // El modal vuelve abierto, con el tipo elegido y lo escrito, y con el error a la vista.
        Assert.Contains("data-abrir-al-cargar=\"true\"", html);
        Assert.Matches($@"<option value=""{TipoConcepto.ManoDeObraId}"" data-seccion=""importe"" selected=""selected""", html);
        Assert.Contains("value=\"Importe mal escrito\"", html);
        Assert.Contains("value=\"no-es-un-numero\"", html);
        Assert.Contains("Ingresá un importe válido", html);
    }

    // ---------- Descarte voluntario ----------

    [FactConSqlServer]
    public async Task Descartar_EliminaSoloLosCambiosDeEsePresupuestoYConservaLoGuardado()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;

        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Ya guardado");
        await GuardarBorradorAsync(navegador, _primerPresupuestoId);
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Para descartar");
        await AgregarManoDeObraAsync(navegador, _segundoPresupuestoId, "Del otro presupuesto");

        string pantalla = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.Contains($"¿Descartar los cambios sin guardar de {CodigoPrimero}?", pantalla);

        Assert.Equal(HttpStatusCode.Redirect, (await DescartarAsync(navegador, _primerPresupuestoId, pantalla)).StatusCode);

        string trasDescartar = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.Contains(DescartarCambiosPresupuesto.MensajeDescartado, trasDescartar);
        Assert.Contains("Ya guardado", trasDescartar);
        Assert.DoesNotContain("Para descartar", trasDescartar);
        Assert.DoesNotContain(TextoIndicador, trasDescartar);
        Assert.Equal(["Ya guardado"], await ConceptosGuardadosAsync(_primerPresupuestoId));

        // El otro presupuesto conserva su avance.
        Assert.Contains("Del otro presupuesto", await LeerPaginaAsync(navegador, RutaConceptos(_segundoPresupuestoId)));
        Assert.Empty(await ConceptosGuardadosAsync(_segundoPresupuestoId));
    }

    [FactConSqlServer]
    public async Task Descartar_DesdeUnaPestanaDesactualizada_NoBorraLosCambiosQueEsaPestanaNoVio()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var pestanaA = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;

        await AgregarManoDeObraAsync(pestanaA, _primerPresupuestoId, "Visto por A");
        string pantallaVieja = await LeerPaginaAsync(pestanaA, RutaConceptos(_primerPresupuestoId));

        var pestanaB = pestanaA.ClonarPestana();
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarManoDeObraAsync(pestanaB, _primerPresupuestoId, "Agregado en B")).StatusCode);

        await DescartarAsync(pestanaA, _primerPresupuestoId, pantallaVieja);

        string resultado = await LeerPaginaAsync(pestanaA, RutaConceptos(_primerPresupuestoId));
        Assert.Contains(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, resultado);
        Assert.Contains("Visto por A", resultado);
        Assert.Contains("Agregado en B", resultado);
        Assert.Contains(TextoIndicador, resultado);

        // Y si B descarta con la pantalla vigente, la pestaña vieja que intenta agregar se entera.
        string pantallaB = await LeerPaginaAsync(pestanaB, RutaConceptos(_primerPresupuestoId));
        await DescartarAsync(pestanaB, _primerPresupuestoId, pantallaB);
        await AgregarManoDeObraAsync(pestanaA, _primerPresupuestoId, "No debe entrar", htmlCapturado: pantallaB);
        string trasIntento = await LeerPaginaAsync(pestanaA, RutaConceptos(_primerPresupuestoId));
        Assert.Contains(MensajesBorradorPresupuesto.CambiosDescartadosEnOtraPestana, trasIntento);
        Assert.DoesNotContain("No debe entrar", trasIntento);
    }

    [FactConSqlServer]
    public async Task Descartar_SinTokenAntifalsificacion_SeRechazaYNoTocaNada()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Protegido");
        string pantalla = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));

        HttpResponseMessage respuesta = await navegador.PostAsync($"/ConceptosPresupuesto/DescartarCambios/{_primerPresupuestoId}", new Dictionary<string, string>
        {
            ["borradorId"] = CampoOculto(pantalla, "borradorId"),
            ["versionBorrador"] = CampoOculto(pantalla, "versionBorrador")
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Protegido", await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId)));
    }

    [FactConSqlServer]
    public async Task OperadorNoResponsable_NoVeIndicadorNiDescarteYNoPuedeGuardar()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Operador, responsableDeAmbos: false)).Navegador;

        string pantalla = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.DoesNotContain(TextoIndicador, pantalla);
        Assert.DoesNotContain("#confirmarDescartarCambios", pantalla);
        Assert.DoesNotContain(">Guardar borrador<", pantalla);

        HttpResponseMessage agregar = await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Sin permiso", htmlCapturado: pantalla);
        Assert.Equal(HttpStatusCode.Redirect, agregar.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", agregar.Headers.Location?.AbsolutePath);
        Assert.DoesNotContain(MarcaDialogoCierre, await LeerPaginaAsync(navegador, "/"));
        Assert.Empty(await ConceptosGuardadosAsync(_primerPresupuestoId));
    }

    // ---------- Cierre de sesión ----------

    [FactConSqlServer]
    public async Task CerrarSesion_SinDecisionYConPendientes_NoCierraYMuestraLaPantallaDeDecision()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Pendiente al salir");

        // Desde otro módulo, sin JavaScript: el formulario del menú manda solo el token.
        string clientes = await LeerPaginaAsync(navegador, "/Clientes");
        HttpResponseMessage cierre = await navegador.PostAsync("/Cuenta/CerrarSesion", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(clientes)
        });

        AssertNoCerroSesion(cierre, navegador);

        string decision = await LeerPaginaAsync(navegador, "/Cuenta/CambiosPendientes");
        Assert.Contains("¿Cerrar sesión con cambios sin guardar?", decision);
        Assert.Contains(CodigoPrimero, decision);
        Assert.Contains("Guardar y cerrar sesión", decision);
        Assert.Contains("Continuar editando", decision);

        // "Continuar editando" no cambia nada: los cambios siguen disponibles.
        Assert.Contains("Pendiente al salir", await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId)));

        // El paso de confirmación del descarte también funciona sin JavaScript.
        string confirmar = await LeerPaginaAsync(navegador, "/Cuenta/CambiosPendientes?confirmarDescarte=true");
        Assert.Contains("¿Descartar los cambios sin guardar de 1 presupuesto?", confirmar);
    }

    [FactConSqlServer]
    public async Task CerrarSesion_GuardandoVariosPresupuestosDesdeOtroModulo_GuardaTodoYCierra()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var sesion = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor);
        var navegador = sesion.Navegador;
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Guardar al salir 1");
        await AgregarManoDeObraAsync(navegador, _segundoPresupuestoId, "Guardar al salir 2");

        string proveedores = await LeerPaginaAsync(navegador, "/Proveedores");
        HttpResponseMessage cierre = await navegador.PostAsync("/Cuenta/CerrarSesion",
            FormularioDecisionCierre(proveedores, "guardar"));

        AssertCerroSesionEnEopYEntra(cierre, navegador);
        Assert.Equal(["Guardar al salir 1"], await ConceptosGuardadosAsync(_primerPresupuestoId));
        Assert.Equal(["Guardar al salir 2"], await ConceptosGuardadosAsync(_segundoPresupuestoId));
    }

    [FactConSqlServer]
    public async Task CerrarSesion_SiUnGuardadoFalla_NoCierraInformaCadaPresupuestoYLuegoSePuedeDescartar()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var sesion = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor);
        var navegador = sesion.Navegador;
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Se guarda bien");
        await AgregarManoDeObraAsync(navegador, _segundoPresupuestoId, "No se puede guardar");

        // Mientras tanto el segundo presupuesto deja de ser editable.
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _segundoPresupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.Estado, EstadoPresupuesto.EnviadoPendienteDeRespuestaDelCliente));
        }

        string inicio = await LeerPaginaAsync(navegador, "/");
        HttpResponseMessage intento = await navegador.PostAsync("/Cuenta/CerrarSesion", FormularioDecisionCierre(inicio, "guardar"));

        AssertNoCerroSesion(intento, navegador);
        string resultado = await LeerPaginaAsync(navegador, "/Cuenta/CambiosPendientes");
        Assert.Contains("No se cerró la sesión porque algunos cambios no se pudieron guardar", resultado);
        Assert.Contains(ResolverCambiosPendientesAlCerrarSesion.MensajeGuardado, resultado);
        Assert.Contains("El presupuesto ya no está disponible para edición.", resultado);
        Assert.Equal(["Se guarda bien"], await ConceptosGuardadosAsync(_primerPresupuestoId));
        Assert.Empty(await ConceptosGuardadosAsync(_segundoPresupuestoId));

        // Solo queda pendiente el segundo. El usuario decide descartarlo y ahí sí se cierra.
        string pantallaDecision = await LeerPaginaAsync(navegador, "/Cuenta/CambiosPendientes");
        Assert.DoesNotContain($"borradores[1]", pantallaDecision);
        HttpResponseMessage descarte = await navegador.PostAsync("/Cuenta/CerrarSesion", FormularioDecisionCierre(pantallaDecision, "descartar"));
        AssertCerroSesionEnEopYEntra(descarte, navegador);
        Assert.Empty(await ConceptosGuardadosAsync(_segundoPresupuestoId));
    }

    [FactConSqlServer]
    public async Task CerrarSesion_Descartando_NoModificaLoGuardadoYNoDejaCambiosParaElProximoIngreso()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var sesion = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor);
        var navegador = sesion.Navegador;
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Guardado antes");
        await GuardarBorradorAsync(navegador, _primerPresupuestoId);
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Descartado al salir");
        await AgregarManoDeObraAsync(navegador, _segundoPresupuestoId, "También descartado");

        string inicio = await LeerPaginaAsync(navegador, "/");
        AssertCerroSesionEnEopYEntra(
            await navegador.PostAsync("/Cuenta/CerrarSesion", FormularioDecisionCierre(inicio, "descartar")), navegador);

        Assert.Equal(["Guardado antes"], await ConceptosGuardadosAsync(_primerPresupuestoId));
        Assert.Empty(await ConceptosGuardadosAsync(_segundoPresupuestoId));

        // El mismo usuario vuelve a entrar en el mismo navegador: no quedó nada temporal.
        await navegador.IniciarSesionAsync(sesion.Claims);
        string primero = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.DoesNotContain("Descartado al salir", primero);
        Assert.DoesNotContain(TextoIndicador, primero);
        Assert.DoesNotContain(MarcaDialogoCierre, primero);
    }

    [FactConSqlServer]
    public async Task CerrarSesion_ConLaAdvertenciaDesactualizada_NoGuardaNiDescartaNada()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Primera versión");
        string advertenciaVieja = await LeerPaginaAsync(navegador, "/");

        // Otra pestaña sigue trabajando y además abre un segundo presupuesto.
        var otraPestana = navegador.ClonarPestana();
        await AgregarManoDeObraAsync(otraPestana, _primerPresupuestoId, "Segunda versión");
        await AgregarManoDeObraAsync(otraPestana, _segundoPresupuestoId, "Presupuesto nuevo");

        foreach (string decision in new[] { "guardar", "descartar" })
        {
            HttpResponseMessage intento = await navegador.PostAsync("/Cuenta/CerrarSesion", FormularioDecisionCierre(advertenciaVieja, decision));
            AssertNoCerroSesion(intento, navegador);
            Assert.Contains(ResolverCambiosPendientesAlCerrarSesion.MensajeListaDesactualizada,
                await LeerPaginaAsync(navegador, "/Cuenta/CambiosPendientes"));
        }

        Assert.Empty(await ConceptosGuardadosAsync(_primerPresupuestoId));
        Assert.Empty(await ConceptosGuardadosAsync(_segundoPresupuestoId));
        string primero = await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId));
        Assert.Contains("Primera versión", primero);
        Assert.Contains("Segunda versión", primero);
        Assert.Contains("Presupuesto nuevo", await LeerPaginaAsync(navegador, RutaConceptos(_segundoPresupuestoId)));
    }

    [FactConSqlServer]
    public async Task CerrarSesion_DespuesDePerderLaSesionTemporal_CierraNormalmenteSinAdvertencia()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "Se pierde con la sesión");

        // Simulamos que expiró o se perdió el almacenamiento temporal (sigue la autenticación).
        navegador.EliminarCookie(".EOP.Borrador");

        string inicio = await LeerPaginaAsync(navegador, "/");
        Assert.DoesNotContain(MarcaDialogoCierre, inicio);

        // Si la pantalla venía de un borrador con cambios, se informa la pérdida (S3-T14).
        HttpResponseMessage regreso = await navegador.GetAsync($"{RutaConceptos(_primerPresupuestoId)}?esperabaCambiosPendientes=true");
        Assert.Equal(HttpStatusCode.Redirect, regreso.StatusCode);
        Assert.Contains(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId)));

        AssertCerroSesionEnEopYEntra(
            await navegador.PostAsync("/Cuenta/CerrarSesion", new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(inicio) }),
            navegador);
    }

    [FactConSqlServer]
    public async Task CerrarSesion_ConDecisionPeroSinToken_SeRechaza()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = (await IniciarSesionAsync(fabrica, RolUsuario.Supervisor)).Navegador;
        await AgregarManoDeObraAsync(navegador, _primerPresupuestoId, "No se descarta por CSRF");
        string inicio = await LeerPaginaAsync(navegador, "/");

        Dictionary<string, string> formulario = FormularioDecisionCierre(inicio, "descartar");
        formulario.Remove("__RequestVerificationToken");

        HttpResponseMessage respuesta = await navegador.PostAsync("/Cuenta/CerrarSesion", formulario);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(navegador.TieneSesion);
        Assert.Contains("No se descarta por CSRF", await LeerPaginaAsync(navegador, RutaConceptos(_primerPresupuestoId)));
    }
}
