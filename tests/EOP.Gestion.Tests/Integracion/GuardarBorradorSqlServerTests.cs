using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;
using EOP.Gestion.Tests.Compartido;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EOP.Gestion.Tests.Integracion;

// S3-T14 (US-003, RF-005): pruebas integrales contra SQL Server real del guardado explícito de
// borradores, gestión de cambios pendientes, valores históricos (DT-11), catálogo intacto,
// permisos (RF-073), estados editables (RF-002), atomicidad, errores con reintento, pérdida del
// borrador temporal, expiración de 8 horas y concurrencia real entre pestañas y usuarios.
public class GuardarBorradorSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    private int _presupuestoId;
    private int _segundoPresupuestoId;
    private int _responsableOriginalId;
    private int _cableId;
    private int _cableProveedorAId;
    private int _cableProveedorBId;
    private int _pinturaProveedorAId;

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
            Nombre = "Cliente Borrador S.A.",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };
        var responsable = new Usuario
        {
            NombreCompleto = "Operador Inicial",
            CorreoCorporativo = "inicial@eop.com.uy",
            Rol = RolUsuario.Operador,
            Activo = true
        };
        var tipoProyecto = new TipoProyecto { Nombre = "Iluminación" };
        contexto.AddRange(cliente, responsable, tipoProyecto);
        await contexto.SaveChangesAsync();

        var presupuesto = new Presupuesto
        {
            Codigo = "PRES-20261124-BORRADOR-0001",
            NumeroCorrelativo = 1,
            NombreProyecto = "Proyecto Borrador",
            ClienteId = cliente.Id,
            TipoProyectoId = tipoProyecto.Id,
            PlazoVigenciaDias = 30,
            ResponsableId = responsable.Id,
            Estado = EstadoPresupuesto.Borrador,
            FechaCreacion = new DateTime(2026, 11, 24, 12, 0, 0)
        };
        var segundoPresupuesto = new Presupuesto
        {
            Codigo = "PRES-20261124-SEGUNDO-0002",
            NumeroCorrelativo = 2,
            NombreProyecto = "Segundo Proyecto Borrador",
            ClienteId = cliente.Id,
            TipoProyectoId = tipoProyecto.Id,
            PlazoVigenciaDias = 30,
            ResponsableId = responsable.Id,
            Estado = EstadoPresupuesto.Borrador,
            FechaCreacion = new DateTime(2026, 11, 24, 12, 5, 0)
        };
        var proveedorA = DatosDePruebaProveedores.CrearProveedor("Proveedor A", "215521750017");
        var proveedorB = DatosDePruebaProveedores.CrearProveedor("Proveedor B", "210000000019");
        var cable = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        var pintura = new Material { Nombre = "Pintura blanca", Unidad = UnidadMedida.Litro };
        contexto.AddRange(presupuesto, segundoPresupuesto, proveedorA, proveedorB, cable, pintura);
        await contexto.SaveChangesAsync();

        var cableA = new MaterialProveedor
        {
            MaterialId = cable.Id,
            ProveedorId = proveedorA.Id,
            PrecioUnitario = 100m,
            Moneda = Moneda.UYU,
            FechaUltimaActualizacion = new DateTime(2026, 11, 1, 10, 0, 0)
        };
        var cableB = new MaterialProveedor
        {
            MaterialId = cable.Id,
            ProveedorId = proveedorB.Id,
            PrecioUnitario = 2.5m,
            Moneda = Moneda.USD,
            FechaUltimaActualizacion = new DateTime(2026, 11, 2, 10, 0, 0)
        };
        var pinturaA = new MaterialProveedor
        {
            MaterialId = pintura.Id,
            ProveedorId = proveedorA.Id,
            PrecioUnitario = 7.5m,
            Moneda = Moneda.USD,
            FechaUltimaActualizacion = new DateTime(2026, 11, 3, 10, 0, 0)
        };
        contexto.MaterialProveedores.AddRange(cableA, cableB, pinturaA);
        await contexto.SaveChangesAsync();

        _presupuestoId = presupuesto.Id;
        _segundoPresupuestoId = segundoPresupuesto.Id;
        _responsableOriginalId = responsable.Id;
        _cableId = cable.Id;
        _cableProveedorAId = cableA.Id;
        _cableProveedorBId = cableB.Id;
        _pinturaProveedorAId = pinturaA.Id;
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

    private EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(_cadenaConexion).Options);

    private async Task<FabricaConEntraSimulado.Navegador> IniciarSesionAsync(
        FabricaConEntraSimulado fabrica, RolUsuario rol, bool responsable = true, string? correo = null)
    {
        using EopDbContext contexto = CrearContexto();
        string objectId = Guid.NewGuid().ToString();
        string correoFinal = correo ?? $"{Guid.NewGuid():N}@eop.com.uy";
        int usuarioId = await fabrica.GuardarUsuarioAsync(correoFinal, objectId, rol);
        if (responsable)
        {
            await contexto.Presupuestos
                .Where(p => p.Id == _presupuestoId || p.Id == _segundoPresupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.ResponsableId, usuarioId));
        }

        var navegador = fabrica.CrearNavegador();
        await navegador.IniciarSesionAsync(new Dictionary<string, object>
        {
            ["sub"] = objectId,
            ["oid"] = objectId,
            ["name"] = "Usuario de prueba",
            ["preferred_username"] = correoFinal
        });
        Assert.True(navegador.TieneSesion);
        return navegador;
    }

    private sealed record EstadoFormularioPagina(
        string Token,
        string EsperabaCambiosPendientes,
        string BorradorId,
        string VersionBorrador,
        string HuellaEstadoBase,
        string Html);

    private async Task<EstadoFormularioPagina> LeerEstadoFormularioAsync(
        FabricaConEntraSimulado.Navegador navegador, int? presupuestoId = null)
    {
        int idObjetivo = presupuestoId ?? _presupuestoId;
        string html = WebUtility.HtmlDecode(
            await (await navegador.GetAsync($"/ConceptosPresupuesto/Index/{idObjetivo}")).Content.ReadAsStringAsync());

        string token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        string esperaba = Regex.Match(html, "name=\"esperabaCambiosPendientes\" value=\"([^\"]+)\"").Groups[1].Value;
        string borradorId = Regex.Match(html, "name=\"borradorId\" value=\"([^\"]*)\"").Groups[1].Value;
        string version = Regex.Match(html, "name=\"versionBorrador\" value=\"([^\"]+)\"").Groups[1].Value;
        string huella = Regex.Match(html, "name=\"huellaEstadoBase\" value=\"([^\"]*)\"").Groups[1].Value;

        return new EstadoFormularioPagina(
            token,
            string.IsNullOrEmpty(esperaba) ? "false" : esperaba,
            borradorId,
            string.IsNullOrEmpty(version) ? "0" : version,
            huella,
            html);
    }

    private async Task<HttpResponseMessage> AgregarConceptoAsync(
        FabricaConEntraSimulado.Navegador navegador,
        Dictionary<string, string> campos,
        int? presupuestoId = null,
        EstadoFormularioPagina? estadoCapturado = null)
    {
        int idObjetivo = presupuestoId ?? _presupuestoId;
        EstadoFormularioPagina estado = estadoCapturado ?? await LeerEstadoFormularioAsync(navegador, idObjetivo);

        var formulario = new Dictionary<string, string>(campos)
        {
            ["__RequestVerificationToken"] = estado.Token,
            ["esperabaCambiosPendientes"] = estado.EsperabaCambiosPendientes,
            ["borradorId"] = estado.BorradorId,
            ["versionBorrador"] = estado.VersionBorrador,
            ["huellaEstadoBase"] = estado.HuellaEstadoBase
        };
        return await navegador.PostAsync($"/ConceptosPresupuesto/Agregar/{idObjetivo}", formulario);
    }

    private async Task<HttpResponseMessage> GuardarBorradorAsync(
        FabricaConEntraSimulado.Navegador navegador,
        bool? esperabaCambiosPendientes = null,
        int? presupuestoId = null,
        EstadoFormularioPagina? estadoCapturado = null)
    {
        int idObjetivo = presupuestoId ?? _presupuestoId;
        EstadoFormularioPagina estado = estadoCapturado ?? await LeerEstadoFormularioAsync(navegador, idObjetivo);

        var formulario = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = estado.Token,
            ["esperabaCambiosPendientes"] = esperabaCambiosPendientes.HasValue
                ? (esperabaCambiosPendientes.Value ? "true" : "false")
                : estado.EsperabaCambiosPendientes,
            ["borradorId"] = estado.BorradorId,
            ["versionBorrador"] = estado.VersionBorrador,
            ["huellaEstadoBase"] = estado.HuellaEstadoBase
        };
        return await navegador.PostAsync($"/ConceptosPresupuesto/GuardarBorrador/{idObjetivo}", formulario);
    }

    private async Task<string> LeerPaginaAsync(FabricaConEntraSimulado.Navegador navegador, string ruta) =>
        WebUtility.HtmlDecode(await (await navegador.GetAsync(ruta)).Content.ReadAsStringAsync());

    private async Task<string> FotoDelCatalogoAsync()
    {
        using EopDbContext contexto = CrearContexto();
        var materiales = await contexto.Materiales.AsNoTracking().OrderBy(m => m.Id)
            .Select(m => $"M{m.Id}|{m.Nombre}|{m.Unidad}").ToListAsync();
        var referencias = await contexto.MaterialProveedores.AsNoTracking().OrderBy(r => r.Id)
            .Select(r => $"R{r.Id}|{r.MaterialId}|{r.ProveedorId}|{r.PrecioUnitario}|{r.Moneda}|{r.FechaUltimaActualizacion:O}").ToListAsync();
        var proveedores = await contexto.Proveedores.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => $"P{p.Id}|{p.Nombre}|{p.Rut}").ToListAsync();
        return string.Join("\n", materiales.Concat(referencias).Concat(proveedores));
    }

    [FactConSqlServer]
    public async Task ConfiguracionDeSesion_TieneOchoHorasDeInactividadYConservaSeguridadYAutenticacion()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        SessionOptions opcionesSesion = fabrica.Services.GetRequiredService<IOptions<SessionOptions>>().Value;
        CookieAuthenticationOptions opcionesAuth = fabrica.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(TimeSpan.FromHours(8), opcionesSesion.IdleTimeout);
        Assert.Equal(".EOP.Borrador", opcionesSesion.Cookie.Name);
        Assert.True(opcionesSesion.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, opcionesSesion.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, opcionesSesion.Cookie.SameSite);
        Assert.True(opcionesSesion.Cookie.IsEssential);

        // Las reglas de seguridad de la cookie de autenticación no fueron alteradas.
        Assert.True(opcionesAuth.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, opcionesAuth.Cookie.SecurePolicy);
        Assert.Equal("/Cuenta/AccesoDenegado", opcionesAuth.AccessDeniedPath);
    }

    [FactConSqlServer]
    public async Task CambiosPendientes_NoSePersistenAntesDeGuardarYNoAparecenAlVolverEnOtraSesion()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var sesionA = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Agregamos material, mano de obra y margen SIN presionar Guardar borrador.
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(sesionA, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MaterialId.ToString(),
            ["Entrada.ReferenciaId"] = _cableProveedorAId.ToString(),
            ["Entrada.Cantidad"] = "5"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(sesionA, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Instalación eléctrica",
            ["Entrada.Importe"] = "2500",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(sesionA, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MargenId.ToString(),
            ["Entrada.PorcentajeMargen"] = "15",
            ["Entrada.TiposBaseSeleccionados"] = TipoConcepto.MaterialId.ToString()
        })).StatusCode);

        // Durante la elaboración se ven los avances y el indicador de cambios pendientes.
        EstadoFormularioPagina estadoConPendientes = await LeerEstadoFormularioAsync(sesionA);
        Assert.Contains("data-cambios-pendientes=\"true\"", estadoConPendientes.Html);
        Assert.Contains("Cable de cobre", estadoConPendientes.Html);
        Assert.Contains("Instalación eléctrica", estadoConPendientes.Html);
        Assert.Contains("Margen", estadoConPendientes.Html);

        // Incluso podemos navegar a SeleccionarMaterial para una fila nueva pendiente (Id negativo)
        // y cambiarle el proveedor antes de guardar definitivamente.
        Match enlaceSeleccion = Regex.Match(estadoConPendientes.Html, @"conceptoId=(-\d+)");
        Assert.True(enlaceSeleccion.Success);
        string idTemporal = enlaceSeleccion.Groups[1].Value;

        string paginaSeleccion = await LeerPaginaAsync(
            sesionA, $"/ConceptosPresupuesto/SeleccionarMaterial/{_presupuestoId}?conceptoId={idTemporal}&esperabaCambiosPendientes=true");
        Assert.Contains("Referencias de precios de Cable de cobre", paginaSeleccion);
        Assert.Contains("data-cambios-pendientes=\"true\"", paginaSeleccion);

        var cambioProveedor = await sesionA.PostAsync(
            $"/ConceptosPresupuesto/CambiarProveedor/{_presupuestoId}?conceptoId={idTemporal}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = estadoConPendientes.Token,
                ["referenciaId"] = _cableProveedorBId.ToString(),
                ["esperabaCambiosPendientes"] = estadoConPendientes.EsperabaCambiosPendientes,
                ["borradorId"] = estadoConPendientes.BorradorId,
                ["versionBorrador"] = estadoConPendientes.VersionBorrador,
                ["huellaEstadoBase"] = estadoConPendientes.HuellaEstadoBase
            });
        Assert.Equal(HttpStatusCode.Redirect, cambioProveedor.StatusCode);

        // SQL Server sigue completamente vacío: nada se persistió sin presionar Guardar borrador.
        using (EopDbContext contexto = CrearContexto())
        {
            Assert.Empty(await contexto.ConceptosPresupuesto.ToListAsync());
            Assert.Empty(await contexto.MargenesBaseTipo.ToListAsync());
        }

        // Si se vuelve posteriormente en una nueva sesión (abandonando sin guardar), solo se ven los
        // datos efectivamente guardados (ninguno) y no hay falsas advertencias.
        var nuevaSesion = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor, responsable: false);
        string paginaNuevaSesion = await LeerPaginaAsync(nuevaSesion, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Todavía no hay conceptos registrados.", paginaNuevaSesion);
        Assert.Contains("data-cambios-pendientes=\"false\"", paginaNuevaSesion);
        Assert.DoesNotContain(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, paginaNuevaSesion);
    }

    [FactConSqlServer]
    public async Task EdicionNuevaSinCambiosPendientes_NoMuestraFalsasAdvertencias()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string paginaInicial = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.DoesNotContain("mensaje-error", paginaInicial);
        Assert.DoesNotContain(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, paginaInicial);
        Assert.DoesNotContain(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, paginaInicial);

        var respuesta = await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Primera edición limpia",
            ["Entrada.Importe"] = "1800",
            ["Entrada.Moneda"] = "UYU"
        });
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        string paginaTrasAgregar = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Primera edición limpia", paginaTrasAgregar);
        Assert.DoesNotContain(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, paginaTrasAgregar);
    }

    [FactConSqlServer]
    public async Task PerdidaDelBorradorAlGuardarOAlContinuarEditando_RechazaRecuperaUltimoGuardadoEInforma()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Guardamos primero un concepto base en SQL Server.
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Guardado previamente",
            ["Entrada.Importe"] = "1000",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        // Ahora agregamos un cambio pendiente SIN guardar y capturamos el estado de esa pantalla.
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MaterialId.ToString(),
            ["Entrada.ReferenciaId"] = _cableProveedorAId.ToString(),
            ["Entrada.Cantidad"] = "3"
        })).StatusCode);

        EstadoFormularioPagina pantallaAntesDePerdida = await LeerEstadoFormularioAsync(navegador);
        Assert.Equal("true", pantallaAntesDePerdida.EsperabaCambiosPendientes);
        Assert.Equal("1", pantallaAntesDePerdida.VersionBorrador);
        Match idTemporalMatch = Regex.Match(pantallaAntesDePerdida.Html, @"conceptoId=(-\d+)");
        Assert.True(idTemporalMatch.Success);
        string idTemporal = idTemporalMatch.Groups[1].Value;

        // Simulamos la pérdida del almacenamiento temporal (la sesión de autenticación sigue activa).
        navegador.EliminarCookie(".EOP.Borrador");

        // 1) Intentar continuar editando (Agregar) rechaza la operación y NO crea un borrador nuevo.
        var intentoAgregar = await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
            ["Entrada.Descripcion"] = "No debe agregarse",
            ["Entrada.Importe"] = "900",
            ["Entrada.Moneda"] = "UYU"
        }, estadoCapturado: pantallaAntesDePerdida);
        Assert.Equal(HttpStatusCode.Redirect, intentoAgregar.StatusCode);

        string paginaTrasAgregarFallido = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, paginaTrasAgregarFallido);
        Assert.Contains("Guardado previamente", paginaTrasAgregarFallido);
        Assert.DoesNotContain("No debe agregarse", paginaTrasAgregarFallido);
        Assert.DoesNotContain("RAM", paginaTrasAgregarFallido);
        Assert.DoesNotContain("reinicio", paginaTrasAgregarFallido);
        Assert.Contains("data-cambios-pendientes=\"false\"", paginaTrasAgregarFallido);

        // 2) Intentar guardar con la pantalla que esperaba cambios pendientes también informa la pérdida.
        var intentoGuardar = await GuardarBorradorAsync(navegador, estadoCapturado: pantallaAntesDePerdida);
        Assert.Equal(HttpStatusCode.Redirect, intentoGuardar.StatusCode);
        string paginaTrasGuardarFallido = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, paginaTrasGuardarFallido);

        // 3) Intentar navegar a SeleccionarMaterial con Id temporal o indicador de cambios pendientes
        // también detecta la pérdida y devuelve al usuario al último guardado.
        var intentoNavegar = await navegador.GetAsync(
            $"/ConceptosPresupuesto/SeleccionarMaterial/{_presupuestoId}?conceptoId={idTemporal}&esperabaCambiosPendientes=true");
        Assert.Equal(HttpStatusCode.Redirect, intentoNavegar.StatusCode);
        string paginaTrasNavegar = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, paginaTrasNavegar);

        // SQL Server conserva intacto exactamente el último guardado.
        using EopDbContext contexto = CrearContexto();
        ConceptoPresupuesto persistido = await contexto.ConceptosPresupuesto.SingleAsync();
        Assert.Equal("Guardado previamente", persistido.Concepto);
        Assert.Equal(1000m, persistido.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task GuardarBorrador_PersisteTodoYSeRecuperaAlVolverPosteriormente()
    {
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MaterialId.ToString(),
            ["Entrada.ReferenciaId"] = _cableProveedorAId.ToString(),
            ["Entrada.Cantidad"] = "12,5"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Cableado",
            ["Entrada.Importe"] = "4000",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MargenId.ToString(),
            ["Entrada.PorcentajeMargen"] = "18,5",
            ["Entrada.TiposBaseSeleccionados"] = TipoConcepto.ManoDeObraId.ToString()
        })).StatusCode);

        // Guardamos explícitamente.
        var respuestaGuardar = await GuardarBorradorAsync(navegador);
        Assert.Equal(HttpStatusCode.Redirect, respuestaGuardar.StatusCode);

        string paginaTrasGuardar = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("El borrador se guardó correctamente.", paginaTrasGuardar);
        Assert.Contains("data-cambios-pendientes=\"false\"", paginaTrasGuardar);
        Assert.Contains("Elaboración", paginaTrasGuardar);
        Assert.Contains("Resumen", paginaTrasGuardar);
        Assert.Contains("Documentos", paginaTrasGuardar);
        Assert.Contains("Revisión interna", paginaTrasGuardar);

        // Al regresar en una sesión nueva se recuperan los datos efectivamente guardados.
        var sesionPosterior = await IniciarSesionAsync(fabrica, RolUsuario.Administrador, responsable: false);
        string paginaRecuperada = await LeerPaginaAsync(sesionPosterior, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Cable de cobre", paginaRecuperada);
        Assert.Contains("Proveedor A", paginaRecuperada);
        Assert.Contains("Cableado", paginaRecuperada);
        Assert.Contains("Margen", paginaRecuperada);

        using EopDbContext contexto = CrearContexto();
        List<ConceptoPresupuesto> conceptos = await contexto.ConceptosPresupuesto
            .Where(c => c.PresupuestoId == _presupuestoId)
            .OrderBy(c => c.TipoConceptoId)
            .ToListAsync();
        Assert.Equal(3, conceptos.Count);

        ConceptoPresupuesto material = conceptos.Single(c => c.TipoConceptoId == TipoConcepto.MaterialId);
        Assert.Equal(_cableId, material.MaterialId);
        Assert.Equal(_cableProveedorAId, material.MaterialProveedorId);
        Assert.Equal("Cable de cobre", material.Concepto);
        Assert.Equal(UnidadMedida.Metro, material.Unidad);
        Assert.Equal(100m, material.PrecioUnitario);
        Assert.Equal(Moneda.UYU, material.Moneda);
        Assert.Equal(12.5m, material.Cantidad);

        ConceptoPresupuesto margen = conceptos.Single(c => c.TipoConceptoId == TipoConcepto.MargenId);
        Assert.Equal(18.5m, margen.PorcentajeMargen);
        List<int> bases = await contexto.MargenesBaseTipo
            .Where(b => b.ConceptoPresupuestoId == margen.Id)
            .Select(b => b.TipoConceptoId)
            .ToListAsync();
        Assert.Equal([TipoConcepto.ManoDeObraId], bases);

        // El catálogo permanece intacto.
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [TheoryConSqlServer]
    [InlineData(EstadoPresupuesto.Borrador, RolUsuario.Operador, true)]
    [InlineData(EstadoPresupuesto.ParaRevisionInterna, RolUsuario.Supervisor, false)]
    [InlineData(EstadoPresupuesto.SolicitudDeModificacionDelCliente, RolUsuario.Administrador, false)]
    public async Task GuardarBorrador_NoCreaOtroPresupuestoNiAlteraCodigoResponsableNiEstado(
        EstadoPresupuesto estado, RolUsuario rol, bool esResponsable)
    {
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.Estado, estado));
        }

        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable: esResponsable);

        Presupuesto antes;
        using (EopDbContext contexto = CrearContexto())
        {
            antes = await contexto.Presupuestos.AsNoTracking().SingleAsync(p => p.Id == _presupuestoId);
        }

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
            ["Entrada.Descripcion"] = "Andamio",
            ["Entrada.Importe"] = "500",
            ["Entrada.Moneda"] = "USD"
        });

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        using EopDbContext verificacion = CrearContexto();
        Assert.Equal(2, await verificacion.Presupuestos.CountAsync());
        Presupuesto despues = await verificacion.Presupuestos.AsNoTracking().SingleAsync(p => p.Id == _presupuestoId);
        Assert.Equal(antes.Codigo, despues.Codigo);
        Assert.Equal(antes.NumeroCorrelativo, despues.NumeroCorrelativo);
        Assert.Equal(antes.ResponsableId, despues.ResponsableId);
        Assert.Equal(estado, despues.Estado);
        Assert.Equal(antes.FechaCreacion, despues.FechaCreacion);
        Assert.Equal(antes.NombreProyecto, despues.NombreProyecto);
        Assert.Equal(antes.ClienteId, despues.ClienteId);
        Assert.Equal(antes.TipoProyectoId, despues.TipoProyectoId);
        Assert.Equal(antes.PlazoVigenciaDias, despues.PlazoVigenciaDias);
    }

    [FactConSqlServer]
    public async Task GuardarBorradorNuevamente_TrasCambiosEnElCatalogo_ConservaValoresHistoricosDeFilasNoModificadas()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.MaterialId.ToString(),
            ["Entrada.ReferenciaId"] = _cableProveedorAId.ToString(),
            ["Entrada.Cantidad"] = "10"
        });
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        int idOriginalFila;
        using (EopDbContext contexto = CrearContexto())
        {
            idOriginalFila = await contexto.ConceptosPresupuesto.Select(c => c.Id).SingleAsync();

            await contexto.Materiales.Where(m => m.Id == _cableId)
                .ExecuteUpdateAsync(c => c
                    .SetProperty(m => m.Nombre, "Cable modificado en catálogo")
                    .SetProperty(m => m.Unidad, UnidadMedida.Unidad));
            await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorAId)
                .ExecuteUpdateAsync(c => c
                    .SetProperty(r => r.PrecioUnitario, 999m)
                    .SetProperty(r => r.Moneda, Moneda.USD));
        }

        string catalogoDespuesDelCambio = await FotoDelCatalogoAsync();

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.TrasladoId.ToString(),
            ["Entrada.Descripcion"] = "Flete",
            ["Entrada.Importe"] = "800",
            ["Entrada.Moneda"] = "UYU"
        });
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        using EopDbContext lectura = CrearContexto();
        ConceptoPresupuesto filaHistorica = await lectura.ConceptosPresupuesto
            .SingleAsync(c => c.Id == idOriginalFila);

        Assert.Equal("Cable de cobre", filaHistorica.Concepto);
        Assert.Equal(UnidadMedida.Metro, filaHistorica.Unidad);
        Assert.Equal(100m, filaHistorica.PrecioUnitario);
        Assert.Equal(Moneda.UYU, filaHistorica.Moneda);
        Assert.Equal(catalogoDespuesDelCambio, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task GuardadosRepetidos_NoGeneranDuplicados()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Mano de obra",
            ["Entrada.Importe"] = "1500",
            ["Entrada.Moneda"] = "UYU"
        });

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        using EopDbContext contexto = CrearContexto();
        Assert.Equal(1, await contexto.ConceptosPresupuesto.CountAsync(c => c.PresupuestoId == _presupuestoId));
    }

    [FactConSqlServer]
    public async Task GuardarBorrador_SinTokenAntifalsificacion_SeRechaza()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await navegador.PostAsync(
            $"/ConceptosPresupuesto/GuardarBorrador/{_presupuestoId}",
            new Dictionary<string, string> { ["esperabaCambiosPendientes"] = "false" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [FactConSqlServer]
    public async Task SiElPresupuestoDejaDeSerEditableAntesDeGuardar_ElServidorRechazaElGuardadoSinPersistir()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Trabajo pendiente",
            ["Entrada.Importe"] = "3000",
            ["Entrada.Moneda"] = "UYU"
        });
        EstadoFormularioPagina estadoAntes = await LeerEstadoFormularioAsync(navegador);

        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.Estado, EstadoPresupuesto.Aprobado));
        }

        var respuesta = await GuardarBorradorAsync(navegador, estadoCapturado: estadoAntes);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());

        using EopDbContext lectura = CrearContexto();
        Assert.Empty(await lectura.ConceptosPresupuesto.ToListAsync());
    }

    [FactConSqlServer]
    public async Task IntercambioDeProveedoresEntreFilasEnElMismoBorrador_SeGuardaSinFalsoDuplicado()
    {
        int fila1Id;
        int fila2Id;
        using (EopDbContext contexto = CrearContexto())
        {
            var cable = await contexto.Materiales.SingleAsync(m => m.Id == _cableId);
            var refA = await contexto.MaterialProveedores.SingleAsync(r => r.Id == _cableProveedorAId);
            var refB = await contexto.MaterialProveedores.SingleAsync(r => r.Id == _cableProveedorBId);

            var fila1 = new ConceptoPresupuesto { PresupuestoId = _presupuestoId, TipoConceptoId = TipoConcepto.MaterialId, Cantidad = 2m };
            fila1.CopiarReferencia(cable, refA);
            var fila2 = new ConceptoPresupuesto { PresupuestoId = _presupuestoId, TipoConceptoId = TipoConcepto.MaterialId, Cantidad = 4m };
            fila2.CopiarReferencia(cable, refB);

            contexto.ConceptosPresupuesto.AddRange(fila1, fila2);
            await contexto.SaveChangesAsync();
            fila1Id = fila1.Id;
            fila2Id = fila2.Id;
        }

        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        EstadoFormularioPagina estado1 = await LeerEstadoFormularioAsync(navegador);
        var asignar1 = await navegador.PostAsync(
            $"/ConceptosPresupuesto/AsignarMaterial/{_presupuestoId}?conceptoId={fila1Id}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = estado1.Token,
                ["referenciaId"] = _pinturaProveedorAId.ToString(),
                ["busqueda"] = "pintura",
                ["esperabaCambiosPendientes"] = estado1.EsperabaCambiosPendientes,
                ["borradorId"] = estado1.BorradorId,
                ["versionBorrador"] = estado1.VersionBorrador,
                ["huellaEstadoBase"] = estado1.HuellaEstadoBase
            });
        Assert.Equal(HttpStatusCode.Redirect, asignar1.StatusCode);

        EstadoFormularioPagina estado2 = await LeerEstadoFormularioAsync(navegador);
        var cambiar2 = await navegador.PostAsync(
            $"/ConceptosPresupuesto/CambiarProveedor/{_presupuestoId}?conceptoId={fila2Id}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = estado2.Token,
                ["referenciaId"] = _cableProveedorAId.ToString(),
                ["esperabaCambiosPendientes"] = estado2.EsperabaCambiosPendientes,
                ["borradorId"] = estado2.BorradorId,
                ["versionBorrador"] = estado2.VersionBorrador,
                ["huellaEstadoBase"] = estado2.HuellaEstadoBase
            });
        Assert.Equal(HttpStatusCode.Redirect, cambiar2.StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        using EopDbContext lectura = CrearContexto();
        Assert.Equal(_pinturaProveedorAId, (await lectura.ConceptosPresupuesto.SingleAsync(c => c.Id == fila1Id)).MaterialProveedorId);
        Assert.Equal(_cableProveedorAId, (await lectura.ConceptosPresupuesto.SingleAsync(c => c.Id == fila2Id)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task ErrorEnPersistenciaDeUnConcepto_RevierteTodaLaTransaccionSinCambiosParciales()
    {
        using EopDbContext contexto = CrearContexto();
        Usuario responsable = await contexto.Usuarios.SingleAsync(u => u.Id == _responsableOriginalId);
        var repositorio = new RepositorioConceptosPresupuesto(contexto);

        BorradorPresupuestoDto borrador = (await repositorio.ObtenerBorradorBaseAsync(_presupuestoId, CancellationToken.None))!;
        borrador.Conceptos.Add(new ConceptoBorradorDto
        {
            Id = borrador.ObtenerSiguienteIdTemporal(),
            TipoConceptoId = TipoConcepto.ManoDeObraId,
            TipoNombre = "Mano de obra",
            Concepto = "Válido que no debe quedar a medias",
            PrecioUnitario = 1000m,
            Moneda = Moneda.UYU,
            EsNuevo = true
        });
        borrador.Conceptos.Add(new ConceptoBorradorDto
        {
            Id = borrador.ObtenerSiguienteIdTemporal(),
            TipoConceptoId = TipoConcepto.MaterialId,
            TipoNombre = "Material",
            Concepto = "Inexistente",
            MaterialId = _cableId,
            MaterialProveedorId = 999999,
            Cantidad = 1m,
            Unidad = UnidadMedida.Metro,
            PrecioUnitario = 10m,
            Moneda = Moneda.UYU,
            EsNuevo = true
        });

        ResultadoPersistenciaBorrador resultado = await repositorio.GuardarBorradorSiPuedeEditarAsync(
            borrador, responsable, CancellationToken.None);

        Assert.Equal(ResultadoGuardarBorrador.ReferenciaInexistente, resultado.Estado);
        using EopDbContext lectura = CrearContexto();
        Assert.Empty(await lectura.ConceptosPresupuesto.ToListAsync());
    }

    [FactConSqlServer]
    public async Task AnteErrorInesperadoAlGuardar_InformaYConservaCambiosPendientesParaReintentar()
    {
        var estadoFallo = new ControlDeFalloTransitorio { FallarProximoGuardado = true };

        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion, servicios =>
        {
            servicios.RemoveAll<IRepositorioConceptosPresupuesto>();
            servicios.AddSingleton(estadoFallo);
            servicios.AddScoped<RepositorioConceptosPresupuesto>();
            servicios.AddScoped<IRepositorioConceptosPresupuesto>(sp =>
                new RepositorioConFalloTransitorio(
                    sp.GetRequiredService<RepositorioConceptosPresupuesto>(),
                    sp.GetRequiredService<ControlDeFalloTransitorio>()));
        });

        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await AgregarConceptoAsync(navegador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Reintento seguro",
            ["Entrada.Importe"] = "2200",
            ["Entrada.Moneda"] = "UYU"
        });

        var primerIntento = await GuardarBorradorAsync(navegador);
        Assert.Equal(HttpStatusCode.Redirect, primerIntento.StatusCode);

        string paginaConError = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("No se pudo guardar el borrador", paginaConError);
        Assert.Contains("Reintento seguro", paginaConError);
        Assert.Contains("data-cambios-pendientes=\"true\"", paginaConError);

        using (EopDbContext contexto = CrearContexto())
        {
            Assert.Empty(await contexto.ConceptosPresupuesto.ToListAsync());
        }

        var reintento = await GuardarBorradorAsync(navegador);
        Assert.Equal(HttpStatusCode.Redirect, reintento.StatusCode);

        string paginaGuardada = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("El borrador se guardó correctamente.", paginaGuardada);
        Assert.Contains("data-cambios-pendientes=\"false\"", paginaGuardada);

        using EopDbContext verificacion = CrearContexto();
        ConceptoPresupuesto guardado = await verificacion.ConceptosPresupuesto.SingleAsync();
        Assert.Equal("Reintento seguro", guardado.Concepto);
        Assert.Equal(2200m, guardado.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task DosPestanasEditandoElMismoBorradorEnParalelo_UnaGanaYLaOtraSeRechazaSinPerdidaSilenciosa()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var pestana1 = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Establecemos la sesión con un primer concepto pendiente (versión 1) y abrimos una segunda pestaña.
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(pestana1, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Concepto inicial",
            ["Entrada.Importe"] = "1000",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        var pestana2 = pestana1.ClonarPestana();

        // Ambas pestañas cargan la misma versión 1 antes de enviar en paralelo.
        EstadoFormularioPagina estado1 = await LeerEstadoFormularioAsync(pestana1);
        EstadoFormularioPagina estado2 = await LeerEstadoFormularioAsync(pestana2);
        Assert.Equal("1", estado1.VersionBorrador);
        Assert.Equal("1", estado2.VersionBorrador);

        var envios = await Task.WhenAll(
            AgregarConceptoAsync(pestana1, new()
            {
                ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
                ["Entrada.Descripcion"] = "Cambio Pestaña 1",
                ["Entrada.Importe"] = "200",
                ["Entrada.Moneda"] = "USD"
            }, estadoCapturado: estado1),
            AgregarConceptoAsync(pestana2, new()
            {
                ["Entrada.TipoId"] = TipoConcepto.TrasladoId.ToString(),
                ["Entrada.Descripcion"] = "Cambio Pestaña 2",
                ["Entrada.Importe"] = "300",
                ["Entrada.Moneda"] = "UYU"
            }, estadoCapturado: estado2));

        Assert.All(envios, respuesta => Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode));

        // Leemos los mensajes recibidos en cada pestaña: exactamente una aplicó y la otra recibió rechazo.
        string pagina1 = await LeerPaginaAsync(pestana1, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        string pagina2 = await LeerPaginaAsync(pestana2, $"/ConceptosPresupuesto/Index/{_presupuestoId}");

        string[] paginas = [pagina1, pagina2];
        Assert.Single(paginas, p => p.Contains("El concepto se agregó al borrador."));
        Assert.Single(paginas, p => p.Contains(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana));

        // El concepto inicial y el ganador siguen presentes (versión 2) sin pérdidas silenciosas.
        EstadoFormularioPagina estadoFinal = await LeerEstadoFormularioAsync(pestana1);
        Assert.Equal("2", estadoFinal.VersionBorrador);
        Assert.Contains("Concepto inicial", estadoFinal.Html);
        bool gano1 = estadoFinal.Html.Contains("Cambio Pestaña 1");
        bool gano2 = estadoFinal.Html.Contains("Cambio Pestaña 2");
        Assert.True(gano1 ^ gano2);
    }

    [FactConSqlServer]
    public async Task DosPestanasEditandoDistintosPresupuestosDeLaMismaSesionEnParalelo_ConservanAmbosBorradores()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var pestana1 = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Inicializamos la cookie de sesión compartida con un cambio en el primer presupuesto.
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(pestana1, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Base P1",
            ["Entrada.Importe"] = "500",
            ["Entrada.Moneda"] = "UYU"
        }, presupuestoId: _presupuestoId)).StatusCode);

        var pestana2 = pestana1.ClonarPestana();

        EstadoFormularioPagina estadoP1 = await LeerEstadoFormularioAsync(pestana1, _presupuestoId);
        EstadoFormularioPagina estadoP2 = await LeerEstadoFormularioAsync(pestana2, _segundoPresupuestoId);

        // Ejecutamos simultáneamente modificaciones sobre dos presupuestos distintos de la MISMA sesión.
        var respuestas = await Task.WhenAll(
            AgregarConceptoAsync(pestana1, new()
            {
                ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
                ["Entrada.Descripcion"] = "Concurrente P1",
                ["Entrada.Importe"] = "1100",
                ["Entrada.Moneda"] = "UYU"
            }, presupuestoId: _presupuestoId, estadoCapturado: estadoP1),
            AgregarConceptoAsync(pestana2, new()
            {
                ["Entrada.TipoId"] = TipoConcepto.TrasladoId.ToString(),
                ["Entrada.Descripcion"] = "Concurrente P2",
                ["Entrada.Importe"] = "2200",
                ["Entrada.Moneda"] = "USD"
            }, presupuestoId: _segundoPresupuestoId, estadoCapturado: estadoP2));

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.Redirect, r.StatusCode));

        // Ambos presupuestos conservan intactos sus cambios pendientes en la sesión compartida.
        string paginaP1 = await LeerPaginaAsync(pestana1, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        string paginaP2 = await LeerPaginaAsync(pestana2, $"/ConceptosPresupuesto/Index/{_segundoPresupuestoId}");
        Assert.Contains("Base P1", paginaP1);
        Assert.Contains("Concurrente P1", paginaP1);
        Assert.Contains("Concurrente P2", paginaP2);

        // Al guardar ambos borradores, los dos persisten en SQL Server sin perder datos.
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(pestana1, presupuestoId: _presupuestoId)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(pestana2, presupuestoId: _segundoPresupuestoId)).StatusCode);

        using EopDbContext contexto = CrearContexto();
        Assert.Equal(2, await contexto.ConceptosPresupuesto.CountAsync(c => c.PresupuestoId == _presupuestoId));
        Assert.Equal(1, await contexto.ConceptosPresupuesto.CountAsync(c => c.PresupuestoId == _segundoPresupuestoId));
    }

    [FactConSqlServer]
    public async Task ModificacionConcurrenteConGuardadoDefinitivo_SeSincronizaSinCorrupcionNiPerdida()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var pestanaGuardar = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(pestanaGuardar, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Trabajo inicial",
            ["Entrada.Importe"] = "1500",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        var pestanaEditar = pestanaGuardar.ClonarPestana();
        EstadoFormularioPagina estadoGuardar = await LeerEstadoFormularioAsync(pestanaGuardar);
        EstadoFormularioPagina estadoEditar = await LeerEstadoFormularioAsync(pestanaEditar);

        var respuestas = await Task.WhenAll(
            GuardarBorradorAsync(pestanaGuardar, estadoCapturado: estadoGuardar),
            AgregarConceptoAsync(pestanaEditar, new()
            {
                ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
                ["Entrada.Descripcion"] = "Cambio simultáneo",
                ["Entrada.Importe"] = "400",
                ["Entrada.Moneda"] = "USD"
            }, estadoCapturado: estadoEditar));

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.Redirect, r.StatusCode));

        string paginaGuardar = await LeerPaginaAsync(pestanaGuardar, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        string paginaEditar = await LeerPaginaAsync(pestanaEditar, $"/ConceptosPresupuesto/Index/{_presupuestoId}");

        using EopDbContext contexto = CrearContexto();
        int cantidadEnBase = await contexto.ConceptosPresupuesto.CountAsync(c => c.PresupuestoId == _presupuestoId);

        if (cantidadEnBase == 1)
        {
            // Ganó GuardarBorrador: la edición simultánea recibió conflicto de otra pestaña.
            Assert.Contains("El borrador se guardó correctamente.", paginaGuardar);
            Assert.Contains(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, paginaEditar);
        }
        else
        {
            // Ganó Agregar: GuardarBorrador se rechazó por versión desactualizada y ambos cambios siguen en sesión.
            Assert.Equal(0, cantidadEnBase);
            Assert.Contains("El concepto se agregó al borrador.", paginaEditar);
            Assert.Contains(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, paginaGuardar);
            Assert.Contains("Trabajo inicial", paginaEditar);
            Assert.Contains("Cambio simultáneo", paginaEditar);
        }
    }

    [FactConSqlServer]
    public async Task PantallaDesactualizadaDespuesDeGuardarEnOtraPestana_EsRechazadaConMensajeDeOtraPestana()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var pestana1 = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Creamos sesión compartida con un cambio inicial y lo guardamos más adelante desde pestaña 1.
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(pestana1, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Guardado en pestaña 1",
            ["Entrada.Importe"] = "1000",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        var pestana2 = pestana1.ClonarPestana();
        EstadoFormularioPagina estadoViejoPestana2 = await LeerEstadoFormularioAsync(pestana2);

        // Pestaña 1 guarda definitivamente el borrador.
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(pestana1)).StatusCode);

        // Pestaña 2 intenta agregar usando su pantalla vieja (versión 1): se rechaza como otra pestaña.
        var intento = await AgregarConceptoAsync(pestana2, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
            ["Entrada.Descripcion"] = "Desactualizado",
            ["Entrada.Importe"] = "200",
            ["Entrada.Moneda"] = "UYU"
        }, estadoCapturado: estadoViejoPestana2);
        Assert.Equal(HttpStatusCode.Redirect, intento.StatusCode);

        string pagina = await LeerPaginaAsync(pestana2, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, pagina);
        Assert.Contains("Guardado en pestaña 1", pagina);
        Assert.DoesNotContain("Desactualizado", pagina);
    }

    [FactConSqlServer]
    public async Task PestanaAntiguaConVersion1_NoPuedeEditarNiGuardarUnNuevoBorradorQueTambienEstaEnVersion1()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var pestana1 = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // 1) Pestaña 1 crea el primer borrador (versión 1, BorradorId = A).
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(pestana1, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Concepto borrador 1",
            ["Entrada.Importe"] = "1000",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        // 2) Pestaña 2 queda abierta viendo el primer borrador en versión 1.
        var pestana2 = pestana1.ClonarPestana();
        EstadoFormularioPagina estadoAntiguoPestana2 = await LeerEstadoFormularioAsync(pestana2);
        Assert.Equal("1", estadoAntiguoPestana2.VersionBorrador);
        Assert.False(string.IsNullOrWhiteSpace(estadoAntiguoPestana2.BorradorId));

        // 3) Pestaña 1 guarda el primer borrador e inicia un SEGUNDO borrador nuevo (vuelve a versión 1,
        // pero con un BorradorId distinto).
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(pestana1)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await AgregarConceptoAsync(pestana1, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
            ["Entrada.Descripcion"] = "Concepto borrador 2",
            ["Entrada.Importe"] = "2000",
            ["Entrada.Moneda"] = "UYU"
        })).StatusCode);

        EstadoFormularioPagina estadoNuevoBorrador = await LeerEstadoFormularioAsync(pestana1);
        Assert.Equal("1", estadoNuevoBorrador.VersionBorrador);
        Assert.NotEqual(estadoAntiguoPestana2.BorradorId, estadoNuevoBorrador.BorradorId);

        // 4) Pestaña 2 (antigua, también con versión 1) intenta EDITAR: debe rechazarse.
        var intentoEditar = await AgregarConceptoAsync(pestana2, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.TrasladoId.ToString(),
            ["Entrada.Descripcion"] = "Edición desde pestaña vieja",
            ["Entrada.Importe"] = "300",
            ["Entrada.Moneda"] = "UYU"
        }, estadoCapturado: estadoAntiguoPestana2);
        Assert.Equal(HttpStatusCode.Redirect, intentoEditar.StatusCode);

        string paginaTrasEditar = await LeerPaginaAsync(pestana2, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, paginaTrasEditar);
        Assert.DoesNotContain("Edición desde pestaña vieja", paginaTrasEditar);
        Assert.Contains("Concepto borrador 2", paginaTrasEditar);

        // 5) Pestaña 2 (antigua, también con versión 1) intenta GUARDAR: debe rechazarse sin persistir el borrador 2.
        var intentoGuardar = await GuardarBorradorAsync(pestana2, estadoCapturado: estadoAntiguoPestana2);
        Assert.Equal(HttpStatusCode.Redirect, intentoGuardar.StatusCode);

        string paginaTrasGuardar = await LeerPaginaAsync(pestana2, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, paginaTrasGuardar);
        Assert.Contains("Concepto borrador 2", paginaTrasGuardar);
        Assert.Contains("data-cambios-pendientes=\"true\"", paginaTrasGuardar);

        // En SQL Server solo sigue guardado el concepto del borrador 1; el borrador 2 no se guardó por error.
        using EopDbContext contexto = CrearContexto();
        ConceptoPresupuesto unicoPersistido = await contexto.ConceptosPresupuesto.SingleAsync();
        Assert.Equal("Concepto borrador 1", unicoPersistido.Concepto);
    }

    [FactConSqlServer]
    public async Task DosUsuariosEditandoElMismoPresupuesto_SeAislanYElSegundoRecibeConflictoConservandoSusPendientes()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var operador = await IniciarSesionAsync(fabrica, RolUsuario.Operador, responsable: true, correo: "op@eop.com.uy");
        var supervisor = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor, responsable: false, correo: "sup@eop.com.uy");

        await AgregarConceptoAsync(operador, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.ManoDeObraId.ToString(),
            ["Entrada.Descripcion"] = "Cambio del operador",
            ["Entrada.Importe"] = "1000",
            ["Entrada.Moneda"] = "UYU"
        });
        await AgregarConceptoAsync(supervisor, new()
        {
            ["Entrada.TipoId"] = TipoConcepto.AlquilerId.ToString(),
            ["Entrada.Descripcion"] = "Cambio del supervisor",
            ["Entrada.Importe"] = "500",
            ["Entrada.Moneda"] = "USD"
        });

        // Aislamiento: cada usuario ve únicamente sus propios cambios pendientes.
        string paginaOperadorAntes = await LeerPaginaAsync(operador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        string paginaSupervisorAntes = await LeerPaginaAsync(supervisor, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Cambio del operador", paginaOperadorAntes);
        Assert.DoesNotContain("Cambio del supervisor", paginaOperadorAntes);
        Assert.Contains("Cambio del supervisor", paginaSupervisorAntes);
        Assert.DoesNotContain("Cambio del operador", paginaSupervisorAntes);

        // El operador guarda primero y tiene éxito.
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(operador)).StatusCode);

        // El supervisor intenta guardar después: recibe el mensaje específico de modificación por otro usuario.
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(supervisor)).StatusCode);
        string paginaSupervisor = await LeerPaginaAsync(supervisor, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains(MensajesBorradorPresupuesto.PresupuestoModificadoConPendientesConservados, paginaSupervisor);
        Assert.DoesNotContain(MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, paginaSupervisor);
        // Sus cambios pendientes siguen conservados.
        Assert.Contains("Cambio del supervisor", paginaSupervisor);

        using EopDbContext contexto = CrearContexto();
        ConceptoPresupuesto unicoGuardado = await contexto.ConceptosPresupuesto.SingleAsync();
        Assert.Equal("Cambio del operador", unicoGuardado.Concepto);
    }

    [FactConSqlServer]
    public async Task SiLaSesionPerdioLosCambiosPendientes_GuardarInformaErrorEnLugarDeConfirmarExito()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await GuardarBorradorAsync(navegador, esperabaCambiosPendientes: true);
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        string pagina = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains(MensajesBorradorPresupuesto.CambiosPendientesPerdidos, pagina);
        Assert.DoesNotContain("El borrador se guardó correctamente.", pagina);
    }

    private sealed class ControlDeFalloTransitorio
    {
        public bool FallarProximoGuardado { get; set; }
    }

    private sealed class RepositorioConFalloTransitorio(
        IRepositorioConceptosPresupuesto interno,
        ControlDeFalloTransitorio control) : IRepositorioConceptosPresupuesto
    {
        public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken) =>
            interno.ObtenerPresupuestoAsync(presupuestoId, cancellationToken);

        public Task<bool> PuedeEditarEnBaseAsync(int presupuestoId, Usuario usuario, CancellationToken cancellationToken) =>
            interno.PuedeEditarEnBaseAsync(presupuestoId, usuario, cancellationToken);

        public Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(int presupuestoId, CancellationToken cancellationToken) =>
            interno.ObtenerBorradorBaseAsync(presupuestoId, cancellationToken);

        public Task<string?> ObtenerHuellaPersistidaAsync(int presupuestoId, CancellationToken cancellationToken) =>
            interno.ObtenerHuellaPersistidaAsync(presupuestoId, cancellationToken);

        public Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(int referenciaId, CancellationToken cancellationToken) =>
            interno.ObtenerReferenciaCatalogoAsync(referenciaId, cancellationToken);

        public Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken) =>
            interno.ObtenerNombreTipoConceptoAsync(tipoId, cancellationToken);

        public Task<ResultadoPersistenciaBorrador> GuardarBorradorSiPuedeEditarAsync(
            BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken)
        {
            if (control.FallarProximoGuardado)
            {
                control.FallarProximoGuardado = false;
                throw new InvalidOperationException("Fallo simulado de persistencia.");
            }

            return interno.GuardarBorradorSiPuedeEditarAsync(borrador, usuario, cancellationToken);
        }
    }
}
