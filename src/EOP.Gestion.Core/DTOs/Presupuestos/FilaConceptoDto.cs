using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.DTOs.Presupuestos;

public record FilaConceptoDto(
    int Id,
    int TipoId,
    string Tipo,
    string? ConceptoDetectado,
    string? Concepto,
    int? MaterialId,
    int? MaterialProveedorId,
    string? Proveedor,
    decimal? Cantidad,
    UnidadMedida? Unidad,
    decimal? PrecioUnitario,
    Moneda? Moneda)
{
    public decimal? PorcentajeMargen { get; init; }

    public IReadOnlyCollection<int> TiposBaseMargen { get; init; }
        = Array.Empty<int>();

    public decimal? SubtotalMargen { get; init; }

    // El subtotal se calcula con los valores históricos de la fila.
    // No necesitamos consultar nuevamente el catálogo.
    public decimal? Subtotal
    {
        get
        {
            // El margen tiene su propia regla de cálculo.
            // Su importe depende de los otros conceptos seleccionados.
            if (TipoId == TipoConcepto.MargenId)
            {
                return SubtotalMargen;
            }

            return CalculadorSubtotales.Calcular(
                TipoId, Cantidad, PrecioUnitario);
        }
    }
}