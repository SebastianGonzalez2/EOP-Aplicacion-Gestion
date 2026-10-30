using System.Data.Common;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// Probamos RepositorioUsuarios contra una base SQLite temporal en memoria,
// como en RepositorioClientesTests. Las tablas se crean con la misma
// configuración de EF Core que usa SQL Server (UsuarioConfiguracion),
// así que también se crean los índices únicos.
//
// xUnit crea una instancia nueva de esta clase para cada prueba, por eso
// cada prueba arranca con una base vacía. Dispose la cierra al terminar.
public class RepositorioUsuariosTests : IDisposable
{
    private const string ObjectIdJuan = "11111111-1111-1111-1111-111111111111";
    private const string ObjectIdAna = "22222222-2222-2222-2222-222222222222";

    private readonly SqliteConnection _conexion;
    private readonly EopDbContext _contexto;
    private readonly RepositorioUsuarios _repositorio;

    public RepositorioUsuariosTests()
    {
        // Mientras la conexión siga abierta, la base en memoria existe.
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();

        _contexto = CrearContexto();
        _contexto.Database.EnsureCreated();

        _repositorio = new RepositorioUsuarios(_contexto);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }

    // Un contexto nuevo sobre la misma base. Lo usamos para leer lo que
    // realmente quedó guardado, sin datos que el otro contexto tenga en memoria.
    private EopDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(_conexion)
            .Options;

