using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EOP.Gestion.Tests.Integracion;

// El alta de usuarios contra un SQL Server real, con el esquema de las migraciones. SQLite no
// alcanza: lo que importa es cómo se comportan el índice único, la comparación sin distinguir
// mayúsculas y la espera entre transacciones en el motor que usa producción.
//
// Cada "solicitud" usa su propio DbContext y su propia conexión, como dos pedidos HTTP distintos.
public class RegistrarUsuarioSqlServerTests : IAsyncLifetime
{
    private const string MensajeCorreoYaRegistrado = "El correo corporativo ya se encuentra registrado.";
    private const string ObjectIdAna = "55555555-5555-5555-5555-555555555555";
    private const int RepeticionesDeLaCarrera = 25;

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

        await contexto.Database.ExecuteSqlRawAsync(prendido
            ? "ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE"
            : "ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT OFF WITH ROLLBACK IMMEDIATE");
    }

    private async Task<List<string>> EjecutarAltaAsync(string? nombre, string? correo, RolUsuario rol = RolUsuario.Operador)
    {
        using EopDbContext contexto = CrearContexto();
        var registrarUsuario = new RegistrarUsuario(new RepositorioUsuarios(contexto));

        return await registrarUsuario.EjecutarAsync(nombre, correo, rol);
    }

    private async Task GuardarUsuarioDirectamenteAsync(string correo)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Usuarios.Add(CrearUsuario(correo));
        await contexto.SaveChangesAsync();
    }

    private static Usuario CrearUsuario(string correo, RolUsuario rol = RolUsuario.Operador) => new()
    {
        NombreCompleto = "Usuario de prueba",
        CorreoCorporativo = correo,
        Rol = rol,
        Activo = true
    };

    private async Task<List<Usuario>> LeerUsuariosAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.Usuarios.AsNoTracking().ToListAsync();
    }

    private static async Task<List<string>[]> LargarAltasALaVezAsync(IEnumerable<Func<Task<List<string>>>> solicitudes)
    {
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        List<Task<List<string>>> tareas = solicitudes
            .Select(solicitud => Task.Run(async () =>
            {
                await largada.Task;
                return await solicitud();
            }))
            .ToList();

        largada.SetResult();

        return await Task.WhenAll(tareas);
    }

    // ---------- Lo que queda guardado ----------

    [FactConSqlServer]
    public async Task AltaValida_QuedaGuardadaActivaConSuRolYSinVincular()
    {
        List<string> errores = await EjecutarAltaAsync("Ana Pérez Ñandú", "Ana.Perez@EOP.com.uy", RolUsuario.Supervisor);

        Assert.Empty(errores);

        Usuario guardado = Assert.Single(await LeerUsuariosAsync());
        Assert.Equal("Ana Pérez Ñandú", guardado.NombreCompleto);
        Assert.Equal("ana.perez@eop.com.uy", guardado.CorreoCorporativo);
        Assert.Equal(RolUsuario.Supervisor, guardado.Rol);
        Assert.True(guardado.Activo);
        Assert.Null(guardado.EntraObjectId);
    }

    [FactConSqlServer]
    public async Task ValoresEnElLimite_EntranEnLasColumnasReales()
    {
        string nombre = new('n', 150);
        string correo = new string('c', 254 - "@eop.com.uy".Length) + "@eop.com.uy";

        Assert.Empty(await EjecutarAltaAsync(nombre, correo));

        Usuario guardado = Assert.Single(await LeerUsuariosAsync());
        Assert.Equal(nombre, guardado.NombreCompleto);
        Assert.Equal(correo, guardado.CorreoCorporativo);
    }

    [FactConSqlServer]
    public async Task VariosUsuariosSinVincular_ConvivenPorqueElIndiceDeEntraFiltraLosNull()
    {
        Assert.Empty(await EjecutarAltaAsync("Ana", "ana@eop.com.uy"));
        Assert.Empty(await EjecutarAltaAsync("Juan", "juan@eop.com.uy"));

        Assert.Equal(2, (await LeerUsuariosAsync()).Count(u => u.EntraObjectId == null));
    }

    [FactConSqlServer]
    public async Task RolFueraDeLosFijos_LoRechazaLaRestriccionDeLaTablaSinInformarloComoDuplicado()
    {
        using EopDbContext contexto = CrearContexto();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new RepositorioUsuarios(contexto).AgregarSiCorreoDisponibleAsync(CrearUsuario("ana@eop.com.uy", (RolUsuario)99)));

        Assert.Empty(await LeerUsuariosAsync());
    }

    // ---------- Duplicados y mayúsculas ----------

    [TheoryConSqlServer]
    [InlineData("ana.perez@eop.com.uy")]
    [InlineData("ANA.PEREZ@EOP.COM.UY")]
    [InlineData("  Ana.Perez@eop.com.uy  ")]
    public async Task CorreoRepetido_SeRechazaSinImportarMayusculasNiEspacios(string correoRepetido)
    {
        await GuardarUsuarioDirectamenteAsync("Ana.Perez@EOP.com.uy");

        List<string> errores = await EjecutarAltaAsync("Otra Persona", correoRepetido);

        Assert.Equal([MensajeCorreoYaRegistrado], errores);
        Assert.Single(await LeerUsuariosAsync());
    }

    [FactConSqlServer]
    public async Task ElIndiceUnico_RechazaAunSiLaConsultaPreviaNoLoDetecto_YElRepositorioLoInformaComoDuplicado()
    {
        // Es la situación exacta de la carrera: vamos directo al guardado, sin la consulta previa.
        await GuardarUsuarioDirectamenteAsync("ana.perez@eop.com.uy");

        using EopDbContext contexto = CrearContexto();
        bool guardado = await new RepositorioUsuarios(contexto).AgregarSiCorreoDisponibleAsync(CrearUsuario("ANA.PEREZ@eop.com.uy"));

        Assert.False(guardado);
        Assert.Single(await LeerUsuariosAsync());
    }

    [FactConSqlServer]
    public async Task CorreoRepetido_NoPisaAlUsuarioQueYaLoTenia()
    {
        using (EopDbContext contextoInicial = CrearContexto())
        {
            contextoInicial.Usuarios.Add(CrearUsuario("juan@eop.com.uy", RolUsuario.Administrador));
            await contextoInicial.SaveChangesAsync();
        }

        using EopDbContext contexto = CrearContexto();
        bool guardado = await new RepositorioUsuarios(contexto).AgregarSiCorreoDisponibleAsync(CrearUsuario("juan@eop.com.uy"));

        Assert.False(guardado);
        Usuario unico = Assert.Single(await LeerUsuariosAsync());
        Assert.Equal(RolUsuario.Administrador, unico.Rol);
    }

    [FactConSqlServer]
    public async Task DespuesDeUnCorreoRepetido_ElMismoContextoSigueSirviendoParaGuardar()
    {
        // Sin sacar del contexto al usuario rechazado, el guardado siguiente lo volvería a intentar y fallaría.
        await GuardarUsuarioDirectamenteAsync("juan@eop.com.uy");

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioUsuarios(contexto);

        Assert.False(await repositorio.AgregarSiCorreoDisponibleAsync(CrearUsuario("juan@eop.com.uy")));
        Assert.True(await repositorio.AgregarSiCorreoDisponibleAsync(CrearUsuario("ana@eop.com.uy")));
        Assert.Equal(2, (await LeerUsuariosAsync()).Count);
    }

    // ---------- Solicitudes simultáneas ----------

    [TheoryConSqlServer]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MuchasAltasSimultaneasConElMismoCorreo_SoloSeGuardaUna_YLasDemasRecibenElMotivo(bool lecturasConCopia)
    {
        await ConfigurarLecturasConCopiaAsync(lecturasConCopia);

        // Repetimos porque en una sola vuelta las solicitudes podrían no llegar a pisarse.
        for (int vuelta = 0; vuelta < RepeticionesDeLaCarrera; vuelta++)
        {
            string correo = $"carrera{vuelta}@eop.com.uy";

            List<string>[] resultados = await LargarAltasALaVezAsync(
                Enumerable.Range(0, 8).Select<int, Func<Task<List<string>>>>(_ => () => EjecutarAltaAsync("Usuario", correo)));

            Assert.Equal(1, resultados.Count(errores => errores.Count == 0));
            Assert.All(
                resultados.Where(errores => errores.Count > 0),
                errores => Assert.Equal([MensajeCorreoYaRegistrado], errores));
            Assert.Equal(1, (await LeerUsuariosAsync()).Count(u => u.CorreoCorporativo == correo));
        }
    }

    [TheoryConSqlServer]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AltasSimultaneasConDistintasMayusculas_TambienChocan(bool lecturasConCopia)
    {
        await ConfigurarLecturasConCopiaAsync(lecturasConCopia);

        List<string>[] resultados = await LargarAltasALaVezAsync(
        [
            () => EjecutarAltaAsync("Ana", "ana@eop.com.uy"),
            () => EjecutarAltaAsync("Ana", "ANA@EOP.COM.UY"),
            () => EjecutarAltaAsync("Ana", "Ana@Eop.Com.Uy")
        ]);

        Assert.Equal(1, resultados.Count(errores => errores.Count == 0));
        Assert.Single(await LeerUsuariosAsync());
    }

    [FactConSqlServer]
    public async Task AltasSimultaneasConCorreosDistintos_SeGuardanTodas()
    {
        List<string>[] resultados = await LargarAltasALaVezAsync(
            Enumerable.Range(0, 8).Select<int, Func<Task<List<string>>>>(numero => () => EjecutarAltaAsync("Usuario", $"usuario{numero}@eop.com.uy")));

        Assert.All(resultados, errores => Assert.Empty(errores));
        Assert.Equal(8, (await LeerUsuariosAsync()).Count);
    }

    [FactConSqlServer]
    public async Task MientrasOtraTransaccionInsertoElCorreoSinConfirmar_ElAltaEspera_YAlConfirmarseSeRechaza()
    {
        // El orden exacto de la carrera, sin depender de la suerte.
        using EopDbContext contextoPrimera = CrearContexto();
        await using IDbContextTransaction transaccionPrimera = await contextoPrimera.Database.BeginTransactionAsync();
        contextoPrimera.Usuarios.Add(CrearUsuario("ana@eop.com.uy"));
        await contextoPrimera.SaveChangesAsync();

        using EopDbContext contextoSegunda = CrearContexto();
        Task<bool> segunda = Task.Run(() =>
            new RepositorioUsuarios(contextoSegunda).AgregarSiCorreoDisponibleAsync(CrearUsuario("ana@eop.com.uy")));

        Assert.NotSame(segunda, await Task.WhenAny(segunda, Task.Delay(TiempoDeEspera)));

        await transaccionPrimera.CommitAsync();

        Assert.False(await segunda);
        Assert.Single(await LeerUsuariosAsync());
    }

    [FactConSqlServer]
    public async Task SiLaOtraTransaccionSeDescarta_ElAltaQueEsperabaSeGuarda()
    {
        using EopDbContext contextoPrimera = CrearContexto();
        IDbContextTransaction transaccionPrimera = await contextoPrimera.Database.BeginTransactionAsync();
        contextoPrimera.Usuarios.Add(CrearUsuario("ana@eop.com.uy"));
        await contextoPrimera.SaveChangesAsync();

        using EopDbContext contextoSegunda = CrearContexto();
        Task<bool> segunda = Task.Run(() =>
            new RepositorioUsuarios(contextoSegunda).AgregarSiCorreoDisponibleAsync(CrearUsuario("ana@eop.com.uy")));

        Assert.NotSame(segunda, await Task.WhenAny(segunda, Task.Delay(TiempoDeEspera)));

        await transaccionPrimera.RollbackAsync();
        await transaccionPrimera.DisposeAsync();

        Assert.True(await segunda);
        Assert.Single(await LeerUsuariosAsync());
    }

    // ---------- Vinculación en el primer acceso ----------

    [FactConSqlServer]
    public async Task UsuarioRegistrado_SeVinculaEnElPrimerAcceso_AunqueMicrosoftInformeElCorreoConOtrasMayusculas()
    {
        // Reutiliza el mecanismo de T10 con la identidad que le pasamos: no es un login real con Entra ID.
        Assert.Empty(await EjecutarAltaAsync("Ana", "Ana.Perez@eop.com.uy", RolUsuario.Supervisor));

        using EopDbContext contexto = CrearContexto();
        var identificarUsuario = new IdentificarOVincularUsuario(new RepositorioUsuarios(contexto));

        Usuario? vinculado = await identificarUsuario.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(ObjectIdAna, "ANA.Perez@EOP.com.uy");

        Assert.NotNull(vinculado);
        Assert.Equal(RolUsuario.Supervisor, vinculado.Rol);
        Assert.Equal(ObjectIdAna, Assert.Single(await LeerUsuariosAsync()).EntraObjectId);
    }
}
