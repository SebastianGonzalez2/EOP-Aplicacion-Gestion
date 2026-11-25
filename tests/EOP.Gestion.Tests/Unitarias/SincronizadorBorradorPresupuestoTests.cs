using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Tests.Unitarias;

// Pruebas deterministas (con barreras TaskCompletionSource) del sincronizador por usuario.
public class SincronizadorBorradorPresupuestoTests
{
    [Fact]
    public async Task MismoUsuario_SerializaOperacionesConcurrentesSinSolaparseYLiberaMemoriaAlTerminar()
    {
        var sincronizador = new SincronizadorBorradorPresupuesto();
        var primeroEntro = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberarPrimero = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var orden = new List<string>();

        Task primero = sincronizador.EjecutarConBloqueoAsync(1, async () =>
        {
            orden.Add("1-inicio");
            primeroEntro.SetResult();
            await liberarPrimero.Task;
            orden.Add("1-fin");
        }, CancellationToken.None);

        await primeroEntro.Task;

        var segundoInicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task segundo = sincronizador.EjecutarConBloqueoAsync(1, () =>
        {
            orden.Add("2-inicio");
            segundoInicio.SetResult();
            orden.Add("2-fin");
            return Task.CompletedTask;
        }, CancellationToken.None);

        // El segundo no puede haber entrado mientras el primero mantiene el bloqueo.
        await Task.Delay(30);
        Assert.False(segundoInicio.Task.IsCompleted);
        Assert.Equal(1, sincronizador.CantidadBloqueosActivos);

        liberarPrimero.SetResult();
        await Task.WhenAll(primero, segundo);

        Assert.Equal(["1-inicio", "1-fin", "2-inicio", "2-fin"], orden);
        Assert.Equal(0, sincronizador.CantidadBloqueosActivos);
    }

    [Fact]
    public async Task DistintosUsuarios_SeEjecutanEnParaleloSinBloquearseEntreSi()
    {
        var sincronizador = new SincronizadorBorradorPresupuesto();
        var usuario1Adentro = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var usuario2Adentro = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task tarea1 = sincronizador.EjecutarConBloqueoAsync(10, async () =>
        {
            usuario1Adentro.SetResult();
            await usuario2Adentro.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }, CancellationToken.None);

        Task tarea2 = sincronizador.EjecutarConBloqueoAsync(20, async () =>
        {
            usuario2Adentro.SetResult();
            await usuario1Adentro.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }, CancellationToken.None);

        // Si se bloquearan entre sí, esta barrera mutua produciría un timeout.
        await Task.WhenAll(tarea1, tarea2);
        Assert.Equal(0, sincronizador.CantidadBloqueosActivos);
    }

    [Fact]
    public async Task AnteErrorEnOperacion_LiberaBloqueoParaElSiguienteYNoFugaEntradas()
    {
        var sincronizador = new SincronizadorBorradorPresupuesto();
        var primeroEntro = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fallarPrimero = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task primero = sincronizador.EjecutarConBloqueoAsync(7, async () =>
        {
            primeroEntro.SetResult();
            await fallarPrimero.Task;
            throw new InvalidOperationException("Error simulado dentro del bloqueo.");
        }, CancellationToken.None);

        await primeroEntro.Task;

        Task<string> segundo = sincronizador.EjecutarConBloqueoAsync(
            7, () => Task.FromResult("ok"), CancellationToken.None);

        fallarPrimero.SetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => primero);
        Assert.Equal("ok", await segundo.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(0, sincronizador.CantidadBloqueosActivos);
    }

    [Fact]
    public async Task AnteCancelacionMientrasEspera_NoRompeElSemaforoNiDejaEntradasActivas()
    {
        var sincronizador = new SincronizadorBorradorPresupuesto();
        var primeroEntro = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberarPrimero = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task primero = sincronizador.EjecutarConBloqueoAsync(9, async () =>
        {
            primeroEntro.SetResult();
            await liberarPrimero.Task;
        }, CancellationToken.None);

        await primeroEntro.Task;

        using var cts = new CancellationTokenSource();
        Task segundo = sincronizador.EjecutarConBloqueoAsync(9, () => Task.CompletedTask, cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => segundo);

        liberarPrimero.SetResult();
        await primero;

        // Después de cancelar y finalizar el primero, una nueva solicitud funciona y no quedan fugas.
        int resultado = await sincronizador.EjecutarConBloqueoAsync(9, () => Task.FromResult(42), CancellationToken.None);
        Assert.Equal(42, resultado);
        Assert.Equal(0, sincronizador.CantidadBloqueosActivos);
    }
}
