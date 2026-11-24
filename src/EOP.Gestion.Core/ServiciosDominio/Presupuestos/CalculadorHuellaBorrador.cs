using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EOP.Gestion.Core.Entidades.Presupuestos;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

// Calcula una huella determinística (SHA-256) del estado guardado en la base para los conceptos
// y la base del margen de un presupuesto.
//
// ¿Para qué sirve? Cuando el usuario empieza a hacer cambios en un borrador, guardamos la huella
// de lo que había en SQL Server en ese momento. Al presionar «Guardar borrador», volvemos a
// calcular la huella dentro de la transacción: si no coincide, significa que otra operación ya
// modificó el presupuesto y rechazamos el guardado en vez de pisar cambios en silencio.
public static class CalculadorHuellaBorrador
{
    public static string Calcular(
        IReadOnlyCollection<ConceptoPresupuesto> conceptos,
        IReadOnlyCollection<MargenBaseTipo> tiposBaseMargen)
    {
        var constructor = new StringBuilder();

        List<ConceptoPresupuesto> conceptosOrdenados = conceptos
            .OrderBy(concepto => concepto.Id)
            .ToList();

        foreach (ConceptoPresupuesto concepto in conceptosOrdenados)
        {
            constructor.Append("C|");
            constructor.Append(concepto.Id.ToString(CultureInfo.InvariantCulture));
            constructor.Append('|');
            constructor.Append(concepto.TipoConceptoId.ToString(CultureInfo.InvariantCulture));
            constructor.Append('|');
            constructor.Append(FormatearTexto(concepto.ConceptoDetectado));
            constructor.Append('|');
            constructor.Append(FormatearTexto(concepto.Concepto));
            constructor.Append('|');
            constructor.Append(FormatearEntero(concepto.MaterialId));
            constructor.Append('|');
            constructor.Append(FormatearEntero(concepto.MaterialProveedorId));
            constructor.Append('|');
            constructor.Append(FormatearDecimal(concepto.Cantidad));
            constructor.Append('|');
            constructor.Append(FormatearEntero(concepto.Unidad.HasValue ? (int)concepto.Unidad.Value : null));
            constructor.Append('|');
            constructor.Append(FormatearDecimal(concepto.PrecioUnitario));
            constructor.Append('|');
            constructor.Append(FormatearEntero(concepto.Moneda.HasValue ? (int)concepto.Moneda.Value : null));
            constructor.Append('|');
            constructor.Append(FormatearDecimal(concepto.PorcentajeMargen));
            constructor.Append('\n');
        }

        List<MargenBaseTipo> basesOrdenadas = tiposBaseMargen
            .OrderBy(baseMargen => baseMargen.ConceptoPresupuestoId)
            .ThenBy(baseMargen => baseMargen.TipoConceptoId)
            .ToList();

        foreach (MargenBaseTipo baseMargen in basesOrdenadas)
        {
            constructor.Append("M|");
            constructor.Append(baseMargen.ConceptoPresupuestoId.ToString(CultureInfo.InvariantCulture));
            constructor.Append('|');
            constructor.Append(baseMargen.TipoConceptoId.ToString(CultureInfo.InvariantCulture));
            constructor.Append('\n');
        }

        byte[] bytes = Encoding.UTF8.GetBytes(constructor.ToString());
        byte[] hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    private static string FormatearTexto(string? texto)
    {
        if (texto == null)
        {
            return "<null>";
        }

        // Guardamos el largo antes del texto para evitar ambigüedades si incluye separadores.
        return texto.Length.ToString(CultureInfo.InvariantCulture) + ":" + texto;
    }

    private static string FormatearEntero(int? valor)
    {
        if (!valor.HasValue)
        {
            return "<null>";
        }

        return valor.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatearDecimal(decimal? valor)
    {
        if (!valor.HasValue)
        {
            return "<null>";
        }

        // SQL Server devuelve decimal(28,10) con ceros finales; formatear siempre con 10 decimales
        // garantiza la misma representación sin importar cómo llegó el número en memoria.
        return valor.Value.ToString("0.0000000000", CultureInfo.InvariantCulture);
    }
}
