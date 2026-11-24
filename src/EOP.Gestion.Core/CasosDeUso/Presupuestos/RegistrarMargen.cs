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
    private readonly ISincronizadorBorradorPresupuesto _sincronizador;

    public RegistrarMargen(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ISincronizadorBorradorPresupuesto sincronizador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
        _sincronizador = sincronizador;
    }

    public async Task<string?> EjecutarAsync(
        int presupuestoId,
        decimal? porcentaje,
        IReadOnlyCollection<int>? tiposBase,
        Usuario usuario,
        CancellationToken cancellationToken,
        ContextoEdicionBorrador? contextoEdicion = null)
    {
        // Validamos el porcentaje y los tipos seleccionados.
        string? error = ValidadorBaseMargen.ObtenerError(porcentaje, tiposBase);

        if (error != null)
        {
            return error;
        }

        ContextoEdicionBorrador contexto = contextoEdicion ?? ContextoEdicionBorrador.Inicial;

        return await _sincronizador.EjecutarConBloqueoAsync(
            usuario.Id,
            async () =>
            {
                await _almacenBorrador.CargarAsync(cancellationToken);

                // Comprobamos permisos y estado editable en el servidor.
                bool puedeEditar = await _repositorio.PuedeEditarEnBaseAsync(
                    presupuestoId, usuario, cancellationToken);

                if (!puedeEditar)
                {
                    return "El presupuesto ya no está disponible para edición.";
                }

                ResultadoVerificacionBorrador verificacion = await ControladorEstadoBorrador.PrepararParaEdicionAsync(
                    presupuestoId, usuario.Id, contexto, _repositorio, _almacenBorrador, cancellationToken);

                if (verificacion.MensajeError != null || verificacion.BorradorParaEditar == null)
                {
                    return verificacion.MensajeError;
                }

                BorradorPresupuestoDto borrador = verificacion.BorradorParaEditar;

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
                if (!verificacion.EsNuevoBorrador)
                {
                    borrador.IncrementarVersion();
                }

                _almacenBorrador.Guardar(presupuestoId, usuario.Id, borrador);
                await _almacenBorrador.ConfirmarAsync(cancellationToken);

                return null;
            },
            cancellationToken);
    }
}
