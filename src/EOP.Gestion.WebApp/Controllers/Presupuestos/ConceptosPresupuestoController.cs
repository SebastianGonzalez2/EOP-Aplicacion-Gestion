using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.DTOs.Materiales;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.Interfaces.Usuarios;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Infrastructure.Consultas.Materiales;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.WebApp.ViewModels.Presupuestos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace EOP.Gestion.WebApp.Controllers;

public class ConceptosPresupuestoController : Controller
{
    // Cuántos materiales muestra cada página del selector de materiales.
    private const int MaterialesPorPagina = 10;

    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;
    private readonly ISincronizadorBorradorPresupuesto _sincronizador;
    private readonly ConsultarConceptosPresupuesto _consultarConceptos;
    private readonly ConsultarMateriales _consultarMateriales;
    private readonly ConsultarPresupuestoDetalle _consultarDetalle;
    private readonly AgregarConceptoManual _agregarConcepto;
    private readonly SeleccionarMaterialDeConcepto _seleccionarMaterial;
    private readonly CambiarProveedorDeConcepto _cambiarProveedor;
    private readonly ActualizarReferenciaDeConcepto _actualizarReferencia;
    private readonly RegistrarMargen _registrarMargen;
    private readonly CalcularMargenPresupuesto _calcularMargen;
    private readonly GuardarBorradorPresupuesto _guardarBorrador;
    private readonly DescartarCambiosPresupuesto _descartarCambios;
    private readonly ILogger<ConceptosPresupuestoController> _logger;

    public ConceptosPresupuestoController(
        IRepositorioUsuarios usuarios,
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ISincronizadorBorradorPresupuesto sincronizador,
        ConsultarConceptosPresupuesto consultarConceptos,
        ConsultarMateriales consultarMateriales,
        ConsultarPresupuestoDetalle consultarDetalle,
        AgregarConceptoManual agregarConcepto,
        SeleccionarMaterialDeConcepto seleccionarMaterial,
        CambiarProveedorDeConcepto cambiarProveedor,
        ActualizarReferenciaDeConcepto actualizarReferencia,
        RegistrarMargen registrarMargen,
        GuardarBorradorPresupuesto guardarBorrador,
        DescartarCambiosPresupuesto descartarCambios,
        CalcularMargenPresupuesto calcularMargen,
        ILogger<ConceptosPresupuestoController> logger)
    {
        _usuarios = usuarios;
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
        _sincronizador = sincronizador;
        _consultarConceptos = consultarConceptos;
        _consultarMateriales = consultarMateriales;
        _consultarDetalle = consultarDetalle;
        _agregarConcepto = agregarConcepto;
        _seleccionarMaterial = seleccionarMaterial;
        _cambiarProveedor = cambiarProveedor;
        _actualizarReferencia = actualizarReferencia;
        _registrarMargen = registrarMargen;
        _guardarBorrador = guardarBorrador;
        _descartarCambios = descartarCambios;
        _calcularMargen = calcularMargen;
        _logger = logger;
    }

    // UI-presupuestos: esta es la pantalla única del presupuesto. Desde el listado se entra
    // directo acá (ya no hay un paso intermedio de «Ver conceptos»): arriba se ve la información
    // general y abajo la tabla de conceptos. Los tres roles pueden consultarla; solo quien puede
    // editar ve las acciones de edición (US-001, US-003).
    [HttpGet]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> Index(
        int id,
        bool esperabaCambiosPendientes,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        bool puedeEditar = PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario);
        if (puedeEditar && esperabaCambiosPendientes)
        {
            string? mensajePerdidaOConflicto = await _sincronizador.EjecutarConBloqueoAsync(
                usuario.Id,
                async () =>
                {
                    await _almacenBorrador.CargarAsync(cancellationToken);
                    return await ControladorEstadoBorrador.VerificarNavegacionConCambiosPendientesAsync(
                        presupuesto.Id, usuario.Id, _repositorio, _almacenBorrador, cancellationToken);
                },
                cancellationToken);

            if (mensajePerdidaOConflicto != null)
            {
                TempData["MensajeError"] = mensajePerdidaOConflicto;
                return RedirectToAction(nameof(Index), new { id });
            }
        }

