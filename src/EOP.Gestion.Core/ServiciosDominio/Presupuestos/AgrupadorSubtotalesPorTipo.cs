
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

public static class AgrupadorSubtotalesPorTipo
{
    public static List<TipoBaseMargenDto> Agrupar(IReadOnlyList<FilaConceptoDto> filas)
    {
        var totalesPorTipo = new Dictionary<int, TipoBaseMargenDto>();

        foreach (FilaConceptoDto fila in filas)
        {
            // El margen nunca puede formar parte de su propia base.
            if (fila.TipoId == TipoConcepto.MargenId)
            {
                continue;
            }

            // Si todavía no tenemos este tipo, lo agregamos.
            if (!totalesPorTipo.TryGetValue(fila.TipoId, out TipoBaseMargenDto? resumen))
            {
                resumen = new TipoBaseMargenDto
                {
                    TipoId = fila.TipoId,
                    Nombre = fila.Tipo
                };

                totalesPorTipo.Add(fila.TipoId, resumen);
            }

            // Un concepto sin subtotal completo no aporta importe.
            if (!fila.Subtotal.HasValue || !fila.Moneda.HasValue)
            {
                continue;
            }

            // Mantenemos separadas las monedas originales.
            // No convertimos USD a UYU en esta etapa.
            if (fila.Moneda.Value == Moneda.UYU)
            {
                resumen.SubtotalUYU += fila.Subtotal.Value;
            }
            else if (fila.Moneda.Value == Moneda.USD)
            {
                resumen.SubtotalUSD += fila.Subtotal.Value;
            }
        }

        return totalesPorTipo.Values.OrderBy(tipo => tipo.TipoId).ToList();
    }
}
