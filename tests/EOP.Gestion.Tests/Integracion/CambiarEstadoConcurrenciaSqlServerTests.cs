using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S2-T07 / RF-75: la protección del último Administrador activo cuando se INACTIVA, ante
// solicitudes simultáneas y contra un SQL Server real. Es el mismo turno que usa el cambio de
// rol (ModificarRolConcurrenciaSqlServerTests): acá se comprueba que inactivar lo pide también
// y que ambas operaciones se bloquean entre sí.
public class CambiarEstadoConcurrenciaSqlServerTests : IAsyncLifetime
{
    private const int RepeticionesDeLaCarrera = 25;

    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    private EopDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer(_cadenaConexion)
            .Options;

        return new EopDbContext(opciones);
    }

    private async Task<List<int>> GuardarAdministradoresAsync(int cantidad)
    {
        using EopDbContext contexto = CrearContexto();
        var usuarios = Enumerable.Range(0, cantidad)
            .Select(_ => new Usuario
            {
                NombreCompleto = "Usuario de prueba",
                CorreoCorporativo = $"{Guid.NewGuid():N}@eop.com.uy",
                Rol = RolUsuario.Administrador,
                Activo = true
            })
            .ToList();

        contexto.Usuarios.AddRange(usuarios);
        await contexto.SaveChangesAsync();

        return usuarios.Select(u => u.Id).ToList();
    }

    private async Task<int> ContarAdministradoresActivosAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await new RepositorioUsuarios(contexto).ContarAdministradoresActivosAsync();
    }

    // Una solicitud completa, de principio a fin, en su propia conexión (como un pedido HTTP distinto).
    private async Task<ResultadoCambiarEstadoUsuario> InactivarAsync(int usuarioId)
    {
        using EopDbContext contexto = CrearContexto();
        return await new CambiarEstadoUsuario(new RepositorioUsuarios(contexto)).EjecutarAsync(usuarioId, nuevoEstadoActivo: false);
    }

    private async Task<ResultadoModificarRol> DegradarAsync(int usuarioId)
    {
        using EopDbContext contexto = CrearContexto();
        return await new ModificarRolUsuario(new RepositorioUsuarios(contexto)).EjecutarAsync(usuarioId, RolUsuario.Operador);
    }

    // Larga todas las solicitudes a la vez y espera a que terminen. Si alguna termina con una
    // excepción (por ejemplo, un deadlock), la prueba falla acá.
    private static async Task<T[]> LargarJuntasAsync<T>(IEnumerable<Func<Task<T>>> solicitudes)
    {
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        List<Task<T>> tareas = solicitudes
            .Select(solicitud => Task.Run(async () =>
            {
                await largada.Task;
                return await solicitud();
            }))
            .ToList();

        largada.SetResult();

        return await Task.WhenAll(tareas);
    }

    [FactConSqlServer]
    public async Task DosSolicitudesSimultaneas_InactivandoALosDosUltimosAdministradores_SoloUnaSeAplica()
    {
        // Repetimos porque en una sola vuelta las solicitudes podrían no llegar a pisarse.
        for (int vuelta = 0; vuelta < RepeticionesDeLaCarrera; vuelta++)
        {
            List<int> administradores = await GuardarAdministradoresAsync(2);

            ResultadoCambiarEstadoUsuario[] resultados = await LargarJuntasAsync(
                administradores.Select<int, Func<Task<ResultadoCambiarEstadoUsuario>>>(id => () => InactivarAsync(id)));

            Assert.Equal(1, resultados.Count(r => r == ResultadoCambiarEstadoUsuario.UsuarioInactivado));
            Assert.Equal(1, resultados.Count(r => r == ResultadoCambiarEstadoUsuario.UltimoAdministradorActivo));
            Assert.Equal(1, await ContarAdministradoresActivosAsync());

            using EopDbContext contexto = CrearContexto();
            await contexto.Usuarios.ExecuteDeleteAsync();
        }
    }

    [FactConSqlServer]
    public async Task MuchasSolicitudesSimultaneas_InactivandoATodosLosAdministradores_SiempreQuedaExactamenteUno()
    {
        List<int> administradores = await GuardarAdministradoresAsync(8);

        ResultadoCambiarEstadoUsuario[] resultados = await LargarJuntasAsync(
            administradores.Select<int, Func<Task<ResultadoCambiarEstadoUsuario>>>(id => () => InactivarAsync(id)));

        Assert.Equal(7, resultados.Count(r => r == ResultadoCambiarEstadoUsuario.UsuarioInactivado));
        Assert.Equal(1, resultados.Count(r => r == ResultadoCambiarEstadoUsuario.UltimoAdministradorActivo));
        Assert.Equal(1, await ContarAdministradoresActivosAsync());
    }

    [FactConSqlServer]
    public async Task InactivarAUnAdministradorYDegradarAlOtroALaVez_NuncaDejaAlSistemaSinAdministradores()
    {
        // Inactivar y cambiar de rol tienen que compartir el turno: si no, cada una vería
        // 2 administradores activos y entre las dos dejarían ninguno.
        for (int vuelta = 0; vuelta < RepeticionesDeLaCarrera; vuelta++)
        {
            List<int> administradores = await GuardarAdministradoresAsync(2);

            Func<Task<bool>> inactivar = async () => await InactivarAsync(administradores[0]) == ResultadoCambiarEstadoUsuario.UsuarioInactivado;
            Func<Task<bool>> degradar = async () => await DegradarAsync(administradores[1]) == ResultadoModificarRol.RolModificado;

            bool[] seAplicaron = await LargarJuntasAsync([inactivar, degradar]);

            Assert.Equal(1, seAplicaron.Count(seAplico => seAplico));
            Assert.Equal(1, await ContarAdministradoresActivosAsync());

            using EopDbContext contexto = CrearContexto();
            await contexto.Usuarios.ExecuteDeleteAsync();
        }
    }

    [FactConSqlServer]
    public async Task InactivarYReactivar_QuedanGuardadosEnSqlServerSinEliminarAlUsuario()
    {
        List<int> administradores = await GuardarAdministradoresAsync(2);

        Assert.Equal(ResultadoCambiarEstadoUsuario.UsuarioInactivado, await InactivarAsync(administradores[0]));

        using (EopDbContext contexto = CrearContexto())
        {
            Usuario inactivo = await contexto.Usuarios.SingleAsync(u => u.Id == administradores[0]);
            Assert.False(inactivo.Activo);
            Assert.Equal(RolUsuario.Administrador, inactivo.Rol);
        }

        using EopDbContext contextoReactivacion = CrearContexto();
        ResultadoCambiarEstadoUsuario reactivacion = await new CambiarEstadoUsuario(new RepositorioUsuarios(contextoReactivacion))
            .EjecutarAsync(administradores[0], nuevoEstadoActivo: true);

        Assert.Equal(ResultadoCambiarEstadoUsuario.UsuarioReactivado, reactivacion);
        Assert.Equal(2, await ContarAdministradoresActivosAsync());
    }
}
