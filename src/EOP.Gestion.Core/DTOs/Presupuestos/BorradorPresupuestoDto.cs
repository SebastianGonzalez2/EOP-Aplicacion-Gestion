using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;

namespace EOP.Gestion.Core.DTOs.Presupuestos;

// Fila en trabajo dentro de los cambios pendientes de un presupuesto antes de guardar (RF-05).
// Las filas que ya están en la base conservan su Id positivo; las nuevas llevan un Id negativo
// temporal (-1, -2, ...) para poder trabajar con ellas durante la elaboración (por ejemplo,
// cambiarles el material o proveedor) antes de persistirlas definitivamente.
public class ConceptoBorradorDto
{
    public int Id { get; set; }

    public int TipoConceptoId { get; set; }

    public string TipoNombre { get; set; } = string.Empty;

    public string? ConceptoDetectado { get; set; }

    public string? Concepto { get; set; }

    public int? MaterialId { get; set; }

    public int? MaterialProveedorId { get; set; }

    public string? ProveedorNombre { get; set; }

    public decimal? Cantidad { get; set; }

    public UnidadMedida? Unidad { get; set; }

    public decimal? PrecioUnitario { get; set; }

    public Moneda? Moneda { get; set; }

    public decimal? PorcentajeMargen { get; set; }

    // Tipos de concepto elegidos como base del margen (solo aplica cuando TipoConceptoId es Margen).
    public List<int> TiposBaseMargen { get; set; } = [];

    // Indica si la fila fue agregada en esta sesión de edición y todavía no existe en SQL Server.
    public bool EsNuevo { get; set; }

    // Indica si una fila que ya existía en SQL Server fue modificada respecto de lo guardado.
    // Así, al guardar el borrador, solo actualizamos las filas modificadas y nunca tocamos los
    // valores históricos de las demás filas (DT-11).
    public bool FueModificado { get; set; }

    public FilaConceptoDto AFilaDto()
    {
        return new FilaConceptoDto(
            Id,
            TipoConceptoId,
            TipoNombre,
            ConceptoDetectado,
            Concepto,
            MaterialId,
            MaterialProveedorId,
            ProveedorNombre,
            Cantidad,
            Unidad,
            PrecioUnitario,
            Moneda);
    }

    // Copia los cuatro valores históricos del catálogo (DT-11) junto con la referencia elegida.
    // No toca ConceptoDetectado ni Cantidad, porque pertenecen a la fila y no al catálogo.
    public void CopiarReferencia(ReferenciaCatalogoPresupuestoDto referencia)
    {
        MaterialId = referencia.MaterialId;
        MaterialProveedorId = referencia.MaterialProveedorId;
        ProveedorNombre = referencia.ProveedorNombre;
        Concepto = referencia.NombreMaterial;
        Unidad = referencia.Unidad;
        PrecioUnitario = referencia.PrecioUnitario;
        Moneda = referencia.Moneda;
    }

    public ConceptoBorradorDto Clonar()
    {
        return new ConceptoBorradorDto
        {
            Id = Id,
            TipoConceptoId = TipoConceptoId,
            TipoNombre = TipoNombre,
            ConceptoDetectado = ConceptoDetectado,
            Concepto = Concepto,
            MaterialId = MaterialId,
            MaterialProveedorId = MaterialProveedorId,
            ProveedorNombre = ProveedorNombre,
            Cantidad = Cantidad,
            Unidad = Unidad,
            PrecioUnitario = PrecioUnitario,
            Moneda = Moneda,
            PorcentajeMargen = PorcentajeMargen,
            TiposBaseMargen = new List<int>(TiposBaseMargen),
            EsNuevo = EsNuevo,
            FueModificado = FueModificado
        };
    }
}

// Estado completo de trabajo de un presupuesto mientras el usuario elabora cambios pendientes
// antes de presionar «Guardar borrador» (US-003, RF-05).
public class BorradorPresupuestoDto
{
    public int PresupuestoId { get; set; }

    // Huella calculada sobre los datos persistidos en SQL Server al iniciar la edición.
    // Permite detectar si otra operación modificó el presupuesto antes de guardar este borrador.
    public string HuellaEstadoBase { get; set; } = string.Empty;

    // Secuencia negativa para identificar conceptos nuevos mientras siguen pendientes de guardado.
    public int SiguienteIdTemporal { get; set; } = -1;

    public List<ConceptoBorradorDto> Conceptos { get; set; } = [];

    // True si existe al menos una fila nueva o modificada pendiente de persistencia definitiva.
    // También deja preparada la detección de cambios sin guardar para la advertencia de S3-T15.
    public bool TieneCambiosPendientes
    {
        get
        {
            foreach (ConceptoBorradorDto concepto in Conceptos)
            {
                if (concepto.EsNuevo || concepto.FueModificado)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public int ObtenerSiguienteIdTemporal()
    {
        int idAsignado = SiguienteIdTemporal;
        SiguienteIdTemporal = SiguienteIdTemporal - 1;
        return idAsignado;
    }

    public BorradorPresupuestoDto Clonar()
    {
        var copia = new BorradorPresupuestoDto
        {
            PresupuestoId = PresupuestoId,
            HuellaEstadoBase = HuellaEstadoBase,
            SiguienteIdTemporal = SiguienteIdTemporal
        };

        foreach (ConceptoBorradorDto concepto in Conceptos)
        {
            copia.Conceptos.Add(concepto.Clonar());
        }

        return copia;
    }
}
