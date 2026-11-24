using System.Globalization;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Web;

namespace EOP.Gestion.WebApp.Controllers;

public class ConceptosPresupuestoController : Controller
{
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;
    private readonly ConsultarConceptosPresupuesto _consultarConceptos;
    private readonly ConsultarMateriales _consultarMateriales;
    private readonly AgregarConceptoManual _agregarConcepto;
    private readonly SeleccionarMaterialDeConcepto _seleccionarMaterial;
    private readonly CambiarProveedorDeConcepto _cambiarProveedor;
    private readonly ActualizarReferenciaDeConcepto _actualizarReferencia;
    private readonly RegistrarMargen _registrarMargen;
    private readonly GuardarBorradorPresupuesto _guardarBorrador;
    private readonly ILogger<ConceptosPresupuestoController> _logger;

    public ConceptosPresupuestoController(
        IRepositorioUsuarios usuarios,
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ConsultarConceptosPresupuesto consultarConceptos,
        ConsultarMateriales consultarMateriales,
        AgregarConceptoManual agregarConcepto,
        SeleccionarMaterialDeConcepto seleccionarMaterial,
        CambiarProveedorDeConcepto cambiarProveedor,
        ActualizarReferenciaDeConcepto actualizarReferencia,
        RegistrarMargen registrarMargen,
        GuardarBorradorPresupuesto guardarBorrador,
        ILogger<ConceptosPresupuestoController> logger)
    {
        _usuarios = usuarios;
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
        _consultarConceptos = consultarConceptos;
        _consultarMateriales = consultarMateriales;
        _agregarConcepto = agregarConcepto;
        _seleccionarMaterial = seleccionarMaterial;
        _cambiarProveedor = cambiarProveedor;
        _actualizarReferencia = actualizarReferencia;
        _registrarMargen = registrarMargen;
        _guardarBorrador = guardarBorrador;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> Index(int id, int tipoId, string? busqueda, CancellationToken cancellationToken)
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

        return View(await PrepararPantallaAsync(
            presupuesto, usuario, new ConceptoManualViewModel { TipoId = tipoId }, busqueda, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> Agregar(
        int id,
        ConceptoManualViewModel entrada,
        string? busqueda,
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
                    cancellationToken);
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
                    cancellationToken);
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

            ModelState.AddModelError(string.Empty, error);
        }

        return View(
            nameof(Index),
            await PrepararPantallaAsync(
                presupuesto,
                usuario,
                entrada,
                busqueda,
                cancellationToken));
    }

