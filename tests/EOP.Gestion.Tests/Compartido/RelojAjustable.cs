using Microsoft.Extensions.Internal;

namespace EOP.Gestion.Tests.Compartido;

// Reloj de prueba que se puede adelantar a mano. Implementa los dos relojes que usa el servicio de
// cotizaciones: TimeProvider (para saber qué día es en Uruguay) e ISystemClock (el que usa
// MemoryCache para decidir si una entrada venció). Así se prueba la expiración sin esperar de verdad.
public sealed class RelojAjustable(DateTimeOffset inicio) : TimeProvider, ISystemClock
{
    private DateTimeOffset _ahora = inicio;

    public override DateTimeOffset GetUtcNow() => _ahora;

    DateTimeOffset ISystemClock.UtcNow => _ahora;

    public void Avanzar(TimeSpan intervalo) => _ahora += intervalo;
}
