using System.Globalization;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EOP.Gestion.WebApp.ViewModels.Presupuestos;

public class ConceptosPresupuestoViewModel
{
    public int PresupuestoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public bool PuedeAgregar { get; init; }
    public bool TieneCambiosPendientes { get; init; }
    public int VersionBorrador { get; init; }
    public string? HuellaEstadoBase { get; init; }
    public IReadOnlyList<FilaConceptoDto> Filas { get; init; } = [];
    public IReadOnlyList<TipoBaseMargenDto> TiposBaseMargen { get; init; } = [];
    public IReadOnlyList<TipoConceptoOpcionDto> Tipos { get; init; } = [];
    public List<SelectListItem> Referencias { get; } = [];
    public string? Busqueda { get; init; }
    public ConceptoManualViewModel Entrada { get; init; } = new();
}

public class ConceptoManualViewModel
{
    public int TipoId { get; set; }
    public string? Descripcion { get; set; }
    public string? Importe { get; set; }
    public Moneda? Moneda { get; set; }
    public int? ReferenciaId { get; set; }
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
