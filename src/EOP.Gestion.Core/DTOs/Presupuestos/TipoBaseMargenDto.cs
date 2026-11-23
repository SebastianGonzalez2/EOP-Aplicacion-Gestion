
namespace EOP.Gestion.Core.DTOs.Presupuestos;

public class TipoBaseMargenDto
{
    public int TipoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal SubtotalUYU { get; set; }

    public decimal SubtotalUSD { get; set; }
}