        return View(await PrepararPantallaAsync(
            presupuesto, usuario, new ConceptoManualViewModel(), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> Agregar(
        int id,
        ConceptoManualViewModel entrada,
        bool esperabaCambiosPendientes,
        int versionBorrador,
        string? huellaEstadoBase,
        string? borradorId,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);

        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(
            id, cancellationToken);

        if (presupuesto == null)
        {
            return NotFound();
        }

        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        decimal? cantidad = null;
        decimal? importe = null;
        decimal? porcentajeMargen = null;

        if (entrada.TipoId == TipoConcepto.MaterialId)
        {
            if (!ConceptoManualViewModel.InterpretarDecimal(
                entrada.Cantidad, out cantidad))
            {
                ModelState.AddModelError(
                    "Entrada.Cantidad",
                    "Ingresá una cantidad válida con hasta 10 decimales.");
            }
        }
        else if (entrada.TipoId == TipoConcepto.MargenId)
        {
            // S3-T07: el porcentaje se interpreta en el servidor.
            // Las reglas del porcentaje y de los tipos seleccionados
            // se comprueban después en el caso de uso RegistrarMargen.
            if (!ConceptoManualViewModel.InterpretarDecimal(
                entrada.PorcentajeMargen, out porcentajeMargen))
            {
                ModelState.AddModelError(
                    "Entrada.PorcentajeMargen",
                    "Ingresá un porcentaje válido.");
            }
        }
        else if (TipoConcepto.AdmiteImporteDirecto(entrada.TipoId))
        {
            if (!ConceptoManualViewModel.InterpretarDecimal(
                entrada.Importe, out importe))
            {
                ModelState.AddModelError(
                    "Entrada.Importe",
                    "Ingresá un importe válido con hasta 10 decimales.");
            }
        }

        if (ModelState.IsValid)
        {
            var contextoEdicion = new ContextoEdicionBorrador(
                esperabaCambiosPendientes, versionBorrador, huellaEstadoBase, borradorId);

            string? error;

            if (entrada.TipoId == TipoConcepto.MargenId)
            {
                // RF-27 + RF-05: el margen y su base se incorporan al borrador pendiente y se
                // persisten recién cuando el usuario presiona «Guardar borrador».
                error = await _registrarMargen.EjecutarAsync(
                    id,
                    porcentajeMargen,
                    entrada.TiposBaseSeleccionados,
                    usuario,
                    cancellationToken,
                    contextoEdicion);
            }
            else
            {
                // RF-05: el concepto se incorpora al borrador pendiente hasta guardar explícitamente.
                error = await _agregarConcepto.EjecutarAsync(
                    id,
                    entrada.TipoId,
                    entrada.Descripcion,
                    importe,
                    entrada.Moneda,
                    entrada.ReferenciaId,
                    cantidad,
                    usuario,
                    cancellationToken,
                    contextoEdicion);
            }

            if (error == null)
            {
                if (entrada.TipoId == TipoConcepto.MargenId)
                {
                    TempData["MensajeExito"] =
                        "El margen se agregó al borrador. Presioná Guardar borrador para confirmar los cambios.";
                }
                else
                {
                    TempData["MensajeExito"] =
                        "El concepto se agregó al borrador. Presioná Guardar borrador para confirmar los cambios.";
                }

                return RedirectToAction(nameof(Index), new { id });
            }

            // Si el borrador se perdió o quedó desactualizado por otra pestaña/usuario, redirigimos
            // a Index para mostrar el estado real vigente junto con el aviso claro al usuario.
            if (EsErrorDeEstadoBorrador(error))
            {
                TempData["MensajeError"] = error;
                return RedirectToAction(nameof(Index), new { id });
            }

            ModelState.AddModelError(string.Empty, error);
        }

        // Volvemos a mostrar la pantalla con el modal «Agregar concepto» abierto y con lo que el
        // usuario escribió, así corrige el error sin tener que cargar todo de nuevo.
        return View(
            nameof(Index),
            await PrepararPantallaAsync(
                presupuesto,
                usuario,
                entrada,
                cancellationToken));
    }

    // US-003 (RF-05): guardado explícito de los cambios pendientes del borrador.
    // Solo esta acción persiste definitivamente los conceptos y el margen en SQL Server.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> GuardarBorrador(
        int id,
        bool esperabaCambiosPendientes,
        int versionBorrador,
        string? huellaEstadoBase,
        string? borradorId,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        var contextoEdicion = new ContextoEdicionBorrador(
            esperabaCambiosPendientes, versionBorrador, huellaEstadoBase, borradorId);

        try
        {
            ResultadoAccionGuardarBorrador resultado = await _guardarBorrador.EjecutarAsync(
                id, contextoEdicion, usuario, cancellationToken);

            if (resultado.Estado == ResultadoGuardarBorrador.EdicionNoPermitida)
            {
                return Forbid();
            }

            if (resultado.GuardadoCorrectamente)
            {
                if (resultado.HuboCambiosGuardados)
                {
                    TempData["MensajeExito"] = "El borrador se guardó correctamente.";
                }
                else
                {
                    TempData["MensajeExito"] = "El borrador ya estaba guardado sin cambios pendientes.";
                }

                return RedirectToAction(nameof(Index), new { id });
            }

            TempData["MensajeError"] = resultado.MensajeError;
            return RedirectToAction(nameof(Index), new { id });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            // RNF-12: registramos solo el tipo de error y el identificador de solicitud,
            // sin datos personales ni mensajes crudos de SQL Server. Los cambios pendientes
            // quedan intactos en la sesión para que el usuario pueda reintentar.
            _logger.LogError(
                "No se pudo guardar el borrador del presupuesto {PresupuestoId}. Tipo de error: {TipoError}. Id de solicitud: {IdSolicitud}",
                id,
                excepcion.GetType().Name,
                HttpContext.TraceIdentifier);

            TempData["MensajeError"] =
                "No se pudo guardar el borrador por un error inesperado. Tus cambios pendientes se conservaron para que puedas reintentar.";

            return RedirectToAction(nameof(Index), new { id });
        }
    }

