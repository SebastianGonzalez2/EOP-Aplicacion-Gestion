using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S3-T09 (US-007): buscar y seleccionar a mano el material de una fila Material ya existente,
// contra un SQL Server real. Cubre RF-12 y RF-13: la selección es expresa, no toca el catálogo,
// conserva Concepto detectado, respeta permisos y estado, y no admite el mismo material-proveedor
// en dos filas (RF-20).
public class SeleccionarMaterialSqlServerTests : IAsyncLifetime
{
    private const string TextoDetectadoPorIa = "cable de cobre 2,5 mm (texto de la IA)";

    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    private int _presupuestoId;
    private int _otroPresupuestoId;
    private int _cableProveedorAId;
    private int _cableProveedorBId;
    private int _pinturaProveedorAId;
    private int _filaSinMaterialId;
    private int _filaConPinturaId;
    private int _filaManoDeObraId;
    private int _filaDeOtroPresupuestoId;

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
            Nombre = "Cliente de prueba",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };
        var responsable = new Usuario
        {
            NombreCompleto = "Ana Pereira",
            CorreoCorporativo = "ana@eop.com.uy",
            Rol = RolUsuario.Operador,
            Activo = true
        };
        var tipoProyecto = new TipoProyecto { Nombre = "Tipo de prueba" };
        contexto.AddRange(cliente, responsable, tipoProyecto);
        await contexto.SaveChangesAsync();

        Presupuesto CrearPresupuesto(string codigo, int correlativo) => new()
        {
            Codigo = codigo,
            NumeroCorrelativo = correlativo,
            NombreProyecto = "Obra",
            ClienteId = cliente.Id,
            TipoProyectoId = tipoProyecto.Id,
            PlazoVigenciaDias = 30,
            ResponsableId = responsable.Id,
            FechaCreacion = new DateTime(2026, 11, 18, 12, 0, 0)
        };
        var presupuesto = CrearPresupuesto("PRES-20261118-OBRA-0001", 1);
        var otroPresupuesto = CrearPresupuesto("PRES-20261118-OTRA-0002", 2);
        var proveedorA = DatosDePruebaProveedores.CrearProveedor("Proveedor A", "215521750017");
        var proveedorB = DatosDePruebaProveedores.CrearProveedor("Proveedor B", "210000000019");
        var cable = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        var pintura = new Material { Nombre = "Pintura blanca", Unidad = UnidadMedida.Litro };
        contexto.AddRange(presupuesto, otroPresupuesto, proveedorA, proveedorB, cable, pintura);
        await contexto.SaveChangesAsync();

        MaterialProveedor CrearReferencia(Material material, Proveedor proveedor, decimal precio, Moneda moneda) => new()
        {
            MaterialId = material.Id,
            ProveedorId = proveedor.Id,
            PrecioUnitario = precio,
            Moneda = moneda,
            FechaUltimaActualizacion = new DateTime(2026, 11, 1, 10, 0, 0)
        };
        var cableA = CrearReferencia(cable, proveedorA, 100m, Moneda.UYU);
        var cableB = CrearReferencia(cable, proveedorB, 2.5m, Moneda.USD);
        var pinturaA = CrearReferencia(pintura, proveedorA, 7.5m, Moneda.USD);
        contexto.MaterialProveedores.AddRange(cableA, cableB, pinturaA);
        await contexto.SaveChangesAsync();

        // Una fila como la que va a generar el análisis con IA (US-006): tipo Material, con el
        // texto detectado y la cantidad, pero sin material elegido todavía.
        var filaSinMaterial = new ConceptoPresupuesto
        {
            PresupuestoId = presupuesto.Id,
            TipoConceptoId = TipoConcepto.MaterialId,
            ConceptoDetectado = TextoDetectadoPorIa,
            Cantidad = 30m
        };
        var filaConPintura = new ConceptoPresupuesto
        {
            PresupuestoId = presupuesto.Id,
            TipoConceptoId = TipoConcepto.MaterialId,
            ConceptoDetectado = "pintura para paredes",
            Cantidad = 4m
        };
        filaConPintura.CopiarReferencia(pintura, pinturaA);
        var filaManoDeObra = new ConceptoPresupuesto
        {
            PresupuestoId = presupuesto.Id,
            TipoConceptoId = TipoConcepto.ManoDeObraId,
            Concepto = "Mano de obra",
            PrecioUnitario = 1500m,
            Moneda = Moneda.UYU
        };
        var filaDeOtroPresupuesto = new ConceptoPresupuesto
        {
            PresupuestoId = otroPresupuesto.Id,
            TipoConceptoId = TipoConcepto.MaterialId,
            ConceptoDetectado = "material de otro presupuesto",
            Cantidad = 1m
        };
        contexto.ConceptosPresupuesto.AddRange(filaSinMaterial, filaConPintura, filaManoDeObra, filaDeOtroPresupuesto);
        await contexto.SaveChangesAsync();

        _presupuestoId = presupuesto.Id;
        _otroPresupuestoId = otroPresupuesto.Id;
        _cableProveedorAId = cableA.Id;
        _cableProveedorBId = cableB.Id;
        _pinturaProveedorAId = pinturaA.Id;
        _filaSinMaterialId = filaSinMaterial.Id;
        _filaConPinturaId = filaConPintura.Id;
        _filaManoDeObraId = filaManoDeObra.Id;
        _filaDeOtroPresupuestoId = filaDeOtroPresupuesto.Id;
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
        FabricaConEntraSimulado fabrica, RolUsuario rol, bool responsable = true)
    {
        using EopDbContext contexto = CrearContexto();
        string objectId = Guid.NewGuid().ToString();
        string correo = "seleccion@eop.com.uy";
        int usuarioId = await fabrica.GuardarUsuarioAsync(correo, objectId, rol);
        if (responsable)
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(p => p.ResponsableId, usuarioId));
        }

        var navegador = fabrica.CrearNavegador();
        await navegador.IniciarSesionAsync(new Dictionary<string, object>
        {
            ["sub"] = objectId,
            ["oid"] = objectId,
            ["name"] = "Usuario de prueba",
            ["preferred_username"] = correo
        });
        Assert.True(navegador.TieneSesion);
        return navegador;
    }

    private string RutaSeleccion(int conceptoId, string? busqueda = null, int? presupuestoId = null)
    {
        string ruta = $"/ConceptosPresupuesto/SeleccionarMaterial/{presupuestoId ?? _presupuestoId}?conceptoId={conceptoId}";
        return busqueda == null ? ruta : $"{ruta}&busqueda={Uri.EscapeDataString(busqueda)}";
    }

    private async Task<HttpResponseMessage> AsignarAsync(
        FabricaConEntraSimulado.Navegador navegador, int conceptoId, string referenciaId,
        string? busqueda = "cable", int? presupuestoId = null)
    {
        var formulario = new Dictionary<string, string>
        {
            // El token se pide a la tabla de conceptos, que pueden abrir todos los roles, así también
            // se prueba la seguridad del POST con usuarios que no pueden abrir la pantalla de selección.
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}"),
            ["referenciaId"] = referenciaId,
            ["busqueda"] = busqueda ?? string.Empty
        };
        return await navegador.PostAsync(
            $"/ConceptosPresupuesto/AsignarMaterial/{presupuestoId ?? _presupuestoId}?conceptoId={conceptoId}", formulario);
    }

    private async Task<HttpResponseMessage> GuardarBorradorAsync(
        FabricaConEntraSimulado.Navegador navegador, int? presupuestoId = null, bool? esperabaCambiosPendientes = null)
    {
        int idObjetivo = presupuestoId ?? _presupuestoId;
        string html = await (await navegador.GetAsync($"/ConceptosPresupuesto/Index/{idObjetivo}")).Content.ReadAsStringAsync();
        string token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        string version = Regex.Match(html, "name=\"versionBorrador\" value=\"([^\"]+)\"").Groups[1].Value;
        string esperaba = Regex.Match(html, "name=\"esperabaCambiosPendientes\" value=\"([^\"]+)\"").Groups[1].Value;
        string huella = Regex.Match(html, "name=\"huellaEstadoBase\" value=\"([^\"]*)\"").Groups[1].Value;

        var formulario = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["esperabaCambiosPendientes"] = esperabaCambiosPendientes.HasValue
                ? (esperabaCambiosPendientes.Value ? "true" : "false")
                : (string.IsNullOrEmpty(esperaba) ? "false" : esperaba),
            ["versionBorrador"] = string.IsNullOrEmpty(version) ? "0" : version,
            ["huellaEstadoBase"] = huella
        };
        return await navegador.PostAsync($"/ConceptosPresupuesto/GuardarBorrador/{idObjetivo}", formulario);
    }

    private async Task<ConceptoPresupuesto> LeerFilaAsync(int conceptoId)
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.ConceptosPresupuesto.AsNoTracking().SingleAsync(c => c.Id == conceptoId);
    }

    // Una "foto" de todo el catálogo para comprobar que seleccionar no cambia nada en él.
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
    public async Task Busqueda_ConCoincidencias_MuestraLasReferenciasSinSeleccionarNada()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = WebUtility.HtmlDecode(await (await navegador.GetAsync(RutaSeleccion(_filaSinMaterialId, "cable"))).Content.ReadAsStringAsync());

        Assert.Contains("Cable de cobre", pagina);
        Assert.Contains("Proveedor A", pagina);
        Assert.Contains("Proveedor B", pagina);
        Assert.DoesNotContain("Pintura blanca", pagina);
        Assert.Contains(TextoDetectadoPorIa, pagina);
        Assert.Contains("Sin material seleccionado", pagina);
        // La búsqueda no elige nada: la fila sigue igual.
        Assert.Null((await LeerFilaAsync(_filaSinMaterialId)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task SinTextoDeBusqueda_NoSeListaNingunMaterialNiSePreseleccionaPorElTextoDetectado()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = WebUtility.HtmlDecode(await (await navegador.GetAsync(RutaSeleccion(_filaSinMaterialId))).Content.ReadAsStringAsync());

        Assert.Contains(TextoDetectadoPorIa, pagina);
        Assert.DoesNotContain("Cable de cobre", pagina);
        Assert.DoesNotContain("name=\"referenciaId\"", pagina);
        Assert.Null((await LeerFilaAsync(_filaSinMaterialId)).MaterialId);
    }

    [FactConSqlServer]
    public async Task Busqueda_SinResultados_InformaQueElMaterialSeRegistraEnElCatalogoYNoCreaNada()
    {
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await navegador.GetAsync(RutaSeleccion(_filaSinMaterialId, "material que no existe"));

        string pagina = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("No se encontraron materiales.", pagina);
        Assert.Contains("registrarse primero en el catálogo", pagina);
        Assert.DoesNotContain("name=\"referenciaId\"", pagina);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
        Assert.Null((await LeerFilaAsync(_filaSinMaterialId)).MaterialProveedorId);
    }

    [TheoryConSqlServer]
    [InlineData(RolUsuario.Operador, false)]
    [InlineData(RolUsuario.Supervisor, true)]
    [InlineData(RolUsuario.Administrador, true)]
    public async Task SinResultados_SoloOfreceRegistrarMaterialAQuienPuedeHacerlo(RolUsuario rol, bool veElBoton)
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable: rol == RolUsuario.Operador);

        string pagina = WebUtility.HtmlDecode(await (await navegador.GetAsync(RutaSeleccion(_filaSinMaterialId, "inexistente"))).Content.ReadAsStringAsync());

        Assert.Equal(veElBoton, pagina.Contains("Registrar material"));
    }

    [FactConSqlServer]
    public async Task Seleccion_VinculaLaFilaConservandoConceptoDetectadoYCantidadYSinTocarElCatalogo()
    {
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await AsignarAsync(navegador, _filaSinMaterialId, _cableProveedorAId.ToString());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        // RF-005: antes de guardar el borrador la base no se modifica.
        Assert.Null((await LeerFilaAsync(_filaSinMaterialId)).MaterialProveedorId);

        var guardado = await GuardarBorradorAsync(navegador);
        Assert.Equal(HttpStatusCode.Redirect, guardado.StatusCode);

        var fila = await LeerFilaAsync(_filaSinMaterialId);
        Assert.Equal(_cableProveedorAId, fila.MaterialProveedorId);
        Assert.NotNull(fila.MaterialId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(UnidadMedida.Metro, fila.Unidad);
        Assert.Equal(100m, fila.PrecioUnitario);
        Assert.Equal(Moneda.UYU, fila.Moneda);
        Assert.Equal(TextoDetectadoPorIa, fila.ConceptoDetectado);
        Assert.Equal(30m, fila.Cantidad);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());

        string tabla = await (await navegador.GetAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}")).Content.ReadAsStringAsync();
        Assert.Contains("Cambiar material", WebUtility.HtmlDecode(tabla));
        Assert.Contains(TextoDetectadoPorIa, WebUtility.HtmlDecode(tabla));
    }

    [FactConSqlServer]
    public async Task Sustitucion_CambiaElMaterialConLosDatosDeLaNuevaReferenciaYConservaLoDemas()
    {
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await AsignarAsync(navegador, _filaConPinturaId, _cableProveedorBId.ToString());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(_pinturaProveedorAId, (await LeerFilaAsync(_filaConPinturaId)).MaterialProveedorId);

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        var fila = await LeerFilaAsync(_filaConPinturaId);
        Assert.Equal(_cableProveedorBId, fila.MaterialProveedorId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(UnidadMedida.Metro, fila.Unidad);
        Assert.Equal(2.5m, fila.PrecioUnitario);
        Assert.Equal(Moneda.USD, fila.Moneda);
        Assert.Equal("pintura para paredes", fila.ConceptoDetectado);
        Assert.Equal(4m, fila.Cantidad);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task Sustitucion_LaPantallaMarcaLaReferenciaActualYNoOfreceElegirlaDeNuevo()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = WebUtility.HtmlDecode(await (await navegador.GetAsync(RutaSeleccion(_filaConPinturaId, "pintura"))).Content.ReadAsStringAsync());

        Assert.Contains("Cambiar material de la fila", pagina);
        Assert.Contains("Seleccionado", pagina);
        Assert.DoesNotContain($"value=\"{_pinturaProveedorAId}\"", pagina);
    }

    [FactConSqlServer]
    public async Task ElegirLaMismaReferenciaOtraVez_NoCambiaLosValoresHistoricosAunqueElCatalogoCambio()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.MaterialProveedores.Where(r => r.Id == _pinturaProveedorAId)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(r => r.PrecioUnitario, 99m));
            await contexto.Materiales.Where(m => m.Nombre == "Pintura blanca")
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(m => m.Nombre, "Pintura renombrada"));
        }

        var respuesta = await AsignarAsync(navegador, _filaConPinturaId, _pinturaProveedorAId.ToString(), "pintura");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("ya es la seleccionada", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var fila = await LeerFilaAsync(_filaConPinturaId);
        Assert.Equal("Pintura blanca", fila.Concepto);
        Assert.Equal(7.5m, fila.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task ReferenciaYaUsadaEnOtraFilaDelPresupuesto_SeRechazaSinCambiarLaFila()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await AsignarAsync(navegador, _filaSinMaterialId, _pinturaProveedorAId.ToString(), "pintura");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("ya están en otra fila", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var fila = await LeerFilaAsync(_filaSinMaterialId);
        Assert.Null(fila.MaterialProveedorId);
        Assert.Null(fila.Concepto);
        Assert.Equal(TextoDetectadoPorIa, fila.ConceptoDetectado);
    }




    // S3-T13 (US-010A): se puede usar el mismo material en dos filas
    // siempre que los proveedores sean distintos.
    [FactConSqlServer]
    public async Task MismoMaterial_ConDistintosProveedores_SePermiteEnFilasDiferentes()
    {
        int filaProveedorAId = await CrearFilaConCableElegidoAsync();
        string catalogoAntes = await FotoDelCatalogoAsync();

        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await AsignarAsync(
            navegador,
            _filaSinMaterialId,
            _cableProveedorBId.ToString(),
            "cable");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        var filaProveedorA = await LeerFilaAsync(filaProveedorAId);
        var filaProveedorB = await LeerFilaAsync(_filaSinMaterialId);

        // Las dos filas utilizan el mismo material.
        Assert.Equal(filaProveedorA.MaterialId, filaProveedorB.MaterialId);

        // Cada fila utiliza un proveedor diferente.
        Assert.Equal(_cableProveedorAId, filaProveedorA.MaterialProveedorId);
        Assert.Equal(_cableProveedorBId, filaProveedorB.MaterialProveedorId);

        // Se guardan los valores históricos de la referencia elegida.
        Assert.Equal(2.5m, filaProveedorB.PrecioUnitario);
        Assert.Equal(Moneda.USD, filaProveedorB.Moneda);
        Assert.Equal(30m, filaProveedorB.Cantidad);

        // Seleccionar una referencia no modifica el catálogo.
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }







    [TheoryConSqlServer]
    [InlineData("999999", "La referencia seleccionada no existe")]
    [InlineData("0", "Seleccioná una referencia de material y proveedor.")]
    [InlineData("-3", "Seleccioná una referencia de material y proveedor.")]
    [InlineData("", "Seleccioná una referencia de material y proveedor.")]
    public async Task ReferenciaInexistenteOVacia_SeRechazaSinCambiarLaFila(string referenciaId, string mensaje)
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await AsignarAsync(navegador, _filaSinMaterialId, referenciaId);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(mensaje, WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var fila = await LeerFilaAsync(_filaSinMaterialId);
        Assert.Null(fila.MaterialProveedorId);
        Assert.Null(fila.MaterialId);
    }

    [FactConSqlServer]
    public async Task FilaQueNoEsDeTipoMaterial_SeRechazaSinCambios()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var pantalla = await navegador.GetAsync(RutaSeleccion(_filaManoDeObraId, "cable"));
        var respuesta = await AsignarAsync(navegador, _filaManoDeObraId, _cableProveedorAId.ToString());

        Assert.Equal(HttpStatusCode.Redirect, pantalla.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("filas de tipo Material", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var fila = await LeerFilaAsync(_filaManoDeObraId);
        Assert.Null(fila.MaterialProveedorId);
        Assert.Equal("Mano de obra", fila.Concepto);
        Assert.Equal(1500m, fila.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task FilaDeOtroPresupuesto_NoSePuedeModificarCambiandoElIdDeLaUrl()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var pantalla = await navegador.GetAsync(RutaSeleccion(_filaDeOtroPresupuestoId, "cable"));
        var respuesta = await AsignarAsync(navegador, _filaDeOtroPresupuestoId, _cableProveedorAId.ToString());

        Assert.Equal(HttpStatusCode.NotFound, pantalla.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Null((await LeerFilaAsync(_filaDeOtroPresupuestoId)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task FilaInexistente_DevuelveNoEncontradoEnLaPantalla()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var pantalla = await navegador.GetAsync(RutaSeleccion(999999, "cable"));

        Assert.Equal(HttpStatusCode.NotFound, pantalla.StatusCode);
    }

    [TheoryConSqlServer]
    [InlineData(EstadoPresupuesto.Borrador, false, RolUsuario.Operador)]
    [InlineData(EstadoPresupuesto.Aprobado, true, RolUsuario.Administrador)]
    [InlineData(EstadoPresupuesto.Rechazado, true, RolUsuario.Supervisor)]
    [InlineData(EstadoPresupuesto.EnviadoPendienteDeRespuestaDelCliente, true, RolUsuario.Operador)]
    [InlineData(EstadoPresupuesto.Cancelado, true, RolUsuario.Administrador)]
    public async Task SinPermisoOPresupuestoNoEditable_ElServidorRechazaLaPantallaYLaSeleccion(
        EstadoPresupuesto estado, bool responsable, RolUsuario rol)
    {
        string catalogoAntes = await FotoDelCatalogoAsync();
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(p => p.Estado, estado));
        }
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable);
        string pantallaIndex = await (await navegador.GetAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}")).Content.ReadAsStringAsync();

        var pantalla = await navegador.GetAsync(RutaSeleccion(_filaSinMaterialId, "cable"));
        var respuesta = await AsignarAsync(navegador, _filaSinMaterialId, _cableProveedorAId.ToString());
        var sustitucion = await AsignarAsync(navegador, _filaConPinturaId, _cableProveedorBId.ToString());

        Assert.DoesNotContain("Seleccionar material", pantallaIndex);
        Assert.DoesNotContain("Cambiar material", pantallaIndex);
        Assert.Contains("/Cuenta/AccesoDenegado", pantalla.Headers.Location?.ToString());
        Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
        Assert.Contains("/Cuenta/AccesoDenegado", sustitucion.Headers.Location?.ToString());
        Assert.Null((await LeerFilaAsync(_filaSinMaterialId)).MaterialProveedorId);
        Assert.Equal(_pinturaProveedorAId, (await LeerFilaAsync(_filaConPinturaId)).MaterialProveedorId);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task ResponsableConElPresupuestoEditable_VeLosBotonesDeSeleccionSoloEnLasFilasMaterial()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = WebUtility.HtmlDecode(await (await navegador.GetAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}")).Content.ReadAsStringAsync());

        Assert.Equal(1, ContarApariciones(pagina, "Seleccionar material"));
        Assert.Equal(1, ContarApariciones(pagina, "Cambiar material"));
        Assert.Contains("Sin material seleccionado", pagina);
    }

    [FactConSqlServer]
    public async Task SeleccionSinTokenAntifalsificacion_SeRechaza()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await navegador.PostAsync(
            $"/ConceptosPresupuesto/AsignarMaterial/{_presupuestoId}?conceptoId={_filaSinMaterialId}",
            new Dictionary<string, string> { ["referenciaId"] = _cableProveedorAId.ToString() });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Null((await LeerFilaAsync(_filaSinMaterialId)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task DosFilasQueSeleccionanLaMismaReferenciaALaVez_SoloUnaLaConsigue()
    {
        using EopDbContext contextoInicial = CrearContexto();
        var responsable = await contextoInicial.Usuarios.SingleAsync();
        var otraFila = new ConceptoPresupuesto
        {
            PresupuestoId = _presupuestoId,
            TipoConceptoId = TipoConcepto.MaterialId,
            ConceptoDetectado = "otra fila detectada",
            Cantidad = 2m
        };
        contextoInicial.ConceptosPresupuesto.Add(otraFila);
        await contextoInicial.SaveChangesAsync();

        async Task<ResultadoAccionGuardarBorrador> SeleccionarYGuardarAsync(int conceptoId)
        {
            using EopDbContext contexto = CrearContexto();
            var repositorio = new RepositorioConceptosPresupuesto(contexto);
            var almacen = new AlmacenBorradorEnMemoria();
            var sincronizador = new SincronizadorBorradorPresupuesto();
            var seleccionar = new SeleccionarMaterialDeConcepto(repositorio, almacen, sincronizador);
            Assert.Null(await seleccionar.EjecutarAsync(_presupuestoId, conceptoId, _cableProveedorAId, responsable, CancellationToken.None));

            var guardar = new GuardarBorradorPresupuesto(repositorio, almacen, sincronizador);
            return await guardar.EjecutarAsync(
                _presupuestoId, new ContextoEdicionBorrador(true, 1), responsable, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(SeleccionarYGuardarAsync(_filaSinMaterialId), SeleccionarYGuardarAsync(otraFila.Id));

        Assert.Single(resultados, resultado => resultado.GuardadoCorrectamente);
        Assert.Single(resultados, resultado => resultado.Estado == ResultadoGuardarBorrador.ConflictoConcurrencia);
        Assert.Equal(1, await contextoInicial.ConceptosPresupuesto.CountAsync(c => c.MaterialProveedorId == _cableProveedorAId));
    }

    [FactConSqlServer]
    public async Task DosSustitucionesSimultaneasDeLaMismaFila_TerminanSinBloquearseYDejanUnaReferenciaValida()
    {
        using EopDbContext contextoInicial = CrearContexto();
        var responsable = await contextoInicial.Usuarios.SingleAsync();

        async Task<ResultadoAccionGuardarBorrador> SeleccionarYGuardarAsync(int referenciaId)
        {
            using EopDbContext contexto = CrearContexto();
            var repositorio = new RepositorioConceptosPresupuesto(contexto);
            var almacen = new AlmacenBorradorEnMemoria();
            var sincronizador = new SincronizadorBorradorPresupuesto();
            var seleccionar = new SeleccionarMaterialDeConcepto(repositorio, almacen, sincronizador);
            Assert.Null(await seleccionar.EjecutarAsync(_presupuestoId, _filaSinMaterialId, referenciaId, responsable, CancellationToken.None));

            var guardar = new GuardarBorradorPresupuesto(repositorio, almacen, sincronizador);
            return await guardar.EjecutarAsync(
                _presupuestoId, new ContextoEdicionBorrador(true, 1), responsable, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(SeleccionarYGuardarAsync(_cableProveedorAId), SeleccionarYGuardarAsync(_cableProveedorBId));

        Assert.Single(resultados, resultado => resultado.GuardadoCorrectamente);
        Assert.Single(resultados, resultado => resultado.Estado == ResultadoGuardarBorrador.ConflictoConcurrencia);
        var fila = await LeerFilaAsync(_filaSinMaterialId);
        Assert.Contains(fila.MaterialProveedorId, new int?[] { _cableProveedorAId, _cableProveedorBId });
        Assert.Equal(TextoDetectadoPorIa, fila.ConceptoDetectado);
    }

    // ---- S3-T11 (US-010A, RF-19): consultar las referencias de precios de un material ----

    // Agrega una fila de tipo Material que ya tiene el cable de cobre elegido (proveedor A) y deja
    // las dos referencias del cable con fechas conocidas. La del proveedor B se guarda a las 01:30
    // UTC del 2/11, que en Uruguay (UTC-3) todavía es el 1/11 a las 22:30: sirve para probar que la
    // fecha se muestra en hora de Uruguay y no en UTC.
    private async Task<int> CrearFilaConCableElegidoAsync()
    {
        using EopDbContext contexto = CrearContexto();
        await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorAId)
            .ExecuteUpdateAsync(c => c.SetProperty(r => r.FechaUltimaActualizacion, new DateTime(2026, 11, 5, 15, 0, 0)));
        await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorBId)
            .ExecuteUpdateAsync(c => c.SetProperty(r => r.FechaUltimaActualizacion, new DateTime(2026, 11, 2, 1, 30, 0)));

        var cable = await contexto.Materiales.SingleAsync(m => m.Nombre == "Cable de cobre");
        var referenciaA = await contexto.MaterialProveedores.SingleAsync(r => r.Id == _cableProveedorAId);
        var fila = new ConceptoPresupuesto
        {
            PresupuestoId = _presupuestoId,
            TipoConceptoId = TipoConcepto.MaterialId,
            ConceptoDetectado = "cable de cobre para tablero",
            Cantidad = 10m
        };
        fila.CopiarReferencia(cable, referenciaA);
        contexto.ConceptosPresupuesto.Add(fila);
        await contexto.SaveChangesAsync();
        return fila.Id;
    }

    private async Task<string> LeerPaginaAsync(FabricaConEntraSimulado.Navegador navegador, string ruta) =>
        WebUtility.HtmlDecode(await (await navegador.GetAsync(ruta)).Content.ReadAsStringAsync());

    [FactConSqlServer]
    public async Task FilaConMaterial_MuestraTodasSusReferenciasSinBuscarYSinModificarElCatalogo()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));

        Assert.Contains("Referencias de precios de Cable de cobre", pagina);
        Assert.Contains("Proveedor A", pagina);
        Assert.Contains("Proveedor B", pagina);
        Assert.Equal(1, ContarApariciones(pagina, "Referencia de esta fila"));
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task FilaConMaterial_ElPanelDistingueMonedasYMuestraPrecioProveedorYFechaEnHoraDeUruguay()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));
        string filas = string.Join("\n", Regex.Matches(pagina, "<tr>.*?</tr>", RegexOptions.Singleline)
            .Select(coincidencia => Regex.Replace(coincidencia.Value, @"\s+", " ")));

        // Cada referencia muestra proveedor, precio, moneda y fecha en la misma fila de la tabla
        // (desde S3-T12 la fila termina además con la celda de acción).
        Assert.Matches(@"<tr> <td class=""col-principal""> Proveedor A.*?<td class=""text-end"">100</td> <td>UYU</td> <td>05/11/2026 12:00</td> .*?</tr>", filas);
        Assert.Matches(@"<tr> <td class=""col-principal""> Proveedor B.*?<td class=""text-end"">2[.,]5</td> <td>USD</td> <td>01/11/2026 22:30</td> .*?</tr>", filas);
        Assert.DoesNotContain("02/11/2026", pagina);
    }

    [FactConSqlServer]
    public async Task FilaSinMaterial_NoMuestraElPanelDeReferencias()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(_filaSinMaterialId));

        Assert.DoesNotContain("Referencias de precios de", pagina);
    }

    [FactConSqlServer]
    public async Task Busqueda_MuestraProveedorPrecioMonedaYFechaDeCadaReferencia()
    {
        await CrearFilaConCableElegidoAsync();
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(_filaSinMaterialId, "cable"));

        Assert.Matches(@"Proveedor A</td>\s*<td class=""text-end"">UYU 100</td>\s*<td>05/11/2026 12:00</td>", pagina);
        Assert.Matches(@"Proveedor B</td>\s*<td class=""text-end"">USD 2[.,]5</td>\s*<td>01/11/2026 22:30</td>", pagina);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task Busqueda_DeUnMaterialSinReferencias_InformaQueNoTieneProveedoresYNoOfreceSeleccionarlo()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            contexto.Materiales.Add(new Material { Nombre = "Cinta aisladora", Unidad = UnidadMedida.Unidad });
            await contexto.SaveChangesAsync();
        }
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(_filaSinMaterialId, "cinta"));

        Assert.Contains("Cinta aisladora", pagina);
        Assert.Contains("Sin proveedores con precio registrado", pagina);
        Assert.DoesNotContain("name=\"referenciaId\"", pagina);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task ConsultaDeReferencias_SinPermisoElServidorNoMuestraLosPrecios()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var operadorNoResponsable = await IniciarSesionAsync(fabrica, RolUsuario.Operador, responsable: false);

        var respuesta = await operadorNoResponsable.GetAsync(RutaSeleccion(filaId));

        Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
        Assert.DoesNotContain("Referencias de precios de", await respuesta.Content.ReadAsStringAsync());
    }

    // ---- S3-T12 (US-010A, RF-20): seleccionar otra referencia del mismo material y conservar la historia ----

    private async Task<HttpResponseMessage> CambiarProveedorAsync(
        FabricaConEntraSimulado.Navegador navegador, int conceptoId, string referenciaId,
        int? presupuestoId = null, Dictionary<string, string>? camposExtra = null)
    {
        var formulario = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}"),
            ["referenciaId"] = referenciaId
        };
        foreach (var campo in camposExtra ?? [])
        {
            formulario[campo.Key] = campo.Value;
        }
        return await navegador.PostAsync(
            $"/ConceptosPresupuesto/CambiarProveedor/{presupuestoId ?? _presupuestoId}?conceptoId={conceptoId}", formulario);
    }

    [FactConSqlServer]
    public async Task PanelDeReferencias_OfreceSeleccionarCadaReferenciaMenosLaQueLaFilaYaTiene()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));

        Assert.Contains($"value=\"{_cableProveedorBId}\"", pagina);
        Assert.DoesNotContain($"value=\"{_cableProveedorAId}\"", pagina);
        Assert.Equal(1, ContarApariciones(pagina, "Seleccionada"));
        // Nada se preselecciona: el cambio ocurre solo cuando el usuario envía el formulario.
        Assert.Equal(_cableProveedorAId, (await LeerFilaAsync(filaId)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task PanelDeReferencias_SoloEnviaLaReferenciaYNoOfreceEditarLosValoresCopiados()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));
        string formularios = string.Join("\n", Regex.Matches(pagina, @"<form[^>]*CambiarProveedor.*?</form>", RegexOptions.Singleline).Select(m => m.Value));
        var campos = Regex.Matches(formularios, @"<input[^>]*name=""([^""]+)""").Select(m => m.Groups[1].Value).Distinct().Order();

        Assert.Equal(
            new[] { "__RequestVerificationToken", "esperabaCambiosPendientes", "huellaEstadoBase", "referenciaId", "versionBorrador" },
            campos);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_ActualizaProveedorPrecioYMonedaYConservaMaterialConceptoDetectadoYCantidad()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        int materialIdAntes = (await LeerFilaAsync(filaId)).MaterialId!.Value;
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await CambiarProveedorAsync(navegador, filaId, _cableProveedorBId.ToString());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        // RF-005: el cambio queda en el borrador hasta guardar explícitamente.
        Assert.Equal(_cableProveedorAId, (await LeerFilaAsync(filaId)).MaterialProveedorId);

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        // Se lee con un contexto nuevo: lo que se ve es lo que quedó guardado en la base.
        var fila = await LeerFilaAsync(filaId);
        Assert.Equal(_cableProveedorBId, fila.MaterialProveedorId);
        Assert.Equal(materialIdAntes, fila.MaterialId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(UnidadMedida.Metro, fila.Unidad);
        Assert.Equal(2.5m, fila.PrecioUnitario);
        Assert.Equal(Moneda.USD, fila.Moneda);
        Assert.Equal("cable de cobre para tablero", fila.ConceptoDetectado);
        Assert.Equal(10m, fila.Cantidad);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());

        // Al volver, el panel marca ahora al proveedor B como la referencia de la fila.
        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));
        Assert.Contains($"value=\"{_cableProveedorAId}\"", pagina);
        Assert.DoesNotContain($"value=\"{_cableProveedorBId}\"", pagina);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_IgnoraValoresHistoricosEscritosAManoEnLaSolicitud()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await CambiarProveedorAsync(navegador, filaId, _cableProveedorBId.ToString(), camposExtra: new()
        {
            ["Concepto"] = "Nombre inventado",
            ["PrecioUnitario"] = "999",
            ["Moneda"] = "UYU",
            ["Unidad"] = "Litro"
        });
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        var fila = await LeerFilaAsync(filaId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(2.5m, fila.PrecioUnitario);
        Assert.Equal(Moneda.USD, fila.Moneda);
        Assert.Equal(UnidadMedida.Metro, fila.Unidad);
    }

    [FactConSqlServer]
    public async Task ElCatalogoCambiaDespuesDeSeleccionar_ElPresupuestoConservaLosCuatroValoresHistoricos()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);
        await CambiarProveedorAsync(navegador, filaId, _cableProveedorBId.ToString());
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        // El catálogo cambia por completo: nombre y unidad del material, precio y moneda de la referencia.
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Materiales.Where(m => m.Nombre == "Cable de cobre")
                .ExecuteUpdateAsync(c => c.SetProperty(m => m.Nombre, "Cable renombrado").SetProperty(m => m.Unidad, UnidadMedida.Unidad));
            await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorBId)
                .ExecuteUpdateAsync(c => c.SetProperty(r => r.PrecioUnitario, 99m).SetProperty(r => r.Moneda, Moneda.UYU));
        }

        var fila = await LeerFilaAsync(filaId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(UnidadMedida.Metro, fila.Unidad);
        Assert.Equal(2.5m, fila.PrecioUnitario);
        Assert.Equal(Moneda.USD, fila.Moneda);
        // Y la tabla del presupuesto muestra los valores históricos, no los nuevos del catálogo.
        string tabla = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Cable de cobre", tabla);
        Assert.DoesNotContain("Cable renombrado", tabla);
    }

    [FactConSqlServer]
    public async Task ElCatalogoCambiaAntesDeCambiarProveedor_LaFilaConservaSuHistoriaHastaLaNuevaSeleccionExpresa()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorAId)
                .ExecuteUpdateAsync(c => c.SetProperty(r => r.PrecioUnitario, 150m));
            await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorBId)
                .ExecuteUpdateAsync(c => c.SetProperty(r => r.PrecioUnitario, 3.5m));
        }

        // Consultar el panel no pisa nada: la fila sigue con el precio que copió al seleccionar.
        await LeerPaginaAsync(navegador, RutaSeleccion(filaId));
        Assert.Equal(100m, (await LeerFilaAsync(filaId)).PrecioUnitario);

        // La nueva selección expresa sí vuelve a copiar los valores vigentes de la referencia elegida.
        await CambiarProveedorAsync(navegador, filaId, _cableProveedorBId.ToString());
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);
        var fila = await LeerFilaAsync(filaId);
        Assert.Equal(_cableProveedorBId, fila.MaterialProveedorId);
        Assert.Equal(3.5m, fila.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_NoModificaLasOtrasFilasDelPresupuesto()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        var otraFilaAntes = await LeerFilaAsync(_filaConPinturaId);
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await CambiarProveedorAsync(navegador, filaId, _cableProveedorBId.ToString());
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        var otraFilaDespues = await LeerFilaAsync(_filaConPinturaId);
        Assert.Equal(otraFilaAntes.MaterialProveedorId, otraFilaDespues.MaterialProveedorId);
        Assert.Equal(otraFilaAntes.PrecioUnitario, otraFilaDespues.PrecioUnitario);
        Assert.Equal(otraFilaAntes.Concepto, otraFilaDespues.Concepto);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_ConUnaReferenciaDeOtroMaterial_SeRechazaYConservaElMaterial()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        // La pintura de A ya está en otra fila: se libera para que el rechazo sea por ser de otro
        // material y no por duplicada.
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.ConceptosPresupuesto.Where(c => c.Id == _filaConPinturaId).ExecuteDeleteAsync();
        }
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await CambiarProveedorAsync(navegador, filaId, _pinturaProveedorAId.ToString());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no es del material de la fila", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var fila = await LeerFilaAsync(filaId);
        Assert.Equal(_cableProveedorAId, fila.MaterialProveedorId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(100m, fila.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_EnUnaFilaSinMaterial_SeRechazaYNoLaVincula()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await CambiarProveedorAsync(navegador, _filaSinMaterialId, _cableProveedorAId.ToString());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("todavía no tiene un material", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var fila = await LeerFilaAsync(_filaSinMaterialId);
        Assert.Null(fila.MaterialProveedorId);
        Assert.Null(fila.MaterialId);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_ALaMismaReferencia_NoCambiaNada()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorAId)
                .ExecuteUpdateAsync(c => c.SetProperty(r => r.PrecioUnitario, 150m));
        }

        var respuesta = await CambiarProveedorAsync(navegador, filaId, _cableProveedorAId.ToString());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("ya es la seleccionada", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        Assert.Equal(100m, (await LeerFilaAsync(filaId)).PrecioUnitario);
    }


    // S3-T13 (US-010A): no se puede repetir la misma combinación
    // material-proveedor en dos filas del mismo presupuesto.
    [FactConSqlServer]
    public async Task CambioDeProveedor_ALaCombinacionDeOtraFila_SeRechazaPorDuplicada()
    {
        int filaId = await CrearFilaConCableElegidoAsync();

        // Otra fila del mismo presupuesto ya usa el cable del proveedor B (RF-20).
        using (EopDbContext contexto = CrearContexto())
        {
            var cable = await contexto.Materiales.SingleAsync(
                m => m.Nombre == "Cable de cobre");

            var referenciaB = await contexto.MaterialProveedores.SingleAsync(
                r => r.Id == _cableProveedorBId);

            var filaConB = new ConceptoPresupuesto
            {
                PresupuestoId = _presupuestoId,
                TipoConceptoId = TipoConcepto.MaterialId,
                Cantidad = 1m
            };

            filaConB.CopiarReferencia(cable, referenciaB);

            contexto.ConceptosPresupuesto.Add(filaConB);
            await contexto.SaveChangesAsync();
        }

        var filaAntes = await LeerFilaAsync(filaId);
        string catalogoAntes = await FotoDelCatalogoAsync();

        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(
            fabrica, RolUsuario.Operador);

        // Intentamos utilizar una combinación que ya está ocupada.
        var respuesta = await CambiarProveedorAsync(
            navegador, filaId, _cableProveedorBId.ToString());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        Assert.Contains(
            "ya están en otra fila",
            WebUtility.HtmlDecode(
                await respuesta.Content.ReadAsStringAsync()));

        var filaDespues = await LeerFilaAsync(filaId);

        // El rechazo no puede modificar la referencia original.
        Assert.Equal(_cableProveedorAId, filaDespues.MaterialProveedorId);

        // Tampoco puede modificar los datos históricos de la fila.
        Assert.Equal(filaAntes.MaterialId, filaDespues.MaterialId);
        Assert.Equal(filaAntes.Concepto, filaDespues.Concepto);
        Assert.Equal(filaAntes.Unidad, filaDespues.Unidad);
        Assert.Equal(filaAntes.PrecioUnitario, filaDespues.PrecioUnitario);
        Assert.Equal(filaAntes.Moneda, filaDespues.Moneda);
        Assert.Equal(filaAntes.Cantidad, filaDespues.Cantidad);

        // El catálogo tampoco debe cambiar por un intento rechazado.
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }








    [TheoryConSqlServer]
    [InlineData("999999", "La referencia seleccionada no existe")]
    [InlineData("0", "Seleccioná la referencia de proveedor")]
    [InlineData("-3", "Seleccioná la referencia de proveedor")]
    [InlineData("", "Seleccioná la referencia de proveedor")]
    public async Task CambioDeProveedor_SinReferenciaValida_SeRechazaSinCambiarLaFila(string referenciaId, string mensaje)
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await CambiarProveedorAsync(navegador, filaId, referenciaId);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(mensaje, WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        Assert.Equal(_cableProveedorAId, (await LeerFilaAsync(filaId)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_EnUnaFilaQueNoEsMaterial_SeRechazaSinCambios()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await CambiarProveedorAsync(navegador, _filaManoDeObraId, _cableProveedorAId.ToString());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("filas de tipo Material", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var fila = await LeerFilaAsync(_filaManoDeObraId);
        Assert.Null(fila.MaterialProveedorId);
        Assert.Equal(1500m, fila.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_EnUnaFilaDeOtroPresupuesto_NoSePuedeCambiandoElIdDeLaUrl()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        // Administrador, para que el rechazo no se deba a permisos sino a que la fila no es de ese presupuesto.
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Administrador, responsable: false);

        // La fila es del primer presupuesto, pero la solicitud dice que es del otro.
        var respuesta = await CambiarProveedorAsync(navegador, filaId, _cableProveedorBId.ToString(), _otroPresupuestoId);

        // La fila no existe en ese presupuesto: no se modifica nada y se vuelve a la tabla con el aviso.
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.DoesNotContain("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
        Assert.Equal(_cableProveedorAId, (await LeerFilaAsync(filaId)).MaterialProveedorId);
    }

    [TheoryConSqlServer]
    [InlineData(EstadoPresupuesto.Borrador, false, RolUsuario.Operador)]
    [InlineData(EstadoPresupuesto.Aprobado, true, RolUsuario.Administrador)]
    [InlineData(EstadoPresupuesto.Rechazado, true, RolUsuario.Supervisor)]
    [InlineData(EstadoPresupuesto.EnviadoPendienteDeRespuestaDelCliente, true, RolUsuario.Operador)]
    [InlineData(EstadoPresupuesto.Cancelado, true, RolUsuario.Administrador)]
    public async Task CambioDeProveedor_SinPermisoOPresupuestoNoEditable_ElServidorLoRechazaYNoCambiaNada(
        EstadoPresupuesto estado, bool responsable, RolUsuario rol)
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        string catalogoAntes = await FotoDelCatalogoAsync();
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(p => p.Estado, estado));
        }
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable);

        var respuesta = await CambiarProveedorAsync(navegador, filaId, _cableProveedorBId.ToString());

        Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
        var fila = await LeerFilaAsync(filaId);
        Assert.Equal(_cableProveedorAId, fila.MaterialProveedorId);
        Assert.Equal(100m, fila.PrecioUnitario);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [TheoryConSqlServer]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Administrador)]
    public async Task CambioDeProveedor_SupervisorYAdministradorPuedenCambiarlo(RolUsuario rol)
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable: false);

        var respuesta = await CambiarProveedorAsync(navegador, filaId, _cableProveedorBId.ToString());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);
        Assert.Equal(_cableProveedorBId, (await LeerFilaAsync(filaId)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task CambioDeProveedor_SinTokenAntifalsificacion_SeRechaza()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await navegador.PostAsync(
            $"/ConceptosPresupuesto/CambiarProveedor/{_presupuestoId}?conceptoId={filaId}",
            new Dictionary<string, string> { ["referenciaId"] = _cableProveedorBId.ToString() });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(_cableProveedorAId, (await LeerFilaAsync(filaId)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task DosCambiosSimultaneosALaMismaReferencia_SoloUnoLoHaceYElOtroDetectaConcurrencia()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        using EopDbContext contextoInicial = CrearContexto();
        var responsable = await contextoInicial.Usuarios.SingleAsync();

        async Task<ResultadoAccionGuardarBorrador> CambiarYGuardarAsync()
        {
            using EopDbContext contexto = CrearContexto();
            var repositorio = new RepositorioConceptosPresupuesto(contexto);
            var almacen = new AlmacenBorradorEnMemoria();
            var sincronizador = new SincronizadorBorradorPresupuesto();
            var cambiar = new CambiarProveedorDeConcepto(repositorio, almacen, sincronizador);
            Assert.Null(await cambiar.EjecutarAsync(_presupuestoId, filaId, _cableProveedorBId, responsable, CancellationToken.None));

            var guardar = new GuardarBorradorPresupuesto(repositorio, almacen, sincronizador);
            return await guardar.EjecutarAsync(
                _presupuestoId, new ContextoEdicionBorrador(true, 1), responsable, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(CambiarYGuardarAsync(), CambiarYGuardarAsync());

        Assert.Single(resultados, resultado => resultado.GuardadoCorrectamente);
        Assert.Single(resultados, resultado => resultado.Estado == ResultadoGuardarBorrador.ConflictoConcurrencia);
        Assert.Equal(_cableProveedorBId, (await LeerFilaAsync(filaId)).MaterialProveedorId);
    }

    // ---- S3-T12 (RF-020): actualizar a pedido y con confirmación la referencia que la fila ya tiene ----

    // El catálogo cambia los cuatro valores de la referencia A del cable: nombre y unidad del material,
    // precio y moneda de la referencia.
    private async Task CambiarElCatalogoDeLaReferenciaAAsync()
    {
        using EopDbContext contexto = CrearContexto();
        await contexto.Materiales.Where(m => m.Nombre == "Cable de cobre")
            .ExecuteUpdateAsync(c => c.SetProperty(m => m.Nombre, "Cable renombrado").SetProperty(m => m.Unidad, UnidadMedida.Unidad));
        await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorAId)
            .ExecuteUpdateAsync(c => c.SetProperty(r => r.PrecioUnitario, 150m).SetProperty(r => r.Moneda, Moneda.USD));
    }

    // confirmar == null no envía el campo: es una solicitud que no pasó por el diálogo de confirmación.
    private async Task<HttpResponseMessage> ActualizarReferenciaAsync(
        FabricaConEntraSimulado.Navegador navegador, int conceptoId, string? confirmar = "true",
        int? presupuestoId = null, Dictionary<string, string>? camposExtra = null)
    {
        var formulario = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}")
        };
        if (confirmar != null)
        {
            formulario["confirmar"] = confirmar;
        }
        foreach (var campo in camposExtra ?? [])
        {
            formulario[campo.Key] = campo.Value;
        }
        return await navegador.PostAsync(
            $"/ConceptosPresupuesto/ActualizarReferencia/{presupuestoId ?? _presupuestoId}?conceptoId={conceptoId}", formulario);
    }

    [FactConSqlServer]
    public async Task PanelDeReferencias_OfreceActualizarSoloSiElCatalogoCambioLaReferenciaDeLaFila()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        string alDia = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));
        Assert.DoesNotContain("Actualizar valores", alDia);
        Assert.Contains("Valores al día con el catálogo", alDia);

        await CambiarElCatalogoDeLaReferenciaAAsync();
        string conCambios = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));

        Assert.Contains("Actualizar valores", conCambios);
        Assert.Contains("confirmarActualizarReferencia", conCambios);
        // El diálogo muestra los valores de la fila y los del catálogo para decidir con información.
        Assert.Contains("Cable renombrado", conCambios);
        string dialogo = Regex.Match(conCambios, @"<form[^>]*ActualizarReferencia.*?</form>", RegexOptions.Singleline).Value;
        Assert.Contains("name=\"confirmar\"", dialogo);
        Assert.DoesNotContain("name=\"referenciaId\"", dialogo);
        // Ver el panel, aunque el catálogo haya cambiado, no actualiza nada.
        var fila = await LeerFilaAsync(filaId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(100m, fila.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task ActualizacionConfirmada_CopiaLosCuatroValoresVigentesYConservaMaterialProveedorDetectadoYCantidad()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        var antes = await LeerFilaAsync(filaId);
        await CambiarElCatalogoDeLaReferenciaAAsync();
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await ActualizarReferenciaAsync(navegador, filaId);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        // RF-005: antes de guardar el borrador la base conserva el valor anterior.
        Assert.Equal(100m, (await LeerFilaAsync(filaId)).PrecioUnitario);

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        var fila = await LeerFilaAsync(filaId);
        Assert.Equal("Cable renombrado", fila.Concepto);
        Assert.Equal(UnidadMedida.Unidad, fila.Unidad);
        Assert.Equal(150m, fila.PrecioUnitario);
        Assert.Equal(Moneda.USD, fila.Moneda);
        Assert.Equal(antes.MaterialId, fila.MaterialId);
        Assert.Equal(antes.MaterialProveedorId, fila.MaterialProveedorId);
        Assert.Equal("cable de cobre para tablero", fila.ConceptoDetectado);
        Assert.Equal(10m, fila.Cantidad);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());

        // Ahora la fila coincide con el catálogo y ya no ofrece actualizar.
        string pagina = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));
        Assert.DoesNotContain("Actualizar valores", pagina);
    }

    [TheoryConSqlServer]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("")]
    public async Task SinConfirmacionExplicita_LaFilaConservaSusValoresHistoricosAunqueElCatalogoCambio(string? confirmar)
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await CambiarElCatalogoDeLaReferenciaAAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await ActualizarReferenciaAsync(navegador, filaId, confirmar);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Confirmá la actualización", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var fila = await LeerFilaAsync(filaId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(UnidadMedida.Metro, fila.Unidad);
        Assert.Equal(100m, fila.PrecioUnitario);
        Assert.Equal(Moneda.UYU, fila.Moneda);
        string tabla = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.DoesNotContain("Cable renombrado", tabla);
    }

    [FactConSqlServer]
    public async Task ActualizacionConfirmada_ConLaFilaYaAlDia_InformaQueNoHayNadaQueActualizar()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        var antes = await LeerFilaAsync(filaId);
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await ActualizarReferenciaAsync(navegador, filaId);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("ya coinciden con los del catálogo", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        var despues = await LeerFilaAsync(filaId);
        Assert.Equal(antes.PrecioUnitario, despues.PrecioUnitario);
        Assert.Equal(antes.Concepto, despues.Concepto);
    }

    [FactConSqlServer]
    public async Task ActualizacionConfirmada_IgnoraUnaReferenciaMandadaEnLaSolicitudYNoCambiaDeProveedor()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await CambiarElCatalogoDeLaReferenciaAAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await ActualizarReferenciaAsync(navegador, filaId, camposExtra: new()
        {
            ["referenciaId"] = _cableProveedorBId.ToString(),
            ["PrecioUnitario"] = "999"
        });
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        var fila = await LeerFilaAsync(filaId);
        Assert.Equal(_cableProveedorAId, fila.MaterialProveedorId);
        Assert.Equal(150m, fila.PrecioUnitario);
    }

    [TheoryConSqlServer]
    [InlineData(EstadoPresupuesto.Borrador, false, RolUsuario.Operador)]
    [InlineData(EstadoPresupuesto.Aprobado, true, RolUsuario.Administrador)]
    [InlineData(EstadoPresupuesto.Rechazado, true, RolUsuario.Supervisor)]
    [InlineData(EstadoPresupuesto.EnviadoPendienteDeRespuestaDelCliente, true, RolUsuario.Operador)]
    [InlineData(EstadoPresupuesto.Vencido, true, RolUsuario.Administrador)]
    [InlineData(EstadoPresupuesto.Cancelado, true, RolUsuario.Administrador)]
    public async Task ActualizacionConfirmada_SinPermisoOPresupuestoNoEditable_ElServidorLaRechazaYNoCambiaNada(
        EstadoPresupuesto estado, bool responsable, RolUsuario rol)
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await CambiarElCatalogoDeLaReferenciaAAsync();
        string catalogoAntes = await FotoDelCatalogoAsync();
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(p => p.Estado, estado));
        }
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable);

        var respuesta = await ActualizarReferenciaAsync(navegador, filaId);

        Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
        var fila = await LeerFilaAsync(filaId);
        Assert.Equal("Cable de cobre", fila.Concepto);
        Assert.Equal(100m, fila.PrecioUnitario);
        Assert.Equal(Moneda.UYU, fila.Moneda);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [TheoryConSqlServer]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Administrador)]
    public async Task ActualizacionConfirmada_SupervisorYAdministradorPuedenHacerlo(RolUsuario rol)
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await CambiarElCatalogoDeLaReferenciaAAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable: false);

        var respuesta = await ActualizarReferenciaAsync(navegador, filaId);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);
        Assert.Equal(150m, (await LeerFilaAsync(filaId)).PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task ActualizacionConfirmada_EnUnaFilaSinMaterial_SeRechazaYNoLaVincula()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await ActualizarReferenciaAsync(navegador, _filaSinMaterialId);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("todavía no tiene un material", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        Assert.Null((await LeerFilaAsync(_filaSinMaterialId)).MaterialProveedorId);
    }

    [FactConSqlServer]
    public async Task ActualizacionConfirmada_EnUnaFilaQueNoEsMaterial_SeRechazaSinCambios()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await ActualizarReferenciaAsync(navegador, _filaManoDeObraId);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("filas de tipo Material", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        Assert.Equal(1500m, (await LeerFilaAsync(_filaManoDeObraId)).PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task ActualizacionConfirmada_EnUnaFilaDeOtroPresupuesto_NoSePuedeCambiandoElIdDeLaUrl()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await CambiarElCatalogoDeLaReferenciaAAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Administrador, responsable: false);

        var respuesta = await ActualizarReferenciaAsync(navegador, filaId, presupuestoId: _otroPresupuestoId);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.DoesNotContain("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
        Assert.Equal(100m, (await LeerFilaAsync(filaId)).PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task ActualizacionConfirmada_SinTokenAntifalsificacion_SeRechaza()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await CambiarElCatalogoDeLaReferenciaAAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        var respuesta = await navegador.PostAsync(
            $"/ConceptosPresupuesto/ActualizarReferencia/{_presupuestoId}?conceptoId={filaId}",
            new Dictionary<string, string> { ["confirmar"] = "true" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(100m, (await LeerFilaAsync(filaId)).PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task DosActualizacionesConfirmadasALaVez_SoloUnaCopiaYLaOtraDetectaConcurrencia()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await CambiarElCatalogoDeLaReferenciaAAsync();
        using EopDbContext contextoInicial = CrearContexto();
        var responsable = await contextoInicial.Usuarios.SingleAsync();

        async Task<ResultadoAccionGuardarBorrador> ActualizarYGuardarAsync()
        {
            using EopDbContext contexto = CrearContexto();
            var repositorio = new RepositorioConceptosPresupuesto(contexto);
            var almacen = new AlmacenBorradorEnMemoria();
            var sincronizador = new SincronizadorBorradorPresupuesto();
            var actualizar = new ActualizarReferenciaDeConcepto(repositorio, almacen, sincronizador);
            Assert.Null(await actualizar.EjecutarAsync(_presupuestoId, filaId, true, responsable, CancellationToken.None));

            var guardar = new GuardarBorradorPresupuesto(repositorio, almacen, sincronizador);
            return await guardar.EjecutarAsync(
                _presupuestoId, new ContextoEdicionBorrador(true, 1), responsable, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(ActualizarYGuardarAsync(), ActualizarYGuardarAsync());

        Assert.Single(resultados, resultado => resultado.GuardadoCorrectamente);
        Assert.Single(resultados, resultado => resultado.Estado == ResultadoGuardarBorrador.ConflictoConcurrencia);
        Assert.Equal(150m, (await LeerFilaAsync(filaId)).PrecioUnitario);
    }

    private static int ContarApariciones(string texto, string buscado) =>
        (texto.Length - texto.Replace(buscado, string.Empty).Length) / buscado.Length;
}
