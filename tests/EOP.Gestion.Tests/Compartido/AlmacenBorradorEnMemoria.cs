using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Tests.Compartido;

internal sealed class AlmacenBorradorEnMemoria : IAlmacenBorradorPresupuesto
{
    private readonly Dictionary<(int PresupuestoId, int UsuarioId), BorradorPresupuestoDto> _borradores = [];
    private readonly Dictionary<(int PresupuestoId, int UsuarioId), string> _ultimasHuellasGuardadas = [];
    private readonly Dictionary<(int PresupuestoId, int UsuarioId), string> _ultimosDescartes = [];

    public int CantidadCargas { get; private set; }
    public int CantidadConfirmaciones { get; private set; }

    public Task CargarAsync(CancellationToken cancellationToken)
    {
        CantidadCargas++;
        return Task.CompletedTask;
    }

    public Task ConfirmarAsync(CancellationToken cancellationToken)
    {
        CantidadConfirmaciones++;
        return Task.CompletedTask;
    }

    public BorradorPresupuestoDto? Obtener(int presupuestoId, int usuarioId) =>
        _borradores.TryGetValue((presupuestoId, usuarioId), out var borrador)
            ? borrador.Clonar()
            : null;

    public void Guardar(int presupuestoId, int usuarioId, BorradorPresupuestoDto borrador) =>
        _borradores[(presupuestoId, usuarioId)] = borrador.Clonar();

    public void Eliminar(int presupuestoId, int usuarioId) =>
        _borradores.Remove((presupuestoId, usuarioId));

    public bool TieneCambiosPendientes(int presupuestoId, int usuarioId) =>
        _borradores.TryGetValue((presupuestoId, usuarioId), out var borrador) && borrador.TieneCambiosPendientes;

    public void RegistrarHuellaGuardada(int presupuestoId, int usuarioId, string huellaGuardada) =>
        _ultimasHuellasGuardadas[(presupuestoId, usuarioId)] = huellaGuardada;

    public bool CoincideConUltimoGuardadoEnSesion(int presupuestoId, int usuarioId, string huellaActual) =>
        _ultimasHuellasGuardadas.TryGetValue((presupuestoId, usuarioId), out string? huella)
        && string.Equals(huella, huellaActual, StringComparison.Ordinal);

    public List<BorradorPresupuestoDto> ObtenerPendientesDelUsuario(int usuarioId) =>
        _borradores
            .Where(par => par.Key.UsuarioId == usuarioId && par.Value.TieneCambiosPendientes)
            .OrderBy(par => par.Key.PresupuestoId)
            .Select(par => par.Value.Clonar())
            .ToList();

    public void RegistrarDescarte(int presupuestoId, int usuarioId, string borradorId) =>
        _ultimosDescartes[(presupuestoId, usuarioId)] = borradorId;

    public bool FueDescartadoEnSesion(int presupuestoId, int usuarioId, string borradorId) =>
        _ultimosDescartes.TryGetValue((presupuestoId, usuarioId), out string? descartado)
        && string.Equals(descartado, borradorId, StringComparison.Ordinal);

    public int CantidadVaciados { get; private set; }

    public void VaciarDatosTemporales()
    {
        CantidadVaciados++;
        SimularPerdidaDeSesion();
    }

    public void SimularPerdidaDeSesion()
    {
        _borradores.Clear();
        _ultimasHuellasGuardadas.Clear();
        _ultimosDescartes.Clear();
    }
}
