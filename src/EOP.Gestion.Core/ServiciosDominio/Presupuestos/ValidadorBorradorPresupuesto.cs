using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Presupuestos;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

// Reglas de validación puras para los conceptos de un presupuesto y para un borrador completo
// antes de persistirlo en la base de datos (RNF-10, RNF-18, RF-16, RF-20, RF-27).
public static class ValidadorBorradorPresupuesto
{
    public const int LargoMaximoDescripcion = 200;

    public static bool AdmitePrecisionDeGuardado(decimal valor)
    {
        return valor >= -ValidadorPrecioUnitario.ValorMaximo
            && valor <= ValidadorPrecioUnitario.ValorMaximo
            && Math.Round(valor, ValidadorPrecioUnitario.CantidadMaximaDeDecimales) == valor;
    }

    // Valida la coherencia completa de un borrador antes de intentar guardarlo en SQL Server.
    public static string? ValidarBorrador(BorradorPresupuestoDto borrador)
    {
        if (borrador.PresupuestoId <= 0)
        {
            return "El presupuesto indicado no es válido.";
        }

        var referenciasUsadas = new HashSet<int>();
        var tiposPresentesSinMargen = new HashSet<int>();
        int cantidadMargenes = 0;

        foreach (ConceptoBorradorDto concepto in borrador.Conceptos)
        {
            if (concepto.TipoConceptoId != TipoConcepto.MargenId)
            {
                tiposPresentesSinMargen.Add(concepto.TipoConceptoId);
            }

            if (concepto.MaterialProveedorId.HasValue)
            {
                if (!referenciasUsadas.Add(concepto.MaterialProveedorId.Value))
                {
                    return "Ese material y proveedor ya están en el presupuesto. Conservá la fila existente o seleccioná otra referencia.";
                }
            }

            if (concepto.TipoConceptoId == TipoConcepto.MargenId)
            {
                cantidadMargenes = cantidadMargenes + 1;

                if (cantidadMargenes > 1)
                {
                    return "Este presupuesto ya tiene un margen registrado.";
                }
            }

            // Solo validamos en profundidad las filas que se van a insertar o actualizar,
            // para no rechazar datos históricos ya guardados en la base.
            if (!concepto.EsNuevo && !concepto.FueModificado)
            {
                continue;
            }

            string? errorFila = ValidarFilaEditable(concepto);
            if (errorFila != null)
            {
                return errorFila;
            }
        }

        // Validamos que los tipos elegidos como base del margen existan efectivamente en el presupuesto.
        foreach (ConceptoBorradorDto concepto in borrador.Conceptos)
        {
            if (concepto.TipoConceptoId != TipoConcepto.MargenId)
            {
                continue;
            }

            if (!concepto.EsNuevo && !concepto.FueModificado)
            {
                continue;
            }

            foreach (int tipoBaseId in concepto.TiposBaseMargen)
            {
                if (!tiposPresentesSinMargen.Contains(tipoBaseId))
                {
                    return "Seleccioná tipos de concepto que estén presentes en el presupuesto.";
                }
            }
        }

        return null;
    }

    private static string? ValidarFilaEditable(ConceptoBorradorDto concepto)
    {
        if (concepto.TipoConceptoId == TipoConcepto.MaterialId)
        {
            return ValidarFilaMaterial(concepto);
        }

        if (TipoConcepto.AdmiteImporteDirecto(concepto.TipoConceptoId))
        {
            return ValidarFilaImporteDirecto(concepto);
        }

        if (concepto.TipoConceptoId == TipoConcepto.MargenId)
        {
            return ValidadorBaseMargen.ObtenerError(concepto.PorcentajeMargen, concepto.TiposBaseMargen);
        }

        return "Seleccioná un tipo de concepto disponible.";
    }

    private static string? ValidarFilaMaterial(ConceptoBorradorDto concepto)
    {
        if (concepto.PorcentajeMargen.HasValue)
        {
            return "Solo el concepto de tipo Margen puede llevar porcentaje.";
        }

        bool tieneMaterial = concepto.MaterialId.HasValue;
        bool tieneReferencia = concepto.MaterialProveedorId.HasValue;

        if (tieneMaterial != tieneReferencia)
        {
            return "La referencia de material y proveedor está incompleta.";
        }

        if (concepto.EsNuevo && (!tieneReferencia || concepto.MaterialProveedorId!.Value <= 0))
        {
            return "Seleccioná una referencia de material y proveedor.";
        }

        if (concepto.EsNuevo && (!concepto.Cantidad.HasValue || concepto.Cantidad.Value <= 0))
        {
            return "Ingresá una cantidad mayor que cero.";
        }

        if (concepto.Cantidad.HasValue)
        {
            if (concepto.Cantidad.Value <= 0)
            {
                return "Ingresá una cantidad mayor que cero.";
            }

            if (!AdmitePrecisionDeGuardado(concepto.Cantidad.Value))
            {
                return "Ingresá una cantidad con hasta 18 dígitos enteros y 10 decimales.";
            }
        }

        if (tieneReferencia)
        {
            if (concepto.MaterialId!.Value <= 0 || concepto.MaterialProveedorId!.Value <= 0)
            {
                return "Seleccioná una referencia de material y proveedor.";
            }

            if (string.IsNullOrWhiteSpace(concepto.Concepto) || concepto.Concepto.Trim().Length > LargoMaximoDescripcion)
            {
                return "El nombre histórico del material no es válido.";
            }

            if (!concepto.Unidad.HasValue || !Enum.IsDefined(concepto.Unidad.Value))
            {
                return "La unidad histórica del material no es válida.";
            }

            if (!concepto.PrecioUnitario.HasValue || concepto.PrecioUnitario.Value <= 0
                || !AdmitePrecisionDeGuardado(concepto.PrecioUnitario.Value))
            {
                return "El precio unitario histórico del material no es válido.";
            }

            if (concepto.Moneda is not (Moneda.UYU or Moneda.USD))
            {
                return "La moneda histórica del material debe ser USD o UYU.";
            }
        }

        return null;
    }

    private static string? ValidarFilaImporteDirecto(ConceptoBorradorDto concepto)
    {
        if (concepto.MaterialId.HasValue || concepto.MaterialProveedorId.HasValue
            || concepto.Cantidad.HasValue || concepto.Unidad.HasValue || concepto.PorcentajeMargen.HasValue)
        {
            return "Los conceptos de importe directo no admiten material, cantidad, unidad ni margen.";
        }

        if (concepto.Concepto != null)
        {
            string descripcionLimpia = concepto.Concepto.Trim();
            if (descripcionLimpia.Length == 0 || descripcionLimpia.Length > LargoMaximoDescripcion)
            {
                return "La descripción admite hasta 200 caracteres.";
            }
        }

        if (!concepto.PrecioUnitario.HasValue)
        {
            return "Ingresá el importe del concepto.";
        }

        if (!AdmitePrecisionDeGuardado(concepto.PrecioUnitario.Value))
        {
            return "Ingresá un importe con hasta 18 dígitos enteros y 10 decimales.";
        }

        if (concepto.Moneda is not (Moneda.UYU or Moneda.USD))
        {
            return "Seleccioná USD o UYU.";
        }

        return null;
    }
}
