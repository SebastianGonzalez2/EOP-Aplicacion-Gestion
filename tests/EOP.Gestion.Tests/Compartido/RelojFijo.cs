namespace EOP.Gestion.Tests.Compartido;

// Reloj de prueba que siempre devuelve el mismo instante, para no depender de la hora real.
public sealed class RelojFijo(DateTimeOffset instante) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => instante;
}
