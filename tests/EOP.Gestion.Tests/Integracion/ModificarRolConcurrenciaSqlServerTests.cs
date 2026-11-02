using System.Data.Common;
using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// La protección del último Administrador activo ante solicitudes simultáneas (RF-72),
// contra un SQL Server real, con el esquema que generan las migraciones. No alcanza con
// SQLite: lo que queremos comprobar es justamente cómo se comporta SQL Server con sus bloqueos.
//
// Cada "solicitud" usa su propio DbContext y por lo tanto su propia conexión, como dos
// pedidos HTTP distintos de la aplicación real.
//
// Varias pruebas corren con READ_COMMITTED_SNAPSHOT apagado y prendido. Es el modo en el
// que las lecturas no esperan a nadie. Viene prendido en Azure SQL Database (producción) y
// también en las bases que crea EF Core, y es donde un error de este tipo se haría notar.
// Lo probamos apagado también porque la protección no debería depender de esa configuración.
public class ModificarRolConcurrenciaSqlServerTests : IAsyncLifetime
{
    private const int RepeticionesDeLaCarrera = 25;

    // Cuánto le damos a una solicitud para demostrar que se quedó esperando.
    private static readonly TimeSpan TiempoDeEspera = TimeSpan.FromSeconds(2);

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

    private async Task ConfigurarLecturasConCopiaAsync(bool prendido)
    {
        using EopDbContext contexto = CrearContexto();

        if (prendido)
        {
            await contexto.Database.ExecuteSqlRawAsync("ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE");
        }
        else
        {
            await contexto.Database.ExecuteSqlRawAsync("ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT OFF WITH ROLLBACK IMMEDIATE");
        }

        // Nos aseguramos de que la prueba corra realmente en el modo que dice.
        List<bool> estado = await contexto.Database
            .SqlQueryRaw<bool>("SELECT is_read_committed_snapshot_on AS [Value] FROM sys.databases WHERE name = DB_NAME()")
            .ToListAsync();

        Assert.Equal(prendido, Assert.Single(estado));
    }

    private async Task<List<int>> GuardarUsuariosAsync(int cantidad, RolUsuario rol)
    {
        using EopDbContext contexto = CrearContexto();
        var usuarios = new List<Usuario>();

        for (int numero = 0; numero < cantidad; numero++)
        {
            usuarios.Add(new Usuario
            {
                NombreCompleto = "Usuario de prueba",
                CorreoCorporativo = $"{Guid.NewGuid():N}@eop.com.uy",
                Rol = rol,
                Activo = true
            });
        }

        contexto.Usuarios.AddRange(usuarios);
        await contexto.SaveChangesAsync();

        return usuarios.Select(u => u.Id).ToList();
    }

    private async Task<int> ContarAdministradoresActivosAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await new RepositorioUsuarios(contexto).ContarAdministradoresActivosAsync();
    }

    // Una solicitud completa, de principio a fin, en su propia conexión.
    private async Task<ResultadoModificarRol> EjecutarSolicitudAsync(int usuarioId, RolUsuario nuevoRol)
    {
        using EopDbContext contexto = CrearContexto();
        var modificarRolUsuario = new ModificarRolUsuario(new RepositorioUsuarios(contexto));

        return await modificarRolUsuario.EjecutarAsync(usuarioId, nuevoRol);
    }

    // Larga todas las solicitudes a la vez, en hilos distintos, y espera a que terminen.
    // Si alguna termina con una excepción (por ejemplo, un deadlock), la prueba falla acá.
    private async Task<ResultadoModificarRol[]> DegradarTodosALaVezAsync(List<int> administradores)
    {
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var solicitudes = new List<Task<ResultadoModificarRol>>();

        foreach (int administradorId in administradores)
        {
            solicitudes.Add(Task.Run(async () =>
            {
                await largada.Task;
                return await EjecutarSolicitudAsync(administradorId, RolUsuario.Operador);
            }));
        }

        largada.SetResult();

        return await Task.WhenAll(solicitudes);
    }

    private static void AssertSoloUnaFueRechazada(ResultadoModificarRol[] resultados)
    {
        Assert.Equal(resultados.Length - 1, resultados.Count(r => r == ResultadoModificarRol.RolModificado));
        Assert.Equal(1, resultados.Count(r => r == ResultadoModificarRol.UltimoAdministradorActivo));
    }

    // ---------- La carrera ----------

    [TheoryConSqlServer]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DosSolicitudesSimultaneas_ContraLosDosUltimosAdministradores_SoloUnaSeAplica(bool lecturasConCopia)
    {
        await ConfigurarLecturasConCopiaAsync(lecturasConCopia);

        // Repetimos porque en una sola vuelta las solicitudes podrían no llegar a pisarse.
        for (int vuelta = 0; vuelta < RepeticionesDeLaCarrera; vuelta++)
        {
            List<int> administradores = await GuardarUsuariosAsync(2, RolUsuario.Administrador);

            ResultadoModificarRol[] resultados = await DegradarTodosALaVezAsync(administradores);

            AssertSoloUnaFueRechazada(resultados);
            Assert.Equal(1, await ContarAdministradoresActivosAsync());

            // Dejamos sin administradores la base para la vuelta siguiente.
            using EopDbContext contexto = CrearContexto();
            await contexto.Usuarios.ExecuteDeleteAsync();
        }
    }

    [TheoryConSqlServer]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MuchasSolicitudesSimultaneas_ContraTodosLosAdministradores_SiempreQuedaExactamenteUno(bool lecturasConCopia)
    {
        await ConfigurarLecturasConCopiaAsync(lecturasConCopia);
        List<int> administradores = await GuardarUsuariosAsync(8, RolUsuario.Administrador);

        ResultadoModificarRol[] resultados = await DegradarTodosALaVezAsync(administradores);

        AssertSoloUnaFueRechazada(resultados);
        Assert.Equal(1, await ContarAdministradoresActivosAsync());
    }

    // ---------- El orden exacto, sin depender de la suerte ----------

    [TheoryConSqlServer]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MientrasUnaSolicitudTieneElTurno_LaOtraEspera_YAlSeguirVeElCambioYSeRechaza(bool lecturasConCopia)
    {
        await ConfigurarLecturasConCopiaAsync(lecturasConCopia);
        List<int> administradores = await GuardarUsuariosAsync(2, RolUsuario.Administrador);

        using EopDbContext contextoPrimera = CrearContexto();
        var repositorioPrimera = new RepositorioUsuarios(contextoPrimera);

        // La primera solicitud toma el turno y degrada a su administrador, pero no confirma todavía.
        await using IBloqueoAdministradores bloqueoPrimera = await repositorioPrimera.BloquearAdministradoresAsync();
        await repositorioPrimera.ActualizarRolAsync(administradores[0], RolUsuario.Operador);

        // Sin el bloqueo, la segunda solicitud contaría 2 administradores y seguiría de largo.
        Task<ResultadoModificarRol> segunda = Task.Run(() => EjecutarSolicitudAsync(administradores[1], RolUsuario.Operador));

        Task primeraEnTerminar = await Task.WhenAny(segunda, Task.Delay(TiempoDeEspera));
        Assert.NotSame(segunda, primeraEnTerminar);

        await bloqueoPrimera.ConfirmarAsync();

        Assert.Equal(ResultadoModificarRol.UltimoAdministradorActivo, await segunda);
        Assert.Equal(1, await ContarAdministradoresActivosAsync());
    }

    [FactConSqlServer]
    public async Task SiLaPrimeraSolicitudSeDescarta_LaSegundaVeElRolOriginalYPuedeContinuar()
    {
        List<int> administradores = await GuardarUsuariosAsync(2, RolUsuario.Administrador);

        using EopDbContext contextoPrimera = CrearContexto();
        var repositorioPrimera = new RepositorioUsuarios(contextoPrimera);

        IBloqueoAdministradores bloqueoPrimera = await repositorioPrimera.BloquearAdministradoresAsync();
        await repositorioPrimera.ActualizarRolAsync(administradores[0], RolUsuario.Operador);

        Task<ResultadoModificarRol> segunda = Task.Run(() => EjecutarSolicitudAsync(administradores[1], RolUsuario.Operador));
        Assert.NotSame(segunda, await Task.WhenAny(segunda, Task.Delay(TiempoDeEspera)));

        // Soltamos el turno sin confirmar: es lo que pasa si la primera falla a mitad de camino.
        await bloqueoPrimera.DisposeAsync();

        Assert.Equal(ResultadoModificarRol.RolModificado, await segunda);

        using EopDbContext contexto = CrearContexto();
        Usuario? primerAdministrador = await new RepositorioUsuarios(contexto).ObtenerPorIdAsync(administradores[0]);
        Assert.NotNull(primerAdministrador);
        Assert.Equal(RolUsuario.Administrador, primerAdministrador.Rol);
    }

    // ---------- Lo que el bloqueo no debe trabar ----------

    [TheoryConSqlServer]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MientrasHayUnTurnoTomado_LasLecturasComunesNoEsperan(bool lecturasConCopia)
    {
        // OnValidatePrincipal lee la tabla Usuarios en cada pedido de cada usuario:
        // un cambio de rol no puede dejar a todos los demás esperando.
        await ConfigurarLecturasConCopiaAsync(lecturasConCopia);
        List<int> administradores = await GuardarUsuariosAsync(2, RolUsuario.Administrador);

        using EopDbContext contextoConTurno = CrearContexto();
        await using IBloqueoAdministradores bloqueo = await new RepositorioUsuarios(contextoConTurno).BloquearAdministradoresAsync();

        using EopDbContext contextoLector = CrearContexto();
        Task<Usuario?> lectura = new RepositorioUsuarios(contextoLector).ObtenerPorIdAsync(administradores[0]);

        Assert.Same(lectura, await Task.WhenAny(lectura, Task.Delay(TiempoDeEspera)));
        Assert.NotNull(await lectura);
    }

    // ---------- El esquema real de SQL Server ----------

    [FactConSqlServer]
    public async Task CambioValido_QuedaGuardadoEnSqlServer()
    {
        List<int> administradores = await GuardarUsuariosAsync(1, RolUsuario.Administrador);
        List<int> operadores = await GuardarUsuariosAsync(1, RolUsuario.Operador);

        ResultadoModificarRol resultado = await EjecutarSolicitudAsync(operadores[0], RolUsuario.Supervisor);

        Assert.Equal(ResultadoModificarRol.RolModificado, resultado);

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioUsuarios(contexto);
        Usuario? operador = await repositorio.ObtenerPorIdAsync(operadores[0]);
        Usuario? administrador = await repositorio.ObtenerPorIdAsync(administradores[0]);
        Assert.Equal(RolUsuario.Supervisor, operador?.Rol);
        Assert.Equal(RolUsuario.Administrador, administrador?.Rol);
    }

    [FactConSqlServer]
    public async Task UltimoAdministrador_SinNadieMasCompitiendo_SeRechazaYConservaElRol()
    {
        List<int> administradores = await GuardarUsuariosAsync(1, RolUsuario.Administrador);

        ResultadoModificarRol resultado = await EjecutarSolicitudAsync(administradores[0], RolUsuario.Operador);

        Assert.Equal(ResultadoModificarRol.UltimoAdministradorActivo, resultado);
        Assert.Equal(1, await ContarAdministradoresActivosAsync());
    }

    [FactConSqlServer]
    public async Task RolFueraDeLosFijos_LoRechazaLaRestriccionDeLaTabla()
    {
        List<int> operadores = await GuardarUsuariosAsync(1, RolUsuario.Operador);

        using EopDbContext contexto = CrearContexto();

        await Assert.ThrowsAnyAsync<DbException>(() =>
            new RepositorioUsuarios(contexto).ActualizarRolAsync(operadores[0], (RolUsuario)99));
    }
}
