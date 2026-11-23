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

    private readonly RegistrarMargen _registrarMargen;
    public ConceptosPresupuestoController(
        IRepositorioUsuarios usuarios, IRepositorioConceptosPresupuesto repositorio,
        ConsultarConceptosPresupuesto consultarConceptos, ConsultarMateriales consultarMateriales,
        AgregarConceptoManual agregarConcepto, SeleccionarMaterialDeConcepto seleccionarMaterial,
        CambiarProveedorDeConcepto cambiarProveedor, RegistrarMargen registrarMargen)
    {
        _usuarios = usuarios;
        _repositorio = repositorio;
        _consultarConceptos = consultarConceptos;
        _consultarMateriales = consultarMateriales;
        _agregarConcepto = agregarConcepto;
        _seleccionarMaterial = seleccionarMaterial;
        _cambiarProveedor = cambiarProveedor;
        _registrarMargen = registrarMargen;
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
                // RF-27: guardamos el margen y los tipos seleccionados.
                // El caso de uso valida los datos y el repositorio
                // comprueba permisos, duplicados y tipos disponibles.
                error = await _registrarMargen.EjecutarAsync(
                    id,
                    porcentajeMargen,
                    entrada.TiposBaseSeleccionados,
                    usuario,
                    cancellationToken);
            }
            else
            {
                // Los demás tipos conservan el funcionamiento anterior.
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
                TempData["MensajeExito"] =
                    entrada.TipoId == TipoConcepto.MargenId
                        ? "El margen se agregó al presupuesto."
                        : "El concepto se agregó al presupuesto.";

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

        // Obtenemos las filas del presupuesto una sola vez.
        var filas = await _consultarConceptos.ObtenerFilasAsync(
            presupuesto.Id, cancellationToken);

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
