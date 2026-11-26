using System.Globalization;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.DTOs.Cotizaciones;

namespace EOP.Gestion.WebApp.ViewModels.Presupuestos;

public class ConceptosPresupuestoViewModel
{
    public int PresupuestoId { get; init; }
    public string Codigo { get; init; } = string.Empty;

    // UI-presupuestos: información general del presupuesto (cliente, tipo de proyecto,
    // responsable, fechas y estado) que se muestra arriba de la tabla de conceptos.
    public required PresupuestoDetalleDto Informacion { get; init; }

    public bool PuedeAgregar { get; init; }
    public string? MensajeErrorCotizacion { get; init; }
    public CotizacionUsd? CotizacionUtilizada { get; init; }
    public bool TieneCambiosPendientes { get; init; }
    public string? BorradorId { get; init; }
    public int VersionBorrador { get; init; }
    public string? HuellaEstadoBase { get; init; }
    public IReadOnlyList<FilaConceptoDto> Filas { get; init; } = [];
    public IReadOnlyList<TipoBaseMargenDto> TiposBaseMargen { get; init; } = [];
    public IReadOnlyList<TipoConceptoOpcionDto> Tipos { get; init; } = [];
    public ConceptoManualViewModel Entrada { get; init; } = new();
}

public class ConceptoManualViewModel
{
    public int TipoId { get; set; }
    public string? Descripcion { get; set; }
    public string? Importe { get; set; }
    public Moneda? Moneda { get; set; }
    public int? ReferenciaId { get; set; }

    // UI-presupuestos: material elegido en el selector del modal. Solo sirve para volver a
    // mostrar sus referencias si el servidor rechaza el envío; el caso de uso no lo usa, porque
    // el material se deduce siempre de la referencia (ReferenciaId).
    public int? MaterialId { get; set; }

    public string? Cantidad { get; set; }

    // Porcentaje que el usuario quiere aplicar como margen.
    public string? PorcentajeMargen { get; set; }

    // Id de los tipos elegidos para formar la base del margen.
    public List<int> TiposBaseSeleccionados { get; set; } = [];

    public static bool InterpretarDecimal(string? texto, out decimal? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return true;
        }

        // Aceptamos coma o punto decimal, sin separadores de miles para evitar importes ambiguos.
        string numero = texto.Trim().Replace(',', '.');
        string[] partes = numero.Split('.');
        if (partes.Length == 2 && partes[1].TrimEnd('0').Length > ValidadorPrecioUnitario.CantidadMaximaDeDecimales)
        {
            return false;
        }

        if (!decimal.TryParse(numero, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out decimal convertido))
        {
            return false;
        }

        valor = convertido;
        return true;
    }
}