    // S3-T15 (US-003, RF-05): descarte voluntario de los cambios sin guardar del presupuesto.
    // Se llega acá desde el diálogo de confirmación de la pantalla (RNF-1). Solo borra el
    // borrador temporal de este usuario: los datos guardados en SQL Server no se tocan.
    //
    // versionBorrador y borradorId identifican el borrador que estaba viendo la pantalla. Si otra
    // pestaña lo cambió mientras tanto, el caso de uso no descarta nada y avisamos al usuario.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> DescartarCambios(
        int id,
        int versionBorrador,
        string? borradorId,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        // No pedimos permiso de edición: descartar no modifica el presupuesto, solo limpia datos
        // temporales que pertenecen al propio usuario (ver DescartarCambiosPresupuesto).
        ResultadoAccionDescartarCambios resultado = await _descartarCambios.EjecutarAsync(
            id, borradorId, versionBorrador, usuario, cancellationToken);

        if (resultado.Estado == ResultadoDescartarCambios.Descartado)
        {
            TempData["MensajeExito"] = resultado.Mensaje;
        }
        else if (resultado.Estado == ResultadoDescartarCambios.SinCambiosPendientes)
        {
            TempData["MensajeAdvertencia"] = resultado.Mensaje;
        }
        else
        {
            TempData["MensajeError"] = resultado.Mensaje;
        }

