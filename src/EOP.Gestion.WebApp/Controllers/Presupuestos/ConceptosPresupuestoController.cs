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
    private readonly ConsultarConceptosPresupuesto _consultarConceptos;
    private readonly ConsultarMateriales _consultarMateriales;
    private readonly AgregarConceptoManual _agregarConcepto;
    private readonly SeleccionarMaterialDeConcepto _seleccionarMaterial;
    private readonly CambiarProveedorDeConcepto _cambiarProveedor;
    private readonly ActualizarReferenciaDeConcepto _actualizarReferencia;

    public ConceptosPresupuestoController(
        IRepositorioUsuarios usuarios, IRepositorioConceptosPresupuesto repositorio,
        ConsultarConceptosPresupuesto consultarConceptos, ConsultarMateriales consultarMateriales,
        AgregarConceptoManual agregarConcepto, SeleccionarMaterialDeConcepto seleccionarMaterial,
        CambiarProveedorDeConcepto cambiarProveedor, ActualizarReferenciaDeConcepto actualizarReferencia)
    {
        _usuarios = usuarios;
        _repositorio = repositorio;
        _consultarConceptos = consultarConceptos;
        _consultarMateriales = consultarMateriales;
        _agregarConcepto = agregarConcepto;
        _seleccionarMaterial = seleccionarMaterial;
        _cambiarProveedor = cambiarProveedor;
        _actualizarReferencia = actualizarReferencia;
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
    public async Task<IActionResult> Agregar(int id, ConceptoManualViewModel entrada, string? busqueda,
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

        decimal? cantidad = null;
        decimal? importe = null;
        if (entrada.TipoId == TipoConcepto.MaterialId)
        {
            if (!ConceptoManualViewModel.InterpretarDecimal(entrada.Cantidad, out cantidad))
            {
                ModelState.AddModelError("Entrada.Cantidad", "Ingresá una cantidad válida con hasta 10 decimales.");
            }
        }
        else if (TipoConcepto.AdmiteImporteDirecto(entrada.TipoId)
            && !ConceptoManualViewModel.InterpretarDecimal(entrada.Importe, out importe))
        {
            ModelState.AddModelError("Entrada.Importe", "Ingresá un importe válido con hasta 10 decimales.");
        }

        if (ModelState.IsValid)
        {
            string? error = await _agregarConcepto.EjecutarAsync(
                id, entrada.TipoId, entrada.Descripcion, importe, entrada.Moneda,
                entrada.ReferenciaId, cantidad, usuario, cancellationToken);
            if (error == null)
            {
                TempData["MensajeExito"] = "El concepto se agregó al presupuesto.";
                return RedirectToAction(nameof(Index), new { id });
            }

            ModelState.AddModelError(string.Empty, error);
        }

        return View(nameof(Index), await PrepararPantallaAsync(
            presupuesto, usuario, entrada, busqueda, cancellationToken));
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

        FilaConceptoDto? fila = await _consultarConceptos.ObtenerFilaAsync(id, conceptoId, cancellationToken);
        if (fila == null)
        {
            return NotFound();
        }

        if (fila.TipoId != TipoConcepto.MaterialId)
        {
            TempData["MensajeError"] = "Solo se puede seleccionar un material en las filas de tipo Material.";
            return RedirectToAction(nameof(Index), new { id });
        }

        return View(await PrepararSeleccionAsync(presupuesto, fila, busqueda, cancellationToken));
    }

    // US-007: guarda la referencia que el usuario eligió para la fila. Sirve para vincular una
    // fila sin material y para sustituir el material de una fila que ya lo tenía (RF-13).
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
            TempData["MensajeExito"] = "El material se asoció a la fila del presupuesto.";
            return RedirectToAction(nameof(Index), new { id });
        }

        FilaConceptoDto? fila = await _consultarConceptos.ObtenerFilaAsync(id, conceptoId, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, fila, busqueda, cancellationToken));
    }

    // S3-T12 (US-010A, RF-20): el usuario eligió otra referencia de proveedor del material que la
    // fila ya tiene, desde el panel de referencias de S3-T11. El material no cambia; se vuelven a
    // copiar a la fila el precio y la moneda (y el nombre y la unidad actuales del material).
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
        // presupuesto está en un estado editable (RNF-4). El repositorio lo vuelve a comprobar
        // dentro de la transacción.
        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        string? error = await _cambiarProveedor.EjecutarAsync(
            id, conceptoId, referenciaId, usuario, cancellationToken);
        if (error == null)
        {
            TempData["MensajeExito"] = "El proveedor de la fila se actualizó con la referencia seleccionada.";
            return RedirectToAction(nameof(Index), new { id });
        }

        FilaConceptoDto? fila = await _consultarConceptos.ObtenerFilaAsync(id, conceptoId, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, fila, null, cancellationToken));
    }

    // S3-T12 (RF-020): el usuario pidió, y confirmó en el diálogo, que la fila vuelva a copiar los
    // valores vigentes de la referencia que ya tiene. No recibe ninguna referencia por parámetro:
    // el servidor usa la que la fila ya guarda, así esta acción no puede cambiar de proveedor.
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
        // El repositorio lo vuelve a comprobar dentro de la transacción.
        if (!PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario))
        {
            return Forbid();
        }

        string? error = await _actualizarReferencia.EjecutarAsync(
            id, conceptoId, confirmar, usuario, cancellationToken);
        if (error == null)
        {
            TempData["MensajeExito"] = "Los valores de la fila se actualizaron con los vigentes del catálogo.";
            return RedirectToAction(nameof(Index), new { id });
        }

        FilaConceptoDto? fila = await _consultarConceptos.ObtenerFilaAsync(id, conceptoId, cancellationToken);
        if (fila == null)
        {
            TempData["MensajeError"] = error;
            return RedirectToAction(nameof(Index), new { id });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(nameof(SeleccionarMaterial), await PrepararSeleccionAsync(
            presupuesto, fila, null, cancellationToken));
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

    private async Task<SeleccionarMaterialViewModel> PrepararSeleccionAsync(
        Presupuesto presupuesto, FilaConceptoDto fila, string? busqueda, CancellationToken cancellationToken)
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

        return new SeleccionarMaterialViewModel
        {
            PresupuestoId = presupuesto.Id,
            Codigo = presupuesto.Codigo,
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
        var tipos = new List<TipoConceptoOpcionDto>();
        if (puedeAgregar)
        {
            var tiposRegistrados = await _consultarConceptos.ObtenerTiposAsync(cancellationToken);
            tipos = tiposRegistrados.Where(tipo =>
                tipo.Id == TipoConcepto.MaterialId || TipoConcepto.AdmiteImporteDirecto(tipo.Id)).ToList();
        }

        var pantalla = new ConceptosPresupuestoViewModel
        {
            PresupuestoId = presupuesto.Id,
            Codigo = presupuesto.Codigo,
            PuedeAgregar = puedeAgregar,
            Filas = await _consultarConceptos.ObtenerFilasAsync(presupuesto.Id, cancellationToken),
            Tipos = tipos,
            Entrada = entrada,
            Busqueda = busqueda
        };

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
                    DateTime actualizacion = ZonaHorariaUruguay.ConvertirUtcALocal(referencia.FechaUltimaActualizacion);
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
