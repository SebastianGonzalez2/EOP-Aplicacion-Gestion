using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// Agrega un concepto manual (Material o importe directo) al borrador pendiente del presupuesto.
// Según RF-05 (US-003), esta acción NO persiste en SQL Server: los cambios quedan pendientes
// hasta que el usuario presiona explícitamente «Guardar borrador».
public class AgregarConceptoManual
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;

    public AgregarConceptoManual(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
    }

    public async Task<string?> EjecutarAsync(
        int presupuestoId, int tipoId, string? descripcion, decimal? importe, Moneda? moneda,
        int? referenciaId, decimal? cantidad, Usuario usuario, CancellationToken cancellationToken)
    {
        string? descripcionLimpia = null;

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

            if (!ValidadorBorradorPresupuesto.AdmitePrecisionDeGuardado(cantidad.Value))
            {
                return "Ingresá una cantidad con hasta 18 dígitos enteros y 10 decimales.";
            }
        }
        else if (TipoConcepto.AdmiteImporteDirecto(tipoId))
        {
            if (descripcion != null)
            {
                descripcionLimpia = descripcion.Trim();
            }

            if (descripcionLimpia != null && descripcionLimpia.Length > ValidadorBorradorPresupuesto.LargoMaximoDescripcion)
            {
                return "La descripción admite hasta 200 caracteres.";
            }

            if (string.IsNullOrEmpty(descripcionLimpia))
            {
                descripcionLimpia = null;
            }

            if (!importe.HasValue)
            {
                return "Ingresá el importe del concepto.";
            }

            if (!ValidadorBorradorPresupuesto.AdmitePrecisionDeGuardado(importe.Value))
            {
                return "Ingresá un importe con hasta 18 dígitos enteros y 10 decimales.";
            }

            if (moneda is not (Moneda.UYU or Moneda.USD))
            {
                return "Seleccioná USD o UYU.";
            }
        }
        else
        {
            return "Seleccioná un tipo de concepto disponible.";
        }

        // Verificamos permisos y estado editable en el servidor antes de modificar el borrador.
        bool puedeEditar = await _repositorio.PuedeEditarEnBaseAsync(
            presupuestoId, usuario, cancellationToken);

        if (!puedeEditar)
        {
            return "El presupuesto ya no está disponible para edición.";
        }

        BorradorPresupuestoDto? borrador = await ObtenerOInicializarBorradorAsync(
            presupuestoId, usuario.Id, cancellationToken);

        if (borrador == null)
        {
            return "El presupuesto ya no está disponible para edición.";
        }

        string? nombreTipo = await _repositorio.ObtenerNombreTipoConceptoAsync(tipoId, cancellationToken);
        if (string.IsNullOrWhiteSpace(nombreTipo))
        {
            return "Seleccioná un tipo de concepto disponible.";
        }

        var nuevoConcepto = new ConceptoBorradorDto
        {
            Id = borrador.ObtenerSiguienteIdTemporal(),
            TipoConceptoId = tipoId,
            TipoNombre = nombreTipo,
            EsNuevo = true
        };

        if (tipoId == TipoConcepto.MaterialId)
        {
            // Leemos la referencia del catálogo sin seguimiento (solo lectura).
            ReferenciaCatalogoPresupuestoDto? referencia = await _repositorio.ObtenerReferenciaCatalogoAsync(
                referenciaId!.Value, cancellationToken);

            if (referencia == null)
            {
                return "La referencia seleccionada no existe. Buscá otra referencia del catálogo.";
            }

            // RF-20: una misma combinación material-proveedor no puede repetirse en el presupuesto.
            bool combinacionRepetida = false;
            foreach (ConceptoBorradorDto filaExistente in borrador.Conceptos)
            {
                if (filaExistente.MaterialProveedorId == referencia.MaterialProveedorId)
                {
                    combinacionRepetida = true;
                    break;
                }
            }

            if (combinacionRepetida)
            {
                return "Ese material y proveedor ya están en el presupuesto. Conservá la fila existente o seleccioná otra referencia.";
            }

            // Copiamos los cuatro valores históricos (DT-11) en el servidor, ignorando cualquier
            // texto, precio o moneda que pudiera venir manipulado en el formulario.
            nuevoConcepto.CopiarReferencia(referencia);
            nuevoConcepto.Cantidad = cantidad;
        }
        else
        {
            nuevoConcepto.Concepto = descripcionLimpia;
            nuevoConcepto.PrecioUnitario = importe;
            nuevoConcepto.Moneda = moneda;
        }

        borrador.Conceptos.Add(nuevoConcepto);
        _almacenBorrador.Guardar(presupuestoId, usuario.Id, borrador);

        return null;
    }

    private async Task<BorradorPresupuestoDto?> ObtenerOInicializarBorradorAsync(
        int presupuestoId, int usuarioId, CancellationToken cancellationToken)
    {
        BorradorPresupuestoDto? borradorExistente = _almacenBorrador.Obtener(presupuestoId, usuarioId);
        if (borradorExistente != null)
        {
            return borradorExistente.Clonar();
        }

        return await _repositorio.ObtenerBorradorBaseAsync(presupuestoId, cancellationToken);
    }
}
