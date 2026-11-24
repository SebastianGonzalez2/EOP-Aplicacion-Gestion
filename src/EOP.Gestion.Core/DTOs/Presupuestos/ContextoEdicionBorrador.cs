namespace EOP.Gestion.Core.DTOs.Presupuestos;

// Información que envía la pantalla al intentar editar o guardar el borrador.
// Permite detectar en el servidor:
// - Si la pantalla esperaba cambios pendientes que ya no están en memoria (pérdida temporal).
// - Si el borrador fue modificado o guardado desde otra pestaña (versión o huella desactualizada).
public record ContextoEdicionBorrador(
    bool EsperabaCambiosPendientes,
    int VersionBorradorEsperada,
    string? HuellaEstadoBaseEsperada = null)
{
    public static ContextoEdicionBorrador Inicial { get; } = new(false, 0, null);
}
