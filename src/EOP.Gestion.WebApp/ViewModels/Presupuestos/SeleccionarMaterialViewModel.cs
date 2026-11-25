using EOP.Gestion.Core.DTOs.Materiales;
using EOP.Gestion.Core.DTOs.Presupuestos;

namespace EOP.Gestion.WebApp.ViewModels.Presupuestos;

// Datos de la pantalla donde se busca y se elige el material de una fila del presupuesto (US-007).
public class SeleccionarMaterialViewModel
{
    public int PresupuestoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public bool TieneCambiosPendientes { get; init; }
    public string? BorradorId { get; init; }
    public int VersionBorrador { get; init; }
    public string? HuellaEstadoBase { get; init; }

    // La fila que se está completando o corrigiendo: sirve para mostrar su Concepto detectado
    // (que no cambia) y el material que tiene hoy.
    public required FilaConceptoDto Fila { get; init; }

    // S3-T11 (US-010A): si la fila ya tiene material, acá van sus referencias de precio actuales del
    // catálogo para consultarlas sin buscar de nuevo. Es solo lectura. Si la fila todavía no tiene
    // material, queda en null y la pantalla no muestra este panel.
    // S3-T12: de este panel sale también el cambio de proveedor (CambiarProveedor), siempre dentro
    // del mismo material. Los datos del panel siguen siendo solo para mostrar.
    public MaterialListadoDto? MaterialActual { get; init; }

    // S3-T12 (RF-020): la referencia que la fila ya tiene, tal como está HOY en el catálogo. Es null
    // si la fila no tiene material o si esa referencia ya no figura entre las del material.
    public ReferenciaPrecioMaterialDto? ReferenciaActualEnCatalogo
    {
        get
        {
            if (MaterialActual == null || Fila.MaterialProveedorId == null)
            {
                return null;
            }

            return MaterialActual.Referencias.FirstOrDefault(
                referencia => referencia.RelacionId == Fila.MaterialProveedorId);
        }
    }

    // True si alguno de los cuatro valores que la fila copió (nombre, unidad, precio, moneda) ya no
    // coincide con el catálogo. Solo sirve para decidir si la pantalla ofrece actualizar; el servidor
    // vuelve a comparar al guardar. Nunca se actualiza nada por el solo hecho de que difieran.
    public bool ElCatalogoCambioLaReferenciaActual
    {
        get
        {
            ReferenciaPrecioMaterialDto? referenciaActual = ReferenciaActualEnCatalogo;
            if (MaterialActual == null || referenciaActual == null)
            {
                return false;
            }

            bool coinciden = Fila.Concepto == MaterialActual.Nombre
                && Fila.Unidad == MaterialActual.Unidad
                && Fila.PrecioUnitario == referenciaActual.PrecioUnitario
                && Fila.Moneda == referenciaActual.Moneda;
            return !coinciden;
        }
    }

    // UI-presupuestos: la búsqueda y el listado de materiales los resuelve el selector de
    // materiales sin recargar la pantalla. Estos dos datos solo se llenan cuando el servidor
    // rechazó una selección, para que el selector vuelva a mostrar lo que el usuario había elegido.
    public int? MaterialElegidoId { get; init; }
    public int? ReferenciaElegidaId { get; init; }
}
