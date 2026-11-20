using EOP.Gestion.Core.DTOs.Materiales;
using EOP.Gestion.Core.DTOs.Presupuestos;

namespace EOP.Gestion.WebApp.ViewModels.Presupuestos;

// Datos de la pantalla donde se busca y se elige el material de una fila del presupuesto (US-007).
public class SeleccionarMaterialViewModel
{
    public int PresupuestoId { get; init; }
    public string Codigo { get; init; } = string.Empty;

    // La fila que se está completando o corrigiendo: sirve para mostrar su Concepto detectado
    // (que no cambia) y el material que tiene hoy.
    public required FilaConceptoDto Fila { get; init; }

    public string? Busqueda { get; init; }

    // Solo se llena cuando el usuario buscó algo. Son materiales del catálogo, solo para leer.
    public IReadOnlyList<MaterialListadoDto> Materiales { get; init; } = [];

    public bool SeBusco
    {
        get { return !string.IsNullOrWhiteSpace(Busqueda); }
    }
}
