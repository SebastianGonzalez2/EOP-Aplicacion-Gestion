
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

public class RegistrarMargen
{
    private readonly IRepositorioMargenPresupuesto _repositorio;

    public RegistrarMargen(IRepositorioMargenPresupuesto repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<string?> EjecutarAsync(int presupuestoId,decimal? porcentaje,IReadOnlyCollection<int>? tiposBase,Usuario usuario,
        CancellationToken cancellationToken)
    {
        // Validamos el porcentaje y los tipos seleccionados.
        string? error = ValidadorBaseMargen.ObtenerError(porcentaje, tiposBase);

        if (error != null)
        {
            return error;
        }

        // El repositorio volverá a comprobar los permisos,
        // la existencia de los tipos y que no haya otro margen.
        ResultadoRegistrarMargen resultado =
            await _repositorio.RegistrarSiPuedeEditarAsync(
                presupuestoId,
                porcentaje!.Value,
                tiposBase!,
                usuario,
                cancellationToken);

        return resultado switch
        {
            ResultadoRegistrarMargen.Guardado => null,

            ResultadoRegistrarMargen.EdicionNoPermitida =>
                "El presupuesto ya no está disponible para edición.",

            ResultadoRegistrarMargen.MargenExistente =>
                "Este presupuesto ya tiene un margen registrado.",

            ResultadoRegistrarMargen.TiposNoDisponibles =>
                "Seleccioná tipos de concepto que estén presentes en el presupuesto.",

            _ => throw new InvalidOperationException(
                "El repositorio devolvió un resultado desconocido.")
        };
    }
}
