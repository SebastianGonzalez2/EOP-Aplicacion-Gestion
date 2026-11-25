using EOP.Gestion.Core.Interfaces.Presupuestos;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos;

// Implementa la sincronización asíncrona por usuario usando SemaphoreSlim con conteo de referencias.
//
// Cada usuario que tiene al menos una solicitud en curso comparte un SemaphoreSlim(1, 1).
// Cuando termina la última solicitud activa de ese usuario (ya sea con éxito, con error o por
// cancelación), el semáforo se elimina del diccionario y se libera con Dispose(). De esta forma
// la memoria ocupada nunca crece de manera ilimitada aunque pasen miles de usuarios distintos.
//
// Limitación técnica: al apoyarse en semáforos en memoria del proceso, protege solicitudes
// concurrentes dentro de una única instancia de la aplicación.
public class SincronizadorBorradorPresupuesto : ISincronizadorBorradorPresupuesto
{
    private sealed class EntradaBloqueo
    {
        public SemaphoreSlim Semaforo { get; } = new(1, 1);

        public int CantidadEnUso;
    }

    private readonly object _sincronizacionMapa = new();
    private readonly Dictionary<int, EntradaBloqueo> _bloqueosPorUsuario = new();

    public int CantidadBloqueosActivos
    {
        get
        {
            lock (_sincronizacionMapa)
            {
                return _bloqueosPorUsuario.Count;
            }
        }
    }

    public async Task<TResultado> EjecutarConBloqueoAsync<TResultado>(
        int usuarioId,
        Func<Task<TResultado>> operacion,
        CancellationToken cancellationToken)
    {
        EntradaBloqueo entrada = ObtenerYRegistrarEntrada(usuarioId);
        bool semaforoAdquirido = false;

        try
        {
            await entrada.Semaforo.WaitAsync(cancellationToken);
            semaforoAdquirido = true;

            return await operacion();
        }
        finally
        {
            if (semaforoAdquirido)
            {
                entrada.Semaforo.Release();
            }

            LiberarEntrada(usuarioId, entrada);
        }
    }

    public async Task EjecutarConBloqueoAsync(
        int usuarioId,
        Func<Task> operacion,
        CancellationToken cancellationToken)
    {
        await EjecutarConBloqueoAsync(
            usuarioId,
            async () =>
            {
                await operacion();
                return true;
            },
            cancellationToken);
    }

    private EntradaBloqueo ObtenerYRegistrarEntrada(int usuarioId)
    {
        lock (_sincronizacionMapa)
        {
            if (!_bloqueosPorUsuario.TryGetValue(usuarioId, out EntradaBloqueo? existente))
            {
                existente = new EntradaBloqueo();
                _bloqueosPorUsuario[usuarioId] = existente;
            }

            existente.CantidadEnUso = existente.CantidadEnUso + 1;
            return existente;
        }
    }

    private void LiberarEntrada(int usuarioId, EntradaBloqueo entrada)
    {
        bool debeLiberarSemaforo = false;

        lock (_sincronizacionMapa)
        {
            entrada.CantidadEnUso = entrada.CantidadEnUso - 1;

            if (entrada.CantidadEnUso == 0)
            {
                _bloqueosPorUsuario.Remove(usuarioId);
                debeLiberarSemaforo = true;
            }
        }

        if (debeLiberarSemaforo)
        {
            entrada.Semaforo.Dispose();
        }
    }
}
