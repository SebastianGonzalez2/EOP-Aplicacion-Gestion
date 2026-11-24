using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Tests.Compartido;

internal sealed class AlmacenBorradorEnMemoria : IAlmacenBorradorPresupuesto
{
    private readonly Dictionary<(int PresupuestoId, int UsuarioId), BorradorPresupuestoDto> _borradores = [];

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
}
