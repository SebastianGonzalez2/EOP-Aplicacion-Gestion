using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

public class AgregarConceptoManual
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;

    public AgregarConceptoManual(IRepositorioConceptosPresupuesto repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<string?> EjecutarAsync(
        int presupuestoId, int tipoId, string? descripcion, decimal? importe, Moneda? moneda,
        int? referenciaId, decimal? cantidad, Usuario usuario, CancellationToken cancellationToken)
    {
        var concepto = new ConceptoPresupuesto { PresupuestoId = presupuestoId, TipoConceptoId = tipoId };
        if (tipoId == TipoConcepto.MaterialId)
        {
            if (referenciaId is null or <= 0)
            {
                return "Seleccioná una referencia de material y proveedor.";
            }

            if (!cantidad.HasValue || cantidad.Value <= 0)
            {
                return "Ingresá una cantidad mayor que cero.";
            }

            if (!AdmitePrecisionDeGuardado(cantidad.Value))
            {
                return "Ingresá una cantidad con hasta 18 dígitos enteros y 10 decimales.";
            }

            concepto.MaterialProveedorId = referenciaId;
            concepto.Cantidad = cantidad;
        }
        else if (TipoConcepto.AdmiteImporteDirecto(tipoId))
        {
            descripcion = descripcion?.Trim();
            if (descripcion?.Length > 200)
            {
                return "La descripción admite hasta 200 caracteres.";
            }

            if (!importe.HasValue)
            {
                return "Ingresá el importe del concepto.";
            }

            if (!AdmitePrecisionDeGuardado(importe.Value))
            {
                return "Ingresá un importe con hasta 18 dígitos enteros y 10 decimales.";
            }

            if (moneda is not (Moneda.UYU or Moneda.USD))
            {
                return "Seleccioná USD o UYU.";
            }

            concepto.Concepto = string.IsNullOrEmpty(descripcion) ? null : descripcion;
            concepto.PrecioUnitario = importe;
            concepto.Moneda = moneda;
        }
        else
        {
            return "Seleccioná un tipo de concepto disponible.";
        }

        ResultadoGuardarConcepto resultado = await _repositorio.AgregarSiPuedeEditarAsync(
            concepto, usuario, cancellationToken);
        switch (resultado)
        {
            case ResultadoGuardarConcepto.Guardado:
                return null;
            case ResultadoGuardarConcepto.EdicionNoPermitida:
                return "El presupuesto ya no está disponible para edición.";
            case ResultadoGuardarConcepto.ReferenciaInexistente:
                return "La referencia seleccionada no existe. Buscá otra referencia del catálogo.";
            case ResultadoGuardarConcepto.ReferenciaDuplicada:
                return "Ese material y proveedor ya están en el presupuesto. Conservá la fila existente o seleccioná otra referencia.";
            default:
                throw new InvalidOperationException("El repositorio devolvió un resultado desconocido.");
        }
    }

    private static bool AdmitePrecisionDeGuardado(decimal valor)
    {
        return valor >= -ValidadorPrecioUnitario.ValorMaximo
            && valor <= ValidadorPrecioUnitario.ValorMaximo
            && Math.Round(valor, ValidadorPrecioUnitario.CantidadMaximaDeDecimales) == valor;
    }
}