        return RedirectToAction(nameof(Index), new { id });
    }

    // US-007: pantalla para buscar y elegir a mano el material de una fila de tipo Material.
    // Buscar solo lee el catálogo; no crea ni modifica nada (RF-12).
    // UI-presupuestos: la búsqueda ya no recarga esta pantalla. El selector de materiales trae el
    // listado y las referencias con CatalogoMateriales y ReferenciasMaterial (más abajo), igual
    // que el modal «Agregar concepto».
    [HttpGet]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> SeleccionarMaterial(
        int id,
        int conceptoId,
        bool esperabaCambiosPendientes,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        // Entrar a la pantalla también exige poder editar: Operador solo si es el responsable
        // y el presupuesto en un estado editable. Ocultar el botón en la tabla no alcanza (RNF-4).
        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        // Si se llegó con indicador de cambios pendientes o con un Id temporal negativo (< 0),
        // verificamos que el borrador siga presente antes de intentar mostrar la fila.
        if (esperabaCambiosPendientes || conceptoId < 0)
        {
            string? mensajePerdidaOConflicto = await _sincronizador.EjecutarConBloqueoAsync(
                usuario.Id,
                async () =>
                {
                    await _almacenBorrador.CargarAsync(cancellationToken);
                    return await ControladorEstadoBorrador.VerificarNavegacionConCambiosPendientesAsync(
                        id, usuario.Id, _repositorio, _almacenBorrador, cancellationToken);
                },
                cancellationToken);

            if (mensajePerdidaOConflicto != null)
            {
                TempData["MensajeError"] = mensajePerdidaOConflicto;
                return RedirectToAction(nameof(Index), new { id });
            }
        }

        (FilaConceptoDto? fila, BorradorPresupuestoDto? borrador) = await ObtenerFilaYEstadoAsync(
            id, conceptoId, usuario.Id, cancellationToken);

        if (fila == null)
        {
            return NotFound();
        }

        if (fila.TipoId != TipoConcepto.MaterialId)
        {
            TempData["MensajeError"] = "Solo se puede seleccionar un material en las filas de tipo Material.";
            return RedirectToAction(nameof(Index), new { id });
        }

        return View(await PrepararSeleccionAsync(presupuesto, borrador, fila, null, null, cancellationToken));
    }

    // US-007 + RF-05: aplica al borrador pendiente la referencia que el usuario eligió para la fila.
    // Sirve para vincular una fila sin material y para sustituir el material de una fila que ya lo tenía (RF-13).
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> AsignarMaterial(
        int id,
        int conceptoId,
        int? referenciaId,
        int? materialId,
        bool esperabaCambiosPendientes,
        int versionBorrador,
        string? huellaEstadoBase,
        string? borradorId,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        var contextoEdicion = new ContextoEdicionBorrador(
            esperabaCambiosPendientes, versionBorrador, huellaEstadoBase, borradorId);

        string? error = await _seleccionarMaterial.EjecutarAsync(
            id, conceptoId, referenciaId, usuario, cancellationToken, contextoEdicion);
        if (error == null)
        {
            TempData["MensajeExito"] =
                "El material se asoció a la fila en el borrador. Presioná Guardar borrador para confirmar los cambios.";
            return RedirectToAction(nameof(Index), new { id });
        }

        if (EsErrorDeEstadoBorrador(error))
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        (FilaConceptoDto? fila, BorradorPresupuestoDto? borrador) = await ObtenerFilaYEstadoAsync(
            id, conceptoId, usuario.Id, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        // materialId y referenciaId solo sirven para que el selector vuelva a mostrar lo que el
        // usuario había elegido. No deciden nada: el caso de uso ya rechazó esa selección.
        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, borrador, fila, materialId, referenciaId, cancellationToken));
    }

    // S3-T12 (US-010A, RF-20) + RF-05: el usuario eligió otra referencia de proveedor del material
    // que la fila ya tiene, desde el panel de referencias de S3-T11. El cambio queda pendiente en el
    // borrador hasta que el usuario presione «Guardar borrador».
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> CambiarProveedor(
        int id,
        int conceptoId,
        int? referenciaId,
        bool esperabaCambiosPendientes,
        int versionBorrador,
        string? huellaEstadoBase,
        string? borradorId,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        // Mismo control que al abrir la pantalla: Operador solo si es el responsable y el
        // presupuesto está en un estado editable (RNF-4).
        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        var contextoEdicion = new ContextoEdicionBorrador(
            esperabaCambiosPendientes, versionBorrador, huellaEstadoBase, borradorId);

        string? error = await _cambiarProveedor.EjecutarAsync(
            id, conceptoId, referenciaId, usuario, cancellationToken, contextoEdicion);
        if (error == null)
        {
            TempData["MensajeExito"] =
                "El proveedor de la fila se actualizó en el borrador. Presioná Guardar borrador para confirmar los cambios.";
            return RedirectToAction(nameof(Index), new { id });
        }

        if (EsErrorDeEstadoBorrador(error))
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        (FilaConceptoDto? fila, BorradorPresupuestoDto? borrador) = await ObtenerFilaYEstadoAsync(
            id, conceptoId, usuario.Id, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, borrador, fila, null, null, cancellationToken));
    }

    // S3-T12 (RF-020) + RF-05: el usuario pidió, y confirmó en el diálogo, que la fila vuelva a copiar
    // los valores vigentes de la referencia que ya tiene. El cambio queda pendiente en el borrador
    // hasta que el usuario presione «Guardar borrador».
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> ActualizarReferencia(
        int id,
        int conceptoId,
        bool confirmar,
        bool esperabaCambiosPendientes,
        int versionBorrador,
        string? huellaEstadoBase,
        string? borradorId,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        // Un presupuesto aprobado o en cualquier estado no editable nunca se modifica (RNF-4).
        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        var contextoEdicion = new ContextoEdicionBorrador(
            esperabaCambiosPendientes, versionBorrador, huellaEstadoBase, borradorId);

        string? error = await _actualizarReferencia.EjecutarAsync(
            id, conceptoId, confirmar, usuario, cancellationToken, contextoEdicion);
        if (error == null)
        {
            TempData["MensajeExito"] =
                "Los valores de la fila se actualizaron en el borrador. Presioná Guardar borrador para confirmar los cambios.";
            return RedirectToAction(nameof(Index), new { id });
        }

        if (EsErrorDeEstadoBorrador(error))
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        (FilaConceptoDto? fila, BorradorPresupuestoDto? borrador) = await ObtenerFilaYEstadoAsync(
            id, conceptoId, usuario.Id, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, borrador, fila, null, null, cancellationToken));
    }

    // UI-presupuestos (US-007, US-011): primer paso del selector de materiales. Devuelve un pedazo
    // de HTML (vista parcial) con una página del catálogo, que el selector inserta sin recargar la
    // pantalla. Sin texto de búsqueda muestra todos los materiales, ordenados por nombre.
    //
    // Reutiliza la misma consulta del Catálogo de Materiales (ConsultarMateriales). Solo lee: no
    // crea materiales ni toca precios ni proveedores (RF-12). Lo usan tanto el modal «Agregar
    // concepto» como la pantalla «Seleccionar material» de una fila.
    [HttpGet]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> CatalogoMateriales(
        int id,
        string? busqueda,
        int pagina,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        // El selector es parte de la edición: solo lo usa quien puede modificar el presupuesto (RNF-4).
        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        List<MaterialListadoDto> materiales = await _consultarMateriales.EjecutarAsync(
            busqueda, null, null, cancellationToken);

        // Paginamos en memoria sobre el resultado de la consulta del catálogo. El catálogo de EOP
        // es chico, así que alcanza y nos evita escribir otra consulta distinta a la del catálogo.
        int cantidadTotal = materiales.Count;
        int cantidadPaginas = (cantidadTotal + MaterialesPorPagina - 1) / MaterialesPorPagina;

        int paginaActual = pagina;
        if (paginaActual < 1)
        {
            paginaActual = 1;
        }
        else if (cantidadPaginas > 0 && paginaActual > cantidadPaginas)
        {
            paginaActual = cantidadPaginas;
        }

        List<MaterialListadoDto> materialesDeLaPagina = materiales
            .Skip((paginaActual - 1) * MaterialesPorPagina)
            .Take(MaterialesPorPagina)
            .ToList();

        var listado = new ListadoMaterialesSelectorViewModel
        {
            Busqueda = busqueda,
            Materiales = materialesDeLaPagina,
            PaginaActual = paginaActual,
            CantidadPaginas = cantidadPaginas,
            CantidadTotal = cantidadTotal,
            MaterialesPorPagina = MaterialesPorPagina
        };

        return PartialView("_ListadoMaterialesSelector", listado);
    }

    // UI-presupuestos (US-010A): segundo paso del selector. Muestra las referencias (proveedor,
    // precio, moneda y fecha de actualización) del material que el usuario eligió, para que elija
    // una expresamente. Ninguna viene marcada de antemano, aunque haya una sola (RF-19).
    //
    // - paraFila: true cuando el selector está en la pantalla «Seleccionar material» de una fila;
    //   cambia solo el nombre del campo que se envía (referenciaId en vez de Entrada.ReferenciaId).
    // - referenciaSeleccionadaId: lo que el usuario ya había elegido antes de que el servidor
    //   rechazara el envío, para no hacerlo elegir de nuevo.
    // - referenciaActualId: la referencia que la fila ya tiene; se muestra pero no se puede elegir.
    // Los tres son solo de presentación: las reglas las vuelve a comprobar el caso de uso al enviar.
    [HttpGet]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> ReferenciasMaterial(
        int id,
        int materialId,
        bool paraFila,
        int? referenciaSeleccionadaId,
        int? referenciaActualId,
        CancellationToken cancellationToken)
    {
        Usuario? usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Forbid();
        }

        Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(id, cancellationToken);
        if (presupuesto == null)
        {
            return NotFound();
        }

        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        // Es la misma consulta que usa el panel de referencias de S3-T11. Solo lee el catálogo.
        MaterialListadoDto? material = await _consultarMateriales.ObtenerConReferenciasAsync(
            materialId, cancellationToken);

        if (material == null)
        {
            return NotFound();
        }

        string nombreCampo = "Entrada.ReferenciaId";
        if (paraFila)
        {
            nombreCampo = "referenciaId";
        }

        var referencias = new ReferenciasMaterialSelectorViewModel
        {
            Material = material,
            NombreCampoReferencia = nombreCampo,
            ReferenciaSeleccionadaId = referenciaSeleccionadaId,
            ReferenciaActualId = referenciaActualId
        };

        return PartialView("_ReferenciasMaterialSelector", referencias);
    }

    private static bool EsErrorDeEstadoBorrador(string error)
    {
        return string.Equals(error, MensajesBorradorPresupuesto.CambiosPendientesPerdidos, StringComparison.Ordinal)
            || string.Equals(error, MensajesBorradorPresupuesto.BorradorModificadoEnOtraPestana, StringComparison.Ordinal)
            || string.Equals(error, MensajesBorradorPresupuesto.PresupuestoModificadoSinPendientes, StringComparison.Ordinal)
            || string.Equals(error, MensajesBorradorPresupuesto.CambiosDescartadosEnOtraPestana, StringComparison.Ordinal);
    }

    private async Task<Usuario?> ObtenerUsuarioAsync(CancellationToken cancellationToken)
    {
        string? identidad = User.GetObjectId();
        if (string.IsNullOrWhiteSpace(identidad))
        {
            return null;
        }

        return await _usuarios.ObtenerPorIdentidadExternaAsync(identidad, cancellationToken);
    }

    // Lee el borrador y la fila bajo el sincronizador para asegurar una lectura consistente con
    // cualquier escritura o guardado concurrente de la misma sesión.
    private Task<(FilaConceptoDto? Fila, BorradorPresupuestoDto? Borrador)> ObtenerFilaYEstadoAsync(
        int presupuestoId, int conceptoId, int usuarioId, CancellationToken cancellationToken)
    {
        return _sincronizador.EjecutarConBloqueoAsync(
            usuarioId,
            async () =>
            {
                await _almacenBorrador.CargarAsync(cancellationToken);
                BorradorPresupuestoDto? borrador = _almacenBorrador.Obtener(presupuestoId, usuarioId);

                if (borrador != null)
                {
                    foreach (ConceptoBorradorDto concepto in borrador.Conceptos)
                    {
                        if (concepto.Id == conceptoId)
                        {
                            return (concepto.AFilaDto(), (BorradorPresupuestoDto?)borrador);
                        }
                    }

                    return ((FilaConceptoDto?)null, (BorradorPresupuestoDto?)borrador);
                }

                FilaConceptoDto? filaPersistida = await _consultarConceptos.ObtenerFilaAsync(
                    presupuestoId, conceptoId, cancellationToken);

                return (filaPersistida, (BorradorPresupuestoDto?)null);
            },
            cancellationToken);
    }

    private async Task<SeleccionarMaterialViewModel> PrepararSeleccionAsync(
        Presupuesto presupuesto,
        BorradorPresupuestoDto? borrador,
        FilaConceptoDto fila,
        int? materialElegidoId,
        int? referenciaElegidaId,
        CancellationToken cancellationToken)
    {
        // UI-presupuestos: la búsqueda de materiales ya no se hace acá. La pantalla trae el
        // selector de materiales, que pide el listado y las referencias por su cuenta (ver
        // CatalogoMateriales y ReferenciasMaterial). Nada se preselecciona a partir del Concepto
        // detectado (RF-12): el usuario siempre elige expresamente.

        // S3-T11 (US-010A): si la fila ya tiene material, se traen sus referencias de precio para
        // mostrarlas sin que el usuario tenga que buscarlo de nuevo. Solo se lee el catálogo.
        MaterialListadoDto? materialActual = null;
        if (fila.MaterialId != null)
        {
            materialActual = await _consultarMateriales.ObtenerConReferenciasAsync(
                fila.MaterialId.Value, cancellationToken);
        }

        bool tieneCambiosPendientes = borrador != null && borrador.TieneCambiosPendientes;
        string? borradorId = tieneCambiosPendientes ? borrador!.BorradorId : null;
        int versionBorrador = tieneCambiosPendientes ? borrador!.Version : 0;
        string? huellaEstadoBase = tieneCambiosPendientes
            ? borrador!.HuellaEstadoBase
            : await _repositorio.ObtenerHuellaPersistidaAsync(presupuesto.Id, cancellationToken);

        return new SeleccionarMaterialViewModel
        {
            PresupuestoId = presupuesto.Id,
            Codigo = presupuesto.Codigo,
            TieneCambiosPendientes = tieneCambiosPendientes,
            BorradorId = borradorId,
            VersionBorrador = versionBorrador,
            HuellaEstadoBase = huellaEstadoBase,
            Fila = fila,
            MaterialActual = materialActual,
            MaterialElegidoId = materialElegidoId,
            ReferenciaElegidaId = referenciaElegidaId
        };
    }

    private async Task<ConceptosPresupuestoViewModel> PrepararPantallaAsync(
        Presupuesto presupuesto, Usuario usuario, ConceptoManualViewModel entrada,
        CancellationToken cancellationToken)
    {
        bool puedeAgregar = PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario);

        // UI-presupuestos: la información general (cliente, tipo de proyecto, responsable, fechas
        // y estado) se muestra arriba de la tabla. Es la misma consulta que usaba la pantalla de
        // detalle que se eliminó, así no duplicamos lógica.
        PresupuestoDetalleDto? informacion = await _consultarDetalle.EjecutarAsync(
            presupuesto.Id, cancellationToken);

        if (informacion == null)
        {
            // No debería pasar: el presupuesto existe porque lo acabamos de leer. Si igual pasa
            // (por ejemplo, se borró en el medio), preferimos cortar acá con un error claro.
            throw new InvalidOperationException(
                $"No se encontró la información general del presupuesto {presupuesto.Id}.");
        }

        List<FilaConceptoDto> filas;
        bool tieneCambiosPendientes = false;
        string? borradorId = null;
        int versionBorrador = 0;
        string? huellaEstadoBase = null;

        // Solo mostramos cambios pendientes cuando el usuario sigue autorizado a editar el
        // presupuesto. En modo solo lectura siempre mostramos los datos efectivamente guardados.
        BorradorPresupuestoDto? borrador = null;
        if (puedeAgregar)
        {
            borrador = await _sincronizador.EjecutarConBloqueoAsync(
                usuario.Id,
                async () =>
                {
                    await _almacenBorrador.CargarAsync(cancellationToken);
                    return _almacenBorrador.Obtener(presupuesto.Id, usuario.Id);
                },
                cancellationToken);
        }

        if (borrador != null && borrador.TieneCambiosPendientes)
        {
            filas = new List<FilaConceptoDto>();
            foreach (ConceptoBorradorDto concepto in borrador.Conceptos)
            {
                filas.Add(concepto.AFilaDto());
            }

            tieneCambiosPendientes = true;
            borradorId = borrador.BorradorId;
            versionBorrador = borrador.Version;
            huellaEstadoBase = borrador.HuellaEstadoBase;
        }
        else
        {
            filas = await _consultarConceptos.ObtenerFilasAsync(
                presupuesto.Id, cancellationToken);

            if (puedeAgregar)
            {
                huellaEstadoBase = await _repositorio.ObtenerHuellaPersistidaAsync(
                    presupuesto.Id, cancellationToken);
            }
        }

        // Agrupamos los subtotales por tipo de concepto.
        // Los importes en USD y UYU se mantienen separados.
        var tiposBaseMargen = AgrupadorSubtotalesPorTipo.Agrupar(filas);

        // Calculamos el margen utilizando los subtotales actuales
        // y la cotización vigente cuando sea necesaria.
        var resultadoMargen = await _calcularMargen.EjecutarAsync(filas, cancellationToken);

        // RF-27: un presupuesto puede tener como máximo un concepto Margen.
        // Si ya existe uno, no mostramos la opción de agregar otro.
        bool tieneMargen = filas.Any(fila =>
            fila.TipoId == TipoConcepto.MargenId);

        var tipos = new List<TipoConceptoOpcionDto>();

        if (puedeAgregar)
        {
            var tiposRegistrados = await _consultarConceptos.ObtenerTiposAsync(cancellationToken);

            // Mostramos los tipos normales y habilitamos Margen solamente
            // cuando todavía no existe y hay conceptos para usar como base.
            tipos = tiposRegistrados.Where(tipo =>
                tipo.Id == TipoConcepto.MaterialId
                || TipoConcepto.AdmiteImporteDirecto(tipo.Id)
                || (tipo.Id == TipoConcepto.MargenId
                    && !tieneMargen
                    && tiposBaseMargen.Count > 0))
                .ToList();
        }

        var pantalla = new ConceptosPresupuestoViewModel
        {
            PresupuestoId = presupuesto.Id,
            Codigo = presupuesto.Codigo,
            Informacion = informacion,
            PuedeAgregar = puedeAgregar,
            MensajeErrorCotizacion = resultadoMargen.MensajeError,
            CotizacionUtilizada = resultadoMargen.Cotizacion,
            TieneCambiosPendientes = tieneCambiosPendientes,
            BorradorId = borradorId,
            VersionBorrador = versionBorrador,
            HuellaEstadoBase = huellaEstadoBase,
            Filas = filas,
            TiposBaseMargen = tiposBaseMargen,
            Tipos = tipos,
            Entrada = entrada
        };

        // UI-presupuestos: los materiales del catálogo ya no se cargan acá. El modal «Agregar
        // concepto» los pide recién cuando el usuario elige el tipo Material (CatalogoMateriales),
        // así no consultamos todo el catálogo cada vez que se abre el presupuesto.

        return pantalla;
    }
}
