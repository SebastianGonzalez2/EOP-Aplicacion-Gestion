using System.Globalization;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
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

    public ConceptosPresupuestoController(
        IRepositorioUsuarios usuarios, IRepositorioConceptosPresupuesto repositorio,
        ConsultarConceptosPresupuesto consultarConceptos, ConsultarMateriales consultarMateriales,
        AgregarConceptoManual agregarConcepto)
    {
        _usuarios = usuarios;
        _repositorio = repositorio;
        _consultarConceptos = consultarConceptos;
        _consultarMateriales = consultarMateriales;
        _agregarConcepto = agregarConcepto;
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

    private async Task<Usuario?> ObtenerUsuarioAsync(CancellationToken cancellationToken)
    {
        string? identidad = User.GetObjectId();
        if (string.IsNullOrWhiteSpace(identidad))
        {
            return null;
        }

        return await _usuarios.ObtenerPorIdentidadExternaAsync(identidad, cancellationToken);
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
                        Text = $"{referencia.ProveedorNombre} · {referencia.Moneda} {precio} · {actualizacion:dd/MM/yyyy}"
                    });
                }
            }
        }

        return pantalla;
    }
}
