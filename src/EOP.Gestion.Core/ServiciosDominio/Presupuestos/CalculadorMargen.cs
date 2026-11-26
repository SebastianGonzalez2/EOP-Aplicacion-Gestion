using EOP.Gestion.Core.DTOs.Presupuestos;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

public static class CalculadorMargen
{
    public static decimal? Calcular(IReadOnlyCollection<TipoBaseMargenDto> subtotalesPorTipo,IReadOnlyCollection<int> tiposSeleccionados,decimal porcentaje,decimal? cotizacion)
    {
        decimal subtotalUYU = 0;
        decimal subtotalUSD = 0;

        // Recorremos únicamente los tipos de concepto
        // que el usuario seleccionó como base del margen.
        foreach (TipoBaseMargenDto tipo in subtotalesPorTipo)
        {
            if (!tiposSeleccionados.Contains(tipo.TipoId))
            {
                continue;
            }

            subtotalUYU += tipo.SubtotalUYU;
            subtotalUSD += tipo.SubtotalUSD;
        }

        decimal baseEnUYU = subtotalUYU;

        // Si tenemos importes en dólares, necesitamos
        // una cotización válida para convertirlos a pesos.
        if (subtotalUSD != 0)
        {
            if (!cotizacion.HasValue || cotizacion.Value <= 0)
            {
                return null;
            }

            baseEnUYU += subtotalUSD * cotizacion.Value;
        }

        // Aplicamos el porcentaje sobre la base en pesos.
        // No redondeamos para conservar la precisión.
        return baseEnUYU * porcentaje / 100m;
    }
}