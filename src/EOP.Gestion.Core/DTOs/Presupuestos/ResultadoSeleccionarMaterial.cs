namespace EOP.Gestion.Core.DTOs.Presupuestos;

// Cómo terminó el intento de asociar (o sustituir) el material de una fila del presupuesto (US-007).
public enum ResultadoSeleccionarMaterial
{
    Seleccionado,
    EdicionNoPermitida,
    ConceptoInexistente,
    ConceptoNoEsMaterial,
    ReferenciaInexistente,
    ReferenciaDuplicada,

    // La fila ya usa esa misma referencia. No se vuelve a copiar nada para no pisar
    // sin querer los valores históricos que la fila ya conserva (DT-11).
    SinCambios
}
