using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.CasosDeUso.Presupuestos;

// S3-T15 (US-003, US-042): averigua en qué presupuestos tiene el usuario cambios sin guardar.
//
// Lo necesitamos fuera de la pantalla de elaboración: por ejemplo, si el usuario está en
// Clientes y aprieta "Cerrar sesión", tenemos que saber si dejó algo pendiente en algún
// presupuesto para advertirle antes de cerrar.
//
// Solo lee: no guarda nada en SQL Server ni modifica los borradores.
public class ConsultarCambiosPendientesDelUsuario
{
    private readonly IRepositorioConceptosPresupuesto _repositorio;
    private readonly IAlmacenBorradorPresupuesto _almacenBorrador;
    private readonly ISincronizadorBorradorPresupuesto _sincronizador;

    public ConsultarCambiosPendientesDelUsuario(
        IRepositorioConceptosPresupuesto repositorio,
        IAlmacenBorradorPresupuesto almacenBorrador,
        ISincronizadorBorradorPresupuesto sincronizador)
    {
        _repositorio = repositorio;
        _almacenBorrador = almacenBorrador;
        _sincronizador = sincronizador;
    }

    public async Task<List<CambioPendienteDto>> EjecutarAsync(int usuarioId, CancellationToken cancellationToken)
    {
        // Leemos la sesión dentro del bloqueo para ver el estado más reciente que dejaron
        // confirmado las otras pestañas del mismo usuario.
        List<BorradorPresupuestoDto> borradores = await _sincronizador.EjecutarConBloqueoAsync(
            usuarioId,
            async () =>
            {
                await _almacenBorrador.CargarAsync(cancellationToken);
                return _almacenBorrador.ObtenerPendientesDelUsuario(usuarioId);
            },
            cancellationToken);

        var cambiosPendientes = new List<CambioPendienteDto>();

        foreach (BorradorPresupuestoDto borrador in borradores)
        {
            // El código y el nombre solo sirven para que el usuario reconozca el presupuesto en
            // la advertencia. Si por algún motivo no se encuentra, igual lo listamos: no podemos
            // esconder un cambio pendiente solo porque no tenemos su nombre.
            Presupuesto? presupuesto = await _repositorio.ObtenerPresupuestoAsync(
                borrador.PresupuestoId, cancellationToken);

            string codigo = $"Presupuesto {borrador.PresupuestoId}";
            string nombreProyecto = string.Empty;

            if (presupuesto != null)
            {
                codigo = presupuesto.Codigo;
                nombreProyecto = presupuesto.NombreProyecto;
            }

            cambiosPendientes.Add(new CambioPendienteDto(
                borrador.PresupuestoId,
                codigo,
                nombreProyecto,
                borrador.BorradorId,
                borrador.Version));
        }

        return cambiosPendientes;
    }
}
