using EOP.Gestion.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios;

// Por dentro, el "turno" es una transacción abierta de la base de datos.
// Los bloqueos de SQL Server duran hasta que la transacción termina, así que
// confirmarla o descartarla es lo que deja pasar a la siguiente solicitud.
internal sealed class BloqueoAdministradores : IBloqueoAdministradores
{
    private readonly IDbContextTransaction _transaccion;

    public BloqueoAdministradores(IDbContextTransaction transaccion)
    {
        _transaccion = transaccion;
    }

    public async Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        await _transaccion.CommitAsync(cancellationToken);
    }

    // Si la transacción se libera sin haberse confirmado, se deshacen sus cambios.
    public async ValueTask DisposeAsync()
    {
        await _transaccion.DisposeAsync();
    }
}
