using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;

namespace EOP.Gestion.Core.DTOs.Presupuestos;

// Representa una referencia de material y proveedor leída del catálogo solo para consultar y copiar
// sus cuatro valores históricos al borrador del presupuesto (DT-11, RF-20).
public record ReferenciaCatalogoPresupuestoDto(
    int MaterialId,
    int MaterialProveedorId,
    string NombreMaterial,
    UnidadMedida Unidad,
    string ProveedorNombre,
    decimal PrecioUnitario,
    Moneda Moneda);
