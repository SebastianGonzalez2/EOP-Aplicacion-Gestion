using System.Data.Common;
using System.Net;
using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;
using static EOP.Gestion.Tests.Compartido.FabricaConEntraSimulado;

namespace EOP.Gestion.Tests.Integracion;

// S1-T30 — Lo que solo se puede comprobar contra un SQL Server real con el esquema de las migraciones:
// - la vinculación de identidades, que hasta ahora corría únicamente sobre listas en memoria y SQLite. Las dos
//   distinguen mayúsculas y no tienen el índice filtrado generado por la migración;
// - la protección del último Administrador con pedidos HTTP de verdad: login, cookie, OnValidatePrincipal,
//   [Authorize], token antifalsificación, Controller, caso de uso y repositorio, todo junto.
//
// Cada prueba usa su propia base, con nombre al azar, y la borra al terminar (ServidorSqlDePrueba).
public class AutenticacionYRolesSqlServerTests : IAsyncLifetime
{
    private const string ObjectIdJuan = "11111111-1111-1111-1111-111111111111";
    private const string ObjectIdOtro = "22222222-2222-2222-2222-222222222222";
    private const string CorreoJuan = "juan.perez@eop.com.uy";
    private const int RepeticionesDeLaCarrera = 15;

    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    public async Task InitializeAsync()
    {
        if (ServidorSqlDePrueba.EstaDisponible)
        {
            using EopDbContext contexto = CrearContexto();
            await contexto.Database.MigrateAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (ServidorSqlDePrueba.EstaDisponible)
        {
            using EopDbContext contexto = CrearContexto();
            await contexto.Database.EnsureDeletedAsync();
        }
    }

    private EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(_cadenaConexion).Options);

    private async Task<int> GuardarUsuarioAsync(string correo, string? entraObjectId = null, RolUsuario rol = RolUsuario.Operador)
    {
        using EopDbContext contexto = CrearContexto();
        var usuario = new Usuario
        {
            NombreCompleto = "Usuario de prueba",
            CorreoCorporativo = correo,
            Rol = rol,
            Activo = true,
            EntraObjectId = entraObjectId
        };

        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }

