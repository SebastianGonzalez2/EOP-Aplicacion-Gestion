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
    SinCambios,

    // Los dos valores que siguen solo los devuelve el cambio de proveedor de S3-T12 (US-010A):
    // ahí el material de la fila se conserva y solo se puede elegir otra referencia del mismo.

    // La fila todavía no tiene material elegido, así que no hay "otro proveedor" para cambiar.
    ConceptoSinMaterial,

    // La referencia elegida existe, pero es de un material distinto al que tiene la fila.
    ReferenciaDeOtroMaterial
}
