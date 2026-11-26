using EOP.Gestion.Core.DTOs.Materiales;

namespace EOP.Gestion.WebApp.ViewModels.Presupuestos;

// UI-presupuestos: datos que necesita el selector de materiales del presupuesto. El selector se
// usa en dos lugares (el modal «Agregar concepto» y la pantalla «Seleccionar material» de una
// fila) y en los dos se ve igual. Funciona en dos pasos:
//   1. Elegir un material del catálogo (listado paginado con búsqueda por nombre).
//   2. Elegir expresamente una de sus referencias de proveedor.

// Configuración del selector para la pantalla donde se incluye.
public class SelectorMaterialViewModel
{
    public int PresupuestoId { get; init; }

    // Nombre del campo oculto que guarda el material elegido. No decide nada en el servidor:
    // solo sirve para volver a mostrar el mismo material si el envío vuelve con errores.
    public string NombreCampoMaterial { get; init; } = string.Empty;

    // True en la pantalla «Seleccionar material» de una fila (el campo enviado es referenciaId).
    public bool ParaFila { get; init; }

    // True si el selector espera a que lo activen (por ejemplo, el modal lo activa recién cuando
    // el usuario elige el tipo Material). Así no se consulta el catálogo si no hace falta.
    public bool CargaDiferida { get; init; }

    // Lo que ya estaba elegido cuando el servidor devolvió la pantalla con errores.
    public int? MaterialElegidoId { get; init; }
    public int? ReferenciaElegidaId { get; init; }

    // Referencia que la fila ya tiene (solo en «Seleccionar material»); no se puede volver a elegir.
    public int? ReferenciaActualId { get; init; }
}

// Paso 1: una página del catálogo.
public class ListadoMaterialesSelectorViewModel
{
    public string? Busqueda { get; init; }
    public IReadOnlyList<MaterialListadoDto> Materiales { get; init; } = [];
    public int PaginaActual { get; init; }
    public int CantidadPaginas { get; init; }
    public int CantidadTotal { get; init; }
    public int MaterialesPorPagina { get; init; }

    public bool SeBusco
    {
        get { return !string.IsNullOrWhiteSpace(Busqueda); }
    }

    // Número del primer y del último material que se muestran, para el texto
    // «Mostrando 1 a 10 de 24 materiales» (docs/diseno-visual/diseno-visual.md, sección 13.3).
    public int PrimeroMostrado
    {
        get
        {
            if (CantidadTotal == 0)
            {
                return 0;
            }

            return ((PaginaActual - 1) * MaterialesPorPagina) + 1;
        }
    }

    public int UltimoMostrado
    {
        get { return PrimeroMostrado + Materiales.Count - 1; }
    }
}

// Paso 2: las referencias de proveedor del material elegido.
public class ReferenciasMaterialSelectorViewModel
{
    public required MaterialListadoDto Material { get; init; }
    public string NombreCampoReferencia { get; init; } = string.Empty;
    public int? ReferenciaSeleccionadaId { get; init; }
    public int? ReferenciaActualId { get; init; }
}