    // US-003 (RF-05): guardado explícito de los cambios pendientes del borrador.
    // Solo esta acción persiste definitivamente los conceptos y el margen en SQL Server.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> GuardarBorrador(
        int id, bool esperabaCambiosPendientes, CancellationToken cancellationToken)
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

        try
        {
            ResultadoAccionGuardarBorrador resultado = await _guardarBorrador.EjecutarAsync(
                id, esperabaCambiosPendientes, usuario, cancellationToken);

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

    // US-007: pantalla para buscar y elegir a mano el material de una fila de tipo Material.
    // Buscar solo lee el catálogo; no crea ni modifica nada (RF-12).
    [HttpGet]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> SeleccionarMaterial(
        int id, int conceptoId, string? busqueda, CancellationToken cancellationToken)
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

        FilaConceptoDto? fila = await ObtenerFilaActualAsync(id, conceptoId, usuario.Id, cancellationToken);
        if (fila == null)
        {
            return NotFound();
        }

        if (fila.TipoId != TipoConcepto.MaterialId)
        {
            TempData["MensajeError"] = "Solo se puede seleccionar un material en las filas de tipo Material.";
            return RedirectToAction(nameof(Index), new { id });
        }

        return View(await PrepararSeleccionAsync(presupuesto, usuario.Id, fila, busqueda, cancellationToken));
    }

    // US-007 + RF-05: aplica al borrador pendiente la referencia que el usuario eligió para la fila.
    // Sirve para vincular una fila sin material y para sustituir el material de una fila que ya lo tenía (RF-13).
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> AsignarMaterial(
        int id, int conceptoId, int? referenciaId, string? busqueda, CancellationToken cancellationToken)
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

        string? error = await _seleccionarMaterial.EjecutarAsync(
            id, conceptoId, referenciaId, usuario, cancellationToken);
        if (error == null)
        {
            TempData["MensajeExito"] =
                "El material se asoció a la fila en el borrador. Presioná Guardar borrador para confirmar los cambios.";
            return RedirectToAction(nameof(Index), new { id });
        }

        FilaConceptoDto? fila = await ObtenerFilaActualAsync(id, conceptoId, usuario.Id, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, usuario.Id, fila, busqueda, cancellationToken));
    }

    // S3-T12 (US-010A, RF-20) + RF-05: el usuario eligió otra referencia de proveedor del material
    // que la fila ya tiene, desde el panel de referencias de S3-T11. El cambio queda pendiente en el
    // borrador hasta que el usuario presione «Guardar borrador».
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> CambiarProveedor(
        int id, int conceptoId, int? referenciaId, CancellationToken cancellationToken)
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

        string? error = await _cambiarProveedor.EjecutarAsync(
            id, conceptoId, referenciaId, usuario, cancellationToken);
        if (error == null)
        {
            TempData["MensajeExito"] =
                "El proveedor de la fila se actualizó en el borrador. Presioná Guardar borrador para confirmar los cambios.";
            return RedirectToAction(nameof(Index), new { id });
        }

        FilaConceptoDto? fila = await ObtenerFilaActualAsync(id, conceptoId, usuario.Id, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, usuario.Id, fila, null, cancellationToken));
    }

    // S3-T12 (RF-020) + RF-05: el usuario pidió, y confirmó en el diálogo, que la fila vuelva a copiar
    // los valores vigentes de la referencia que ya tiene. El cambio queda pendiente en el borrador
    // hasta que el usuario presione «Guardar borrador».
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> ActualizarReferencia(
        int id, int conceptoId, bool confirmar, CancellationToken cancellationToken)
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

        string? error = await _actualizarReferencia.EjecutarAsync(
            id, conceptoId, confirmar, usuario, cancellationToken);
        if (error == null)
        {
            TempData["MensajeExito"] =
                "Los valores de la fila se actualizaron en el borrador. Presioná Guardar borrador para confirmar los cambios.";
            return RedirectToAction(nameof(Index), new { id });
        }

        FilaConceptoDto? fila = await ObtenerFilaActualAsync(id, conceptoId, usuario.Id, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, usuario.Id, fila, null, cancellationToken));
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

    // Si el usuario tiene un borrador pendiente para este presupuesto, buscamos la fila allí
    // (soporta tanto filas persistidas con Id positivo como filas nuevas con Id negativo).
    // Si no hay borrador pendiente, consultamos directamente lo guardado en SQL Server.
    private async Task<FilaConceptoDto?> ObtenerFilaActualAsync(
        int presupuestoId, int conceptoId, int usuarioId, CancellationToken cancellationToken)
    {
        BorradorPresupuestoDto? borrador = _almacenBorrador.Obtener(presupuestoId, usuarioId);
        if (borrador != null)
        {
            foreach (ConceptoBorradorDto concepto in borrador.Conceptos)
            {
                if (concepto.Id == conceptoId)
                {
                    return concepto.AFilaDto();
                }
            }

            return null;
        }

        return await _consultarConceptos.ObtenerFilaAsync(presupuestoId, conceptoId, cancellationToken);
    }

    private async Task<SeleccionarMaterialViewModel> PrepararSeleccionAsync(
        Presupuesto presupuesto, int usuarioId, FilaConceptoDto fila, string? busqueda,
        CancellationToken cancellationToken)
    {
        // Sin texto de búsqueda no se lista nada: el usuario tiene que buscar a propósito y elegir
        // expresamente. Nada se preselecciona a partir del Concepto detectado (RF-12).
        var materiales = new List<MaterialListadoDto>();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            materiales = await _consultarMateriales.EjecutarAsync(busqueda, null, null, cancellationToken);
        }

        // S3-T11 (US-010A): si la fila ya tiene material, se traen sus referencias de precio para
        // mostrarlas sin que el usuario tenga que buscarlo de nuevo. Solo se lee el catálogo.
        MaterialListadoDto? materialActual = null;
        if (fila.MaterialId != null)
        {
            materialActual = await _consultarMateriales.ObtenerConReferenciasAsync(
                fila.MaterialId.Value, cancellationToken);
        }

        bool tieneCambiosPendientes = _almacenBorrador.TieneCambiosPendientes(presupuesto.Id, usuarioId);

        return new SeleccionarMaterialViewModel
        {
            PresupuestoId = presupuesto.Id,
            Codigo = presupuesto.Codigo,
            TieneCambiosPendientes = tieneCambiosPendientes,
            Fila = fila,
            MaterialActual = materialActual,
            Busqueda = busqueda,
            Materiales = materiales
        };
    }

    private async Task<ConceptosPresupuestoViewModel> PrepararPantallaAsync(
        Presupuesto presupuesto, Usuario usuario, ConceptoManualViewModel entrada, string? busqueda,
        CancellationToken cancellationToken)
    {
        bool puedeAgregar = PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario);

        List<FilaConceptoDto> filas;
        bool tieneCambiosPendientes = false;

        // Solo mostramos cambios pendientes cuando el usuario sigue autorizado a editar el
        // presupuesto. En modo solo lectura siempre mostramos los datos efectivamente guardados.
        BorradorPresupuestoDto? borrador = null;
        if (puedeAgregar)
        {
            borrador = _almacenBorrador.Obtener(presupuesto.Id, usuario.Id);
        }

        if (borrador != null)
        {
            filas = new List<FilaConceptoDto>();
            foreach (ConceptoBorradorDto concepto in borrador.Conceptos)
            {
                filas.Add(concepto.AFilaDto());
            }

            tieneCambiosPendientes = borrador.TieneCambiosPendientes;
        }
        else
        {
            filas = await _consultarConceptos.ObtenerFilasAsync(
                presupuesto.Id, cancellationToken);
        }

        // Agrupamos los subtotales por tipo de concepto.
        // Los importes en USD y UYU se mantienen separados.
        var tiposBaseMargen = AgrupadorSubtotalesPorTipo.Agrupar(filas);

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
            PuedeAgregar = puedeAgregar,
            TieneCambiosPendientes = tieneCambiosPendientes,
            Filas = filas,
            TiposBaseMargen = tiposBaseMargen,
            Tipos = tipos,
            Entrada = entrada,
            Busqueda = busqueda
        };

        // Si el usuario está buscando un material, cargamos sus referencias
        // de proveedores para que pueda elegir una de forma explícita.
        if (puedeAgregar && entrada.TipoId == TipoConcepto.MaterialId && !string.IsNullOrWhiteSpace(busqueda))
        {
            var materiales = await _consultarMateriales.EjecutarAsync(busqueda, null, null, cancellationToken);
            var cultura = CultureInfo.GetCultureInfo("es-UY");

            foreach (var material in materiales)
            {
                var grupo = new SelectListGroup { Name = material.Nombre };

                foreach (var referencia in material.Referencias)
                {
                    string precio = referencia.PrecioUnitario.ToString("0.##########", cultura);

                    DateTime actualizacion = ZonaHorariaUruguay.ConvertirUtcALocal(
                        referencia.FechaUltimaActualizacion);

                    pantalla.Referencias.Add(new SelectListItem
                    {
                        Group = grupo,
                        Value = referencia.RelacionId.ToString(CultureInfo.InvariantCulture),
                        Text = $"{referencia.ProveedorNombre} · {referencia.Moneda} {precio} · {actualizacion:dd/MM/yyyy HH:mm}"
                    });
                }
            }
        }

        return pantalla;
    }
}
