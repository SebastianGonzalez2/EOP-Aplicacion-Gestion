using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;

namespace EOP.Gestion.Core.DTOs.Presupuestos;

public record FilaConceptoDto(
    int TipoId, string Tipo, string? ConceptoDetectado, string? Concepto, string? Proveedor,
    decimal? Cantidad, UnidadMedida? Unidad, decimal? PrecioUnitario, Moneda? Moneda);
