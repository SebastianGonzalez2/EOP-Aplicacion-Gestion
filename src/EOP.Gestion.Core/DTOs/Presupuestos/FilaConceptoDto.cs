
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
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
    // El subtotal se calcula con los valores históricos de la fila.
    // No necesitamos consultar nuevamente el catálogo.
    public decimal? Subtotal
    {
        get
        {
            return CalculadorSubtotales.Calcular(TipoId,Cantidad,PrecioUnitario);
        }
    }
}