    private async Task<Usuario> LeerUsuarioAsync(int usuarioId)
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
    }

    // Un inicio de sesión completo en su propia conexión, como un pedido HTTP distinto.
    private async Task<Usuario?> IdentificarAsync(string entraObjectId, string? correo)
    {
        using EopDbContext contexto = CrearContexto();
        var identificar = new IdentificarOVincularUsuario(new RepositorioUsuarios(contexto));

        return await identificar.ObtenerUsuarioEopPorEntraIdOVincularloPorCorreoCorporativoAsync(entraObjectId, correo);
    }

    private static async Task<T[]> LargarALaVezAsync<T>(params Func<Task<T>>[] operaciones)
    {
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<T>[] tareas = operaciones
            .Select(operacion => Task.Run(async () =>
            {
                await largada.Task;
                return await operacion();
            }))
            .ToArray();

        largada.SetResult();
        return await Task.WhenAll(tareas);
    }

    // ---------- Vinculación ----------

    [FactConSqlServer]
    public async Task PrimerAcceso_ConElCorreoEnOtrasMayusculas_VinculaAlUsuario()
    {
        // Entra ID suele informar el correo tal como lo escribió quien creó la cuenta, y EOP lo guarda en minúsculas.
        // La coincidencia depende de la intercalación de la base: las listas en memoria y SQLite no la representan.
        int idJuan = await GuardarUsuarioAsync(CorreoJuan);

        Usuario? usuario = await IdentificarAsync(ObjectIdJuan, "Juan.Perez@EOP.com.uy");

        Assert.NotNull(usuario);
        Assert.Equal(idJuan, usuario.Id);
        Assert.Equal(ObjectIdJuan, (await LeerUsuarioAsync(idJuan)).EntraObjectId);
    }

    [FactConSqlServer]
    public async Task DosIdentidadesConElMismoCorreoALaVez_SoloUnaQuedaVinculadaYEntra()
    {
        for (int vuelta = 0; vuelta < RepeticionesDeLaCarrera; vuelta++)
        {
            string correo = $"carrera{vuelta}@eop.com.uy";
            string objectIdA = Guid.NewGuid().ToString();
            string objectIdB = Guid.NewGuid().ToString();
            int idUsuario = await GuardarUsuarioAsync(correo);

            Usuario?[] resultados = await LargarALaVezAsync(
                () => IdentificarAsync(objectIdA, correo),
                () => IdentificarAsync(objectIdB, correo));

            Assert.Single(resultados, resultado => resultado != null);

            string? vinculado = (await LeerUsuarioAsync(idUsuario)).EntraObjectId;
            Assert.Equal(resultados.Single(resultado => resultado != null)!.EntraObjectId, vinculado);
        }
    }

    [FactConSqlServer]
    public async Task LaMismaIdentidadEnDosPestanasALaVez_EntraEnLasDosConLaMismaVinculacion()
    {
        for (int vuelta = 0; vuelta < RepeticionesDeLaCarrera; vuelta++)
        {
            string correo = $"pestanas{vuelta}@eop.com.uy";
            string objectId = Guid.NewGuid().ToString();
            int idUsuario = await GuardarUsuarioAsync(correo);

            Usuario?[] resultados = await LargarALaVezAsync(
                () => IdentificarAsync(objectId, correo),
                () => IdentificarAsync(objectId, correo));

            Assert.All(resultados, resultado => Assert.Equal(idUsuario, resultado?.Id));
            Assert.Equal(objectId, (await LeerUsuarioAsync(idUsuario)).EntraObjectId);
        }
    }

    [FactConSqlServer]
    public async Task ObjectIdQueYaUsaOtroUsuario_LoRechazaElIndiceFiltradoDeLaMigracion()
    {
        int idAna = await GuardarUsuarioAsync("ana@eop.com.uy", ObjectIdOtro);
        int idJuan = await GuardarUsuarioAsync(CorreoJuan);

        using EopDbContext contexto = CrearContexto();

        await Assert.ThrowsAnyAsync<DbException>(() =>
            new RepositorioUsuarios(contexto).VincularIdentidadSiNoVinculadaAsync(idJuan, ObjectIdOtro));

        Assert.Null((await LeerUsuarioAsync(idJuan)).EntraObjectId);
        Assert.Equal(ObjectIdOtro, (await LeerUsuarioAsync(idAna)).EntraObjectId);
    }

    // ---------- El último Administrador, de punta a punta ----------

    private static Dictionary<string, object> ClaimsDe(string objectId, string correo) => new()
    {
        ["sub"] = objectId,
        ["oid"] = objectId,
        ["name"] = "Usuario de prueba",
        ["preferred_username"] = correo
    };

    private sealed record AdministradorConectado(int Id, Navegador Navegador, string Token);

    private async Task<AdministradorConectado> ConectarAdministradorAsync(FabricaConEntraSimulado fabrica, string correo)
    {
        string objectId = Guid.NewGuid().ToString();
        int id = await GuardarUsuarioAsync(correo, objectId, RolUsuario.Administrador);
        Navegador navegador = fabrica.CrearNavegador();

        await navegador.IniciarSesionAsync(ClaimsDe(objectId, correo));
        Assert.True(navegador.TieneSesion);

        return new AdministradorConectado(id, navegador, await navegador.ObtenerTokenAntifalsificacionAsync());
    }

    private static Task<HttpResponseMessage> DegradarAsync(AdministradorConectado solicitante, int usuarioId) =>
        solicitante.Navegador.PostAsync("/Usuarios/ModificarRol", new Dictionary<string, string>
        {
            ["UsuarioId"] = usuarioId.ToString(),
            ["NuevoRol"] = "Operador",
            ["__RequestVerificationToken"] = solicitante.Token
        });

    [FactConSqlServer]
    public async Task DosAdministradoresQueSeDegradanEntreSiALaVezPorHttp_SiempreQuedaUnAdministradorActivo()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);

        for (int vuelta = 0; vuelta < RepeticionesDeLaCarrera; vuelta++)
        {
            AdministradorConectado ana = await ConectarAdministradorAsync(fabrica, $"ana{vuelta}@eop.com.uy");
            AdministradorConectado beto = await ConectarAdministradorAsync(fabrica, $"beto{vuelta}@eop.com.uy");

            HttpResponseMessage[] respuestas = await LargarALaVezAsync(
                () => DegradarAsync(ana, beto.Id),
                () => DegradarAsync(beto, ana.Id));

            // Ninguna solicitud se rompe con un error de la base (por ejemplo, un deadlock).
            Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode));

            using EopDbContext contexto = CrearContexto();
            Assert.Equal(1, await contexto.Usuarios.CountAsync(u => u.Rol == RolUsuario.Administrador && u.Activo));

            // Dejamos la base sin administradores para la vuelta siguiente.
            await contexto.Usuarios.ExecuteDeleteAsync();
        }
    }

    [FactConSqlServer]
    public async Task UltimoAdministradorDegradadoPorHttp_ConElEsquemaReal_ConservaElRolYSusPermisos()
    {
        await using var fabrica = new FabricaConEntraSimulado(_cadenaConexion);
        AdministradorConectado administrador = await ConectarAdministradorAsync(fabrica, "unico@eop.com.uy");

        HttpResponseMessage respuesta = await DegradarAsync(administrador, administrador.Id);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(RolUsuario.Administrador, (await LeerUsuarioAsync(administrador.Id)).Rol);
        Assert.Equal(HttpStatusCode.OK, (await administrador.Navegador.GetAsync("/Usuarios/Registrar")).StatusCode);
    }
}
