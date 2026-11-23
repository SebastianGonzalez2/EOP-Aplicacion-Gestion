
using EOP.Gestion.Core.Entidades.Presupuestos;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

public static class ValidadorBaseMargen
{
    public static string? ObtenerError(
        decimal? porcentaje,
        IReadOnlyCollection<int>? tiposSeleccionados)
    {
        // RF-27: el usuario debe ingresar un porcentaje de margen.
        if (!porcentaje.HasValue)
        {
            return "Ingresá el porcentaje de margen.";
        }

        // La columna PorcentajeMargen es decimal(9,4).
        // No permitimos valores negativos ni valores que no entren en SQL Server.
        if (porcentaje.Value < 0 || porcentaje.Value > 99999.9999m)
        {
            return "El porcentaje debe estar entre 0 y 99999,9999.";
        }

        if (decimal.Round(porcentaje.Value, 4) != porcentaje.Value)
        {
            return "El porcentaje admite como máximo 4 decimales.";
        }

        // La base debe contener al menos un tipo seleccionado.
        if (tiposSeleccionados == null || tiposSeleccionados.Count == 0)
        {
            return "Seleccioná al menos un tipo de concepto para la base del margen.";
        }

        // Un mismo tipo no puede seleccionarse dos veces.
        if (tiposSeleccionados.Distinct().Count() != tiposSeleccionados.Count)
        {
            return "No se puede repetir un tipo de concepto en la base del margen.";
        }

        // RF-27: el margen no puede formar parte de su propia base.
        if (tiposSeleccionados.Contains(TipoConcepto.MargenId))
        {
            return "El margen no puede utilizarse como base de otro margen.";
        }

        // Solo aceptamos los tipos económicos definidos en el sistema.
        if (tiposSeleccionados.Any(tipoId =>
            tipoId != TipoConcepto.MaterialId &&
            !TipoConcepto.AdmiteImporteDirecto(tipoId)))
        {
            return "Seleccioná únicamente tipos de concepto válidos.";
        }

        return null;
    }
}
