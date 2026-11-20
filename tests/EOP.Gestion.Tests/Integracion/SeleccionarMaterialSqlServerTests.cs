using System.Net;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Entidades.Usuarios;
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

        async Task<string?> SeleccionarAsync(int conceptoId)
        {
            using EopDbContext contexto = CrearContexto();
            var casoDeUso = new SeleccionarMaterialDeConcepto(new RepositorioConceptosPresupuesto(contexto));
            return await casoDeUso.EjecutarAsync(_presupuestoId, conceptoId, _cableProveedorAId, responsable, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(SeleccionarAsync(_filaSinMaterialId), SeleccionarAsync(otraFila.Id));

        Assert.Single(resultados, resultado => resultado is null);
        Assert.Contains(resultados, resultado => resultado?.Contains("ya están en otra fila") == true);
        Assert.Equal(1, await contextoInicial.ConceptosPresupuesto.CountAsync(c => c.MaterialProveedorId == _cableProveedorAId));
    }

    [FactConSqlServer]
    public async Task DosSustitucionesSimultaneasDeLaMismaFila_TerminanSinBloquearseYDejanUnaReferenciaValida()
    {
        using EopDbContext contextoInicial = CrearContexto();
        var responsable = await contextoInicial.Usuarios.SingleAsync();

        async Task<string?> SeleccionarAsync(int referenciaId)
        {
            using EopDbContext contexto = CrearContexto();
            var casoDeUso = new SeleccionarMaterialDeConcepto(new RepositorioConceptosPresupuesto(contexto));
            return await casoDeUso.EjecutarAsync(_presupuestoId, _filaSinMaterialId, referenciaId, responsable, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(SeleccionarAsync(_cableProveedorAId), SeleccionarAsync(_cableProveedorBId));

        Assert.All(resultados, Assert.Null);
        var fila = await LeerFilaAsync(_filaSinMaterialId);
        Assert.Contains(fila.MaterialProveedorId, new int?[] { _cableProveedorAId, _cableProveedorBId });
        Assert.Equal(TextoDetectadoPorIa, fila.ConceptoDetectado);
    }

    private static int ContarApariciones(string texto, string buscado) =>
        (texto.Length - texto.Replace(buscado, string.Empty).Length) / buscado.Length;
}
