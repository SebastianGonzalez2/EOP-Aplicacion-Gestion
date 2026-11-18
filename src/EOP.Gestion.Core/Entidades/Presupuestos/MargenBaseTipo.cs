namespace EOP.Gestion.Core.Entidades.Presupuestos
{
    // Tipos de concepto que el usuario eligió como base para calcular el margen
    // (RF-27). Es la tabla intermedia entre un concepto Margen y los tipos que
    // suman su base. La clave es compuesta: un mismo tipo no puede repetirse en
    // la base del mismo margen.
    public class MargenBaseTipo
    {
        public int ConceptoPresupuestoId { get; set; }

        public int TipoConceptoId { get; set; }
    }
}
