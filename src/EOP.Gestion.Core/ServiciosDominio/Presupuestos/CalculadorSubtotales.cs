
using EOP.Gestion.Core.Entidades.Presupuestos;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

public static class CalculadorSubtotales
{
    public static decimal? Calcular(ConceptoPresupuesto concepto)
    {
        return Calcular(concepto.TipoConceptoId,concepto.Cantidad,concepto.PrecioUnitario);
    }

    public static decimal? Calcular(int tipoId,decimal? cantidad,decimal? precioUnitario)
    {
        // Para los materiales, multiplicamos la cantidad por el precio
        // unitario que ya quedó guardado en el presupuesto.
        if (tipoId == TipoConcepto.MaterialId)
        {
            if (!cantidad.HasValue || !precioUnitario.HasValue)
            {
                return null;
            }

            return cantidad.Value * precioUnitario.Value;
        }

        // Para mano de obra, alquiler, traslado y costos indirectos,
        // el importe ingresado ya representa el subtotal.
        if (TipoConcepto.AdmiteImporteDirecto(tipoId))
        {
            return precioUnitario;
        }

        // El margen se calcula en otra tarea, con sus propias reglas.
        // Tampoco inventamos un subtotal para tipos no reconocidos.
        return null;
    }
}