        return new EopDbContext(opciones);
    }

    private async Task<Usuario> GuardarUsuarioAsync(string correo, bool activo = true, string? entraObjectId = null)
    {
        var usuario = new Usuario
        {
            NombreCompleto = "Usuario de prueba",
            CorreoCorporativo = correo,
            Rol = RolUsuario.Operador,
            Activo = activo,
            EntraObjectId = entraObjectId
        };

        using var contexto = CrearContexto();
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();

        return usuario;
    }

    private async Task<string?> LeerEntraObjectIdGuardadoAsync(int usuarioId)
    {
        using var contexto = CrearContexto();
        return await contexto.Usuarios
            .Where(u => u.Id == usuarioId)
            .Select(u => u.EntraObjectId)
            .SingleAsync();
    }

    // ---------- Búsquedas ----------

    [Fact]
    public async Task ObtenerPorIdentidadExternaAsync_DevuelveElUsuarioVinculado()
    {
        await GuardarUsuarioAsync("ana@eop.com.uy", entraObjectId: ObjectIdAna);
        var juan = await GuardarUsuarioAsync("juan@eop.com.uy", entraObjectId: ObjectIdJuan);

        Usuario? resultado = await _repositorio.ObtenerPorIdentidadExternaAsync(ObjectIdJuan);

        Assert.NotNull(resultado);
        Assert.Equal(juan.Id, resultado.Id);
        Assert.Equal("juan@eop.com.uy", resultado.CorreoCorporativo);
    }

    [Fact]
    public async Task ObtenerPorIdentidadExternaAsync_DevuelveNullSiNadieTieneEseIdentificador()
    {
        await GuardarUsuarioAsync("juan@eop.com.uy");

        Usuario? resultado = await _repositorio.ObtenerPorIdentidadExternaAsync(ObjectIdJuan);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObtenerPorCorreoCorporativoAsync_DevuelveElUsuarioConEseCorreo()
    {
        await GuardarUsuarioAsync("ana@eop.com.uy");
        var juan = await GuardarUsuarioAsync("juan@eop.com.uy");

        Usuario? resultado = await _repositorio.ObtenerPorCorreoCorporativoAsync("juan@eop.com.uy");

        Assert.NotNull(resultado);
        Assert.Equal(juan.Id, resultado.Id);
        Assert.Null(resultado.EntraObjectId);
    }

    [Fact]
    public async Task ObtenerPorCorreoCorporativoAsync_DevuelveNullSiElCorreoNoExiste()
    {
        await GuardarUsuarioAsync("juan@eop.com.uy");

        Usuario? resultado = await _repositorio.ObtenerPorCorreoCorporativoAsync("otro@eop.com.uy");

        Assert.Null(resultado);
    }

    // ---------- Vinculación ----------

    [Fact]
    public async Task VincularIdentidadSiNoVinculadaAsync_VinculaUnUsuarioActivoSinIdentificador()
    {
        var juan = await GuardarUsuarioAsync("juan@eop.com.uy");

        bool resultado = await _repositorio.VincularIdentidadSiNoVinculadaAsync(juan.Id, ObjectIdJuan);

        Assert.True(resultado);
        Assert.Equal(ObjectIdJuan, await LeerEntraObjectIdGuardadoAsync(juan.Id));

        // Después de vincularlo, se lo encuentra por su identificador.
        Usuario? encontrado = await _repositorio.ObtenerPorIdentidadExternaAsync(ObjectIdJuan);
        Assert.NotNull(encontrado);
        Assert.Equal(juan.Id, encontrado.Id);
    }

    [Fact]
    public async Task VincularIdentidadSiNoVinculadaAsync_NoSobrescribeUnaVinculacionExistente()
    {
        var juan = await GuardarUsuarioAsync("juan@eop.com.uy", entraObjectId: ObjectIdJuan);

        bool resultado = await _repositorio.VincularIdentidadSiNoVinculadaAsync(juan.Id, ObjectIdAna);

        Assert.False(resultado);
        Assert.Equal(ObjectIdJuan, await LeerEntraObjectIdGuardadoAsync(juan.Id));
    }

    [Fact]
    public async Task VincularIdentidadSiNoVinculadaAsync_SegundoIntentoConOtroIdentificadorNoPisaElPrimero()
    {
        // Imita dos inicios de sesión casi simultáneos para el mismo usuario.
        var juan = await GuardarUsuarioAsync("juan@eop.com.uy");

        bool primero = await _repositorio.VincularIdentidadSiNoVinculadaAsync(juan.Id, ObjectIdJuan);
        bool segundo = await _repositorio.VincularIdentidadSiNoVinculadaAsync(juan.Id, ObjectIdAna);

        Assert.True(primero);
        Assert.False(segundo);
        Assert.Equal(ObjectIdJuan, await LeerEntraObjectIdGuardadoAsync(juan.Id));
    }

    [Fact]
    public async Task VincularIdentidadSiNoVinculadaAsync_NoVinculaUnUsuarioInactivo()
    {
        var juan = await GuardarUsuarioAsync("juan@eop.com.uy", activo: false);

        bool resultado = await _repositorio.VincularIdentidadSiNoVinculadaAsync(juan.Id, ObjectIdJuan);

        Assert.False(resultado);
        Assert.Null(await LeerEntraObjectIdGuardadoAsync(juan.Id));
    }

    [Fact]
    public async Task VincularIdentidadSiNoVinculadaAsync_DevuelveFalseSiElUsuarioNoExiste()
    {
        var juan = await GuardarUsuarioAsync("juan@eop.com.uy");

        // Ningún usuario tiene este Id, así que ninguna fila cumple el filtro del UPDATE.
        bool resultado = await _repositorio.VincularIdentidadSiNoVinculadaAsync(juan.Id + 999, ObjectIdJuan);

        Assert.False(resultado);
        Assert.Null(await LeerEntraObjectIdGuardadoAsync(juan.Id));
    }

    [Fact]
    public async Task VincularIdentidadSiNoVinculadaAsync_NoPermiteAsignarUnIdentificadorQueYaTieneOtroUsuario()
    {
        // Ana ya está vinculada con ObjectIdAna; intentamos darle el mismo a Juan.
        var ana = await GuardarUsuarioAsync("ana@eop.com.uy", entraObjectId: ObjectIdAna);
        var juan = await GuardarUsuarioAsync("juan@eop.com.uy");

        // El índice único de la base rechaza el UPDATE. DbException es la
        // clase base de los errores de SQLite y de SQL Server.
        await Assert.ThrowsAnyAsync<DbException>(() =>
            _repositorio.VincularIdentidadSiNoVinculadaAsync(juan.Id, ObjectIdAna));

        Assert.Null(await LeerEntraObjectIdGuardadoAsync(juan.Id));
        Assert.Equal(ObjectIdAna, await LeerEntraObjectIdGuardadoAsync(ana.Id));
    }

    // ---------- Índice único de EntraObjectId ----------

    [Fact]
    public async Task IndiceUnico_RechazaGuardarDosUsuariosConElMismoEntraObjectId()
    {
        await GuardarUsuarioAsync("ana@eop.com.uy", entraObjectId: ObjectIdJuan);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            GuardarUsuarioAsync("juan@eop.com.uy", entraObjectId: ObjectIdJuan));

        using var contexto = CrearContexto();
        Assert.Equal(1, await contexto.Usuarios.CountAsync());
    }

    [Fact]
    public async Task IndiceUnico_PermiteVariosUsuariosTodaviaSinVincular()
    {
        // El índice tiene filtro "IS NOT NULL": los null no cuentan como repetidos.
        await GuardarUsuarioAsync("ana@eop.com.uy");
        await GuardarUsuarioAsync("juan@eop.com.uy");

        using var contexto = CrearContexto();
        Assert.Equal(2, await contexto.Usuarios.CountAsync(u => u.EntraObjectId == null));
    }

    [Fact]
    public void IndiceUnico_EstaConfiguradoEnElModeloConFiltroParaNull()
    {
        // Revisamos la configuración que también genera la migración de SQL Server.
        var entidad = _contexto.Model.FindEntityType(typeof(Usuario))!;
        var indice = entidad.GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(Usuario.EntraObjectId));

        Assert.True(indice.IsUnique);
        Assert.Equal("[EntraObjectId] IS NOT NULL", indice.GetFilter());
    }
}
