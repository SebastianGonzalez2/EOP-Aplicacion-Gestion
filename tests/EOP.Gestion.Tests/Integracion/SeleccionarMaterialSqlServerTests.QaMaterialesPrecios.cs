using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S3-T28 (QA de materiales y precios; US-007, US-010A, RF-19, RF-20, DT-11): recorridos que cruzan
// el catálogo, el borrador pendiente y el guardado en SQL Server. Usan la misma base y los mismos
// helpers que el resto de SeleccionarMaterialSqlServerTests.
public partial class SeleccionarMaterialSqlServerTests
{
    [FactConSqlServer]
    public async Task CatalogoEditadoDesdeSuPantalla_ElPresupuestoConservaValoresYSubtotalHastaActualizarlosExpresamente()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        int cableId = await MaterialDeLaReferenciaAsync(_cableProveedorAId);
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Supervisor);

        // US-071: el catálogo se modifica por su propio formulario, no escribiendo en la base.
        // Los precios van sin decimales para no depender de la cultura de la máquina.
        var edicion = await navegador.PostAsync("/Materiales/Editar", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await navegador.ObtenerTokenAntifalsificacionAsync($"/Materiales/Editar?materialId={cableId}"),
            ["MaterialId"] = cableId.ToString(),
            ["Nombre"] = "Cable renombrado",
            ["Unidad"] = nameof(UnidadMedida.Unidad),
            ["Referencias[0].RelacionId"] = _cableProveedorAId.ToString(),
            ["Referencias[0].PrecioUnitario"] = "150",
            ["Referencias[0].Moneda"] = "USD",
            ["Referencias[1].RelacionId"] = _cableProveedorBId.ToString(),
            ["Referencias[1].PrecioUnitario"] = "3",
            ["Referencias[1].Moneda"] = "USD"
        });
        Assert.Equal(HttpStatusCode.Redirect, edicion.StatusCode);
        string catalogoEditado = await FotoDelCatalogoAsync();
        Assert.Contains("Cable renombrado", catalogoEditado);

        await ComprobarConceptoAsync(filaId, _cableProveedorAId, "Cable de cobre", UnidadMedida.Metro, 100m, Moneda.UYU, 1000m);
        string tabla = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Cable de cobre", tabla);
        Assert.Contains("UYU 100", tabla);
        Assert.DoesNotContain("Cable renombrado", tabla);

        // El panel muestra lo vigente y ofrece actualizar, pero consultarlo no cambia la fila.
        string seleccion = await LeerPaginaAsync(navegador, RutaSeleccion(filaId));
        Assert.Contains("Referencias de precios de Cable renombrado", seleccion);
        Assert.Contains("Actualizar valores", seleccion);
        await ComprobarConceptoAsync(filaId, _cableProveedorAId, "Cable de cobre", UnidadMedida.Metro, 100m, Moneda.UYU, 1000m);

        await EnviarConBorradorAsync(navegador, "ActualizarReferencia", HttpStatusCode.Redirect,
            ("conceptoId", filaId.ToString()), ("confirmar", "true"));
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        await ComprobarConceptoAsync(filaId, _cableProveedorAId, "Cable renombrado", UnidadMedida.Unidad, 150m, Moneda.USD, 1500m);
        Assert.Equal(catalogoEditado, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task CatalogoCambiaEntreLaSeleccionYElGuardado_SeGuardanLosValoresCopiadosAlSeleccionar()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // Quedan pendientes una fila existente (selección) y una nueva (alta manual) del mismo material.
        await EnviarConBorradorAsync(navegador, "AsignarMaterial", HttpStatusCode.Redirect,
            ("conceptoId", _filaSinMaterialId.ToString()), ("referenciaId", _cableProveedorBId.ToString()));
        await AgregarMaterialAsync(navegador, _cableProveedorAId, "2", HttpStatusCode.Redirect);

        await CambiarElCatalogoDeLaReferenciaAAsync();
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorBId)
                .ExecuteUpdateAsync(c => c.SetProperty(r => r.PrecioUnitario, 99m).SetProperty(r => r.Moneda, Moneda.UYU));
        }
        string catalogoCambiado = await FotoDelCatalogoAsync();

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        int filaNuevaId = await IdDeLaFilaConReferenciaAsync(_cableProveedorAId);
        await ComprobarConceptoAsync(_filaSinMaterialId, _cableProveedorBId, "Cable de cobre", UnidadMedida.Metro, 2.5m, Moneda.USD, 75m);
        await ComprobarConceptoAsync(filaNuevaId, _cableProveedorAId, "Cable de cobre", UnidadMedida.Metro, 100m, Moneda.UYU, 200m);

        // Otra sesión que vuelve a consultar el presupuesto ve lo guardado, no el catálogo de hoy.
        var otraSesion = await IniciarSesionAsync(
            fabrica, RolUsuario.Supervisor, responsable: false, correo: "consulta@eop.com.uy");
        string tabla = await LeerPaginaAsync(otraSesion, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        Assert.Contains("Cable de cobre", tabla);
        Assert.Contains("USD 2,5", tabla);
        Assert.Contains("UYU 100", tabla);
        Assert.DoesNotContain("Cable renombrado", tabla);
        Assert.Equal(catalogoCambiado, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task DuplicadoContraFilasPendientes_SeRechazaEnLasTresOperacionesYElBorradorSeConserva()
    {
        string catalogoAntes = await FotoDelCatalogoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        // RF-20: el mismo material con proveedores distintos también se admite en el alta manual.
        await AgregarMaterialAsync(navegador, _cableProveedorAId, "2", HttpStatusCode.Redirect);
        await AgregarMaterialAsync(navegador, _cableProveedorBId, "3", HttpStatusCode.Redirect);
        var pendientes = await LeerEstadoDelBorradorAsync(navegador);
        string tablaPendiente = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        int filaPendienteA = int.Parse(Regex.Match(tablaPendiente, @"conceptoId=(-\d+)").Groups[1].Value);

        string alta = await AgregarMaterialAsync(navegador, _cableProveedorAId, "7", HttpStatusCode.OK);
        Assert.Contains("ya están en el presupuesto", alta);
        Assert.Equal(pendientes, await LeerEstadoDelBorradorAsync(navegador));

        string seleccion = await EnviarConBorradorAsync(navegador, "AsignarMaterial", HttpStatusCode.OK,
            ("conceptoId", _filaSinMaterialId.ToString()), ("referenciaId", _cableProveedorBId.ToString()));
        Assert.Contains("ya están en otra fila", seleccion);
        Assert.Equal(pendientes, await LeerEstadoDelBorradorAsync(navegador));

        string cambio = await EnviarConBorradorAsync(navegador, "CambiarProveedor", HttpStatusCode.OK,
            ("conceptoId", filaPendienteA.ToString()), ("referenciaId", _cableProveedorBId.ToString()));
        Assert.Contains("ya están en otra fila", cambio);
        Assert.Equal(pendientes, await LeerEstadoDelBorradorAsync(navegador));

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        await ComprobarConceptoAsync(await IdDeLaFilaConReferenciaAsync(_cableProveedorAId),
            _cableProveedorAId, "Cable de cobre", UnidadMedida.Metro, 100m, Moneda.UYU, 200m);
        await ComprobarConceptoAsync(await IdDeLaFilaConReferenciaAsync(_cableProveedorBId),
            _cableProveedorBId, "Cable de cobre", UnidadMedida.Metro, 2.5m, Moneda.USD, 7.5m);
        Assert.Null((await LeerFilaAsync(_filaSinMaterialId)).MaterialProveedorId);
        Assert.Equal(catalogoAntes, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task FilaSinGuardar_AdmiteCambiarProveedorMaterialYValores_YGuardaSoloElUltimoEstado()
    {
        int filasAntes = await ContarFilasAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);
        await AgregarMaterialAsync(navegador, _cableProveedorAId, "2", HttpStatusCode.Redirect);
        string tabla = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        string filaTemporal = Regex.Match(tabla, @"conceptoId=(-\d+)").Groups[1].Value;

        await EnviarConBorradorAsync(navegador, "CambiarProveedor", HttpStatusCode.Redirect,
            ("conceptoId", filaTemporal), ("referenciaId", _cableProveedorBId.ToString()));
        await EnviarConBorradorAsync(navegador, "AsignarMaterial", HttpStatusCode.Redirect,
            ("conceptoId", filaTemporal), ("referenciaId", _cableProveedorAId.ToString()));
        await CambiarElCatalogoDeLaReferenciaAAsync();
        string catalogoCambiado = await FotoDelCatalogoAsync();

        string seleccion = await LeerPaginaAsync(navegador, RutaSeleccion(int.Parse(filaTemporal)));
        Assert.Contains("Actualizar valores", seleccion);
        await EnviarConBorradorAsync(navegador, "ActualizarReferencia", HttpStatusCode.Redirect,
            ("conceptoId", filaTemporal), ("confirmar", "true"));
        Assert.Equal(filasAntes, await ContarFilasAsync());

        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        Assert.Equal(filasAntes + 1, await ContarFilasAsync());
        await ComprobarConceptoAsync(await IdDeLaFilaConReferenciaAsync(_cableProveedorAId),
            _cableProveedorAId, "Cable renombrado", UnidadMedida.Unidad, 150m, Moneda.USD, 300m);
        using EopDbContext contexto = CrearContexto();
        Assert.False(await contexto.ConceptosPresupuesto.AnyAsync(c => c.MaterialProveedorId == _cableProveedorBId));
        Assert.Equal(catalogoCambiado, await FotoDelCatalogoAsync());
    }

    [FactConSqlServer]
    public async Task CombinacionLiberadaEnElBorrador_PuedeUsarseEnUnaFilaNuevaAntesDeGuardar()
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await EnviarConBorradorAsync(navegador, "CambiarProveedor", HttpStatusCode.Redirect,
            ("conceptoId", filaId.ToString()), ("referenciaId", _cableProveedorBId.ToString()));
        await AgregarMaterialAsync(navegador, _cableProveedorAId, "4", HttpStatusCode.Redirect);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        await ComprobarConceptoAsync(filaId, _cableProveedorBId, "Cable de cobre", UnidadMedida.Metro, 2.5m, Moneda.USD, 25m);
        await ComprobarConceptoAsync(await IdDeLaFilaConReferenciaAsync(_cableProveedorAId),
            _cableProveedorAId, "Cable de cobre", UnidadMedida.Metro, 100m, Moneda.UYU, 400m);
    }

    [TheoryConSqlServer]
    [InlineData(EstadoPresupuesto.ParaRevisionInterna, RolUsuario.Operador, true)]
    [InlineData(EstadoPresupuesto.ParaRevisionInterna, RolUsuario.Administrador, false)]
    [InlineData(EstadoPresupuesto.SolicitudDeModificacionDelCliente, RolUsuario.Operador, true)]
    [InlineData(EstadoPresupuesto.SolicitudDeModificacionDelCliente, RolUsuario.Supervisor, false)]
    public async Task EstadosEditables_PermitenSeleccionarCambiarProveedorYActualizarSinAlterarElEstado(
        EstadoPresupuesto estado, RolUsuario rol, bool responsable)
    {
        int filaId = await CrearFilaConCableElegidoAsync();
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Presupuestos.Where(p => p.Id == _presupuestoId)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.Estado, estado));
        }
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, rol, responsable);

        await EnviarConBorradorAsync(navegador, "CambiarProveedor", HttpStatusCode.Redirect,
            ("conceptoId", filaId.ToString()), ("referenciaId", _cableProveedorBId.ToString()));
        await EnviarConBorradorAsync(navegador, "AsignarMaterial", HttpStatusCode.Redirect,
            ("conceptoId", _filaSinMaterialId.ToString()), ("referenciaId", _cableProveedorAId.ToString()));
        await CambiarElCatalogoDeLaReferenciaAAsync();
        await EnviarConBorradorAsync(navegador, "ActualizarReferencia", HttpStatusCode.Redirect,
            ("conceptoId", _filaSinMaterialId.ToString()), ("confirmar", "true"));
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        // La fila que cambió de proveedor tomó el nombre y la unidad de antes del cambio del catálogo.
        await ComprobarConceptoAsync(filaId, _cableProveedorBId, "Cable de cobre", UnidadMedida.Metro, 2.5m, Moneda.USD, 25m);
        await ComprobarConceptoAsync(_filaSinMaterialId, _cableProveedorAId, "Cable renombrado", UnidadMedida.Unidad, 150m, Moneda.USD, 4500m);
        using EopDbContext lectura = CrearContexto();
        Assert.Equal(estado, await lectura.Presupuestos.Where(p => p.Id == _presupuestoId).Select(p => p.Estado).SingleAsync());
    }

    [FactConSqlServer]
    public async Task NombreYPrecioEnElLimite_PasanPorElBorradorYSeGuardanSinPerderDatos()
    {
        // DT-11: 200 caracteres de nombre y los 28 dígitos de decimal(28,10).
        string nombreLargo = new('N', 200);
        const decimal precioPreciso = 123456789012345678.0123456789m;
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Materiales.Where(m => m.Nombre == "Cable de cobre")
                .ExecuteUpdateAsync(c => c.SetProperty(m => m.Nombre, nombreLargo));
            await contexto.MaterialProveedores.Where(r => r.Id == _cableProveedorBId)
                .ExecuteUpdateAsync(c => c.SetProperty(r => r.PrecioUnitario, precioPreciso));
        }
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        var navegador = await IniciarSesionAsync(fabrica, RolUsuario.Operador);

        await AgregarMaterialAsync(navegador, _cableProveedorBId, "1", HttpStatusCode.Redirect);
        Assert.Equal(HttpStatusCode.Redirect, (await GuardarBorradorAsync(navegador)).StatusCode);

        await ComprobarConceptoAsync(await IdDeLaFilaConReferenciaAsync(_cableProveedorBId),
            _cableProveedorBId, nombreLargo, UnidadMedida.Metro, precioPreciso, Moneda.USD, precioPreciso);
    }

    // Lee los campos ocultos que los formularios de la pantalla mandan junto con cada cambio.
    private async Task<Dictionary<string, string>> LeerEstadoDelBorradorAsync(FabricaConEntraSimulado.Navegador navegador)
    {
        string html = await LeerPaginaAsync(navegador, $"/ConceptosPresupuesto/Index/{_presupuestoId}");
        string Valor(string nombre, string porDefecto)
        {
            string valor = Regex.Match(html, $"name=\"{nombre}\" value=\"([^\"]*)\"").Groups[1].Value;
            return valor.Length == 0 ? porDefecto : valor;
        }

        return new Dictionary<string, string>
        {
            ["esperabaCambiosPendientes"] = Valor("esperabaCambiosPendientes", "false"),
            ["borradorId"] = Valor("borradorId", ""),
            ["versionBorrador"] = Valor("versionBorrador", "0"),
            ["huellaEstadoBase"] = Valor("huellaEstadoBase", "")
        };
    }

    // A diferencia de AsignarAsync y CambiarProveedorAsync, manda el estado del borrador vigente,
    // así se pueden encadenar varios cambios pendientes. Devuelve el HTML de la respuesta.
    private async Task<string> EnviarConBorradorAsync(
        FabricaConEntraSimulado.Navegador navegador, string accion, HttpStatusCode esperado,
        params (string Nombre, string Valor)[] campos)
    {
        var formulario = await LeerEstadoDelBorradorAsync(navegador);
        formulario["__RequestVerificationToken"] =
            await navegador.ObtenerTokenAntifalsificacionAsync($"/ConceptosPresupuesto/Index/{_presupuestoId}");
        foreach (var (nombre, valor) in campos)
        {
            formulario[nombre] = valor;
        }

        var respuesta = await navegador.PostAsync($"/ConceptosPresupuesto/{accion}/{_presupuestoId}", formulario);
        Assert.Equal(esperado, respuesta.StatusCode);
        return WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
    }

    private Task<string> AgregarMaterialAsync(
        FabricaConEntraSimulado.Navegador navegador, int referenciaId, string cantidad, HttpStatusCode esperado) =>
        EnviarConBorradorAsync(navegador, "Agregar", esperado,
            ("Entrada.TipoId", TipoConcepto.MaterialId.ToString()),
            ("Entrada.ReferenciaId", referenciaId.ToString()),
            ("Entrada.Cantidad", cantidad));

    private async Task<int> ContarFilasAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.ConceptosPresupuesto.CountAsync();
    }

    private async Task<int> IdDeLaFilaConReferenciaAsync(int referenciaId)
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.ConceptosPresupuesto
            .Where(c => c.PresupuestoId == _presupuestoId && c.MaterialProveedorId == referenciaId)
            .Select(c => c.Id)
            .SingleAsync();
    }

    // Lee la fila con la misma consulta que usa la pantalla, desde un contexto nuevo.
    private async Task ComprobarConceptoAsync(
        int conceptoId, int referenciaId, string nombre, UnidadMedida unidad, decimal precio, Moneda moneda, decimal subtotal)
    {
        using EopDbContext contexto = CrearContexto();
        FilaConceptoDto? fila = await new ConsultarConceptosPresupuesto(contexto)
            .ObtenerFilaAsync(_presupuestoId, conceptoId, CancellationToken.None);

        Assert.NotNull(fila);
        Assert.Equal(referenciaId, fila.MaterialProveedorId);
        Assert.Equal(nombre, fila.Concepto);
        Assert.Equal(unidad, fila.Unidad);
        Assert.Equal(precio, fila.PrecioUnitario);
        Assert.Equal(moneda, fila.Moneda);
        Assert.Equal(subtotal, fila.Subtotal);
    }
}
