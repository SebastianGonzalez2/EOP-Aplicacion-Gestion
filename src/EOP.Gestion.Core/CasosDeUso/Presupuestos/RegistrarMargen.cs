using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// S3-T07 (RF-27) + S3-T14 (RF-05): incorpora el concepto Margen y los tipos elegidos como su base
// de cálculo al borrador pendiente del presupuesto. No guarda en SQL Server hasta que el usuario
// presiona explícitamente «Guardar borrador».
public class RegistrarMargen
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;

    public RegistrarMargen(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
    }

    public async Task<string?> EjecutarAsync(
        int presupuestoId,
        decimal? porcentaje,
        IReadOnlyCollection<int>? tiposBase,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        // Validamos el porcentaje y los tipos seleccionados.
        string? error = ValidadorBaseMargen.ObtenerError(porcentaje, tiposBase);

        if (error != null)
        {
            return error;
        }

        // Comprobamos permisos y estado editable en el servidor.
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

        // RF-27: un presupuesto no puede tener más de un concepto Margen.
        bool yaTieneMargen = false;
        var tiposDisponibles = new HashSet<int>();

        foreach (ConceptoBorradorDto concepto in borrador.Conceptos)
        {
            if (concepto.TipoConceptoId == TipoConcepto.MargenId)
            {
                yaTieneMargen = true;
            }
            else
            {
                tiposDisponibles.Add(concepto.TipoConceptoId);
            }
        }

        if (yaTieneMargen)
        {
            return "Este presupuesto ya tiene un margen registrado.";
        }

        // Comprobamos que todos los tipos seleccionados estén realmente presentes en el borrador.
        foreach (int tipoSeleccionado in tiposBase!)
        {
            if (!tiposDisponibles.Contains(tipoSeleccionado))
            {
                return "Seleccioná tipos de concepto que estén presentes en el presupuesto.";
            }
        }

        string? nombreTipoMargen = await _repositorio.ObtenerNombreTipoConceptoAsync(
            TipoConcepto.MargenId, cancellationToken);

        if (string.IsNullOrWhiteSpace(nombreTipoMargen))
        {
            nombreTipoMargen = "Margen";
        }

        var nuevoMargen = new ConceptoBorradorDto
        {
            Id = borrador.ObtenerSiguienteIdTemporal(),
            TipoConceptoId = TipoConcepto.MargenId,
            TipoNombre = nombreTipoMargen,
            PorcentajeMargen = porcentaje!.Value,
            TiposBaseMargen = new List<int>(tiposBase),
            EsNuevo = true
        };

        borrador.Conceptos.Add(nuevoMargen);
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
