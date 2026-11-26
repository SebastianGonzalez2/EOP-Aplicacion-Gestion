using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Cotizaciones;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Interfaces.Cotizaciones;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

public class CalcularMargenPresupuesto
{
    private readonly IServicioCotizaciones _servicioCotizaciones;

    public CalcularMargenPresupuesto(IServicioCotizaciones servicioCotizaciones)
    {
        _servicioCotizaciones = servicioCotizaciones;
    }

    public async Task<(string? MensajeError, CotizacionUsd? Cotizacion)>
        EjecutarAsync(List<FilaConceptoDto> filas,CancellationToken cancellationToken = default)
    {
        int indiceMargen = filas.FindIndex(
            fila => fila.TipoId == TipoConcepto.MargenId);

        // Si el presupuesto no tiene margen,
        // no tenemos nada que calcular.
        if (indiceMargen == -1)
        {
            return (null, null);
        }

        FilaConceptoDto margen = filas[indiceMargen];

        if (!margen.PorcentajeMargen.HasValue)
        {
            return ("El margen no tiene un porcentaje definido.", null);
        }

        // Agrupamos los subtotales utilizando los valores
        // actuales de los conceptos del presupuesto.
        var subtotales = AgrupadorSubtotalesPorTipo.Agrupar(filas);

        bool necesitaCotizacion = false;

        foreach (TipoBaseMargenDto tipo in subtotales)
        {
            if (margen.TiposBaseMargen.Contains(tipo.TipoId)
                && tipo.SubtotalUSD != 0)
            {
                necesitaCotizacion = true;
                break;
            }
        }

        decimal? valorCotizacion = null;
        CotizacionUsd? cotizacionUtilizada = null;

        // Solo necesitamos consultar la cotización si existen
        // importes en dólares dentro de la base seleccionada.
        if (necesitaCotizacion)
        {
            try
            {
                CotizacionUsd? cotizacion =await _servicioCotizaciones.ObtenerCotizacionVigenteAsync(cancellationToken);

                if (cotizacion == null || cotizacion.Valor <= 0)
                {
                    filas[indiceMargen] = margen with
                    {
                        SubtotalMargen = null
                    };

                    return (
                        "No hay una cotización válida para calcular el margen.",
                        null);
                }

                valorCotizacion = cotizacion.Valor;

                // Conservamos también la cotización completa
                // para mostrar su valor y fecha de publicación.
                cotizacionUtilizada = cotizacion;
            }
            catch (CotizacionNoDisponibleException excepcion)
            {
                filas[indiceMargen] = margen with
                {
                    SubtotalMargen = null
                };

                return (excepcion.Message, null);
            }
        }

        decimal? subtotalMargen = CalculadorMargen.Calcular(subtotales,margen.TiposBaseMargen,margen.PorcentajeMargen.Value,valorCotizacion);

        // Actualizamos únicamente la fila que representa el margen.
        // El resultado siempre se expresa en pesos uruguayos.
        filas[indiceMargen] = margen with
        {
            SubtotalMargen = subtotalMargen,
            Moneda = Moneda.UYU
        };

        return (null, cotizacionUtilizada);
    }
}