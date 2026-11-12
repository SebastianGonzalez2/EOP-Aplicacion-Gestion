using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Interfaces.Materiales;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Materiales;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S1-T34: cuando dos personas registran el mismo material a la vez, el índice único de
// Materiales.NombreNormalizado deja entrar a una sola, y el repositorio lo informa como duplicado
// en lugar de dejar que termine en la página de error. Hace falta SQL Server real: la detección
// mira el código de error y el nombre del índice que manda SQL Server.
public class DuplicadosMaterialesSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    private int _proveedorId;

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();

        var proveedor = DatosDePruebaProveedores.CrearProveedor();
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();

        _proveedorId = proveedor.Id;
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

    private EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>().UseSqlServer(_cadenaConexion).Options);

    private static Material CrearMaterial(string nombre) => new() { Nombre = nombre, Unidad = UnidadMedida.Metro };

    private MaterialProveedor CrearRelacion(int? proveedorId = null) => new()
    {
        ProveedorId = proveedorId ?? _proveedorId,
        PrecioUnitario = 100m,
        Moneda = Moneda.UYU,
        FechaUltimaActualizacion = DateTime.UtcNow
    };

    private async Task<bool> AgregarConContextoNuevoAsync(string nombre)
    {
        using EopDbContext contexto = CrearContexto();
        return await new RepositorioMateriales(contexto).AgregarSiNombreDisponibleAsync(CrearMaterial(nombre), CrearRelacion());
    }

    private Task<List<string>> RegistrarConContextoNuevoAsync(string nombre)
    {
        return Task.Run(async () =>
        {
            using EopDbContext contexto = CrearContexto();
            var registrarMaterial = new RegistrarMaterial(new RepositorioMateriales(contexto), new RepositorioProveedores(contexto));

            return await registrarMaterial.EjecutarAsync(nombre, UnidadMedida.Metro, _proveedorId, 100m, Moneda.UYU);
        });
    }

    private async Task<(int Materiales, int Relaciones)> ContarAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return (await contexto.Materiales.CountAsync(), await contexto.MaterialProveedores.CountAsync());
    }

    [FactConSqlServer]
    public async Task NombreNuevo_SeGuardaConSuPrimeraRelacion()
    {
        Assert.True(await AgregarConContextoNuevoAsync("Caño de cobre"));
        Assert.Equal((1, 1), await ContarAsync());
    }

    [TheoryConSqlServer]
    [InlineData("Caño de cobre")]
    [InlineData("  CAÑO   de COBRE ")]
    public async Task NombreRepetido_AunqueNoPaseLaConsultaPrevia_DevuelveFalseSinGuardarNada(string nombreRepetido)
    {
        // Es la situación exacta de la carrera: vamos directo al guardado, sin la consulta previa.
        await AgregarConContextoNuevoAsync("Caño de cobre");

        Assert.False(await AgregarConContextoNuevoAsync(nombreRepetido));
        Assert.Equal((1, 1), await ContarAsync());
    }

    [FactConSqlServer]
    public async Task DespuesDeUnDuplicado_ElMismoContextoSigueSirviendoParaGuardar()
    {
        await AgregarConContextoNuevoAsync("Caño de cobre");

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioMateriales(contexto);

        Assert.False(await repositorio.AgregarSiNombreDisponibleAsync(CrearMaterial("Caño de cobre"), CrearRelacion()));
        Assert.True(await repositorio.AgregarSiNombreDisponibleAsync(CrearMaterial("Codo de cobre"), CrearRelacion()));
        Assert.Equal((2, 2), await ContarAsync());
    }

    [FactConSqlServer]
    public async Task OtroErrorDeLaBase_NoSeInformaComoDuplicado_YNoQuedaElMaterialSinRelacion()
    {
        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioMateriales(contexto);

        // El proveedor no existe: la clave foránea de la relación frena el guardado.
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repositorio.AgregarSiNombreDisponibleAsync(CrearMaterial("Caño de cobre"), CrearRelacion(proveedorId: 999_999)));

        Assert.Equal((0, 0), await ContarAsync());
    }

    [FactConSqlServer]
    public async Task EjecutarAsync_SiOtroUsuarioGuardaDespuesDeLaValidacion_InformaElMensajeDeSiempre()
    {
        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioQueDejaGuardarAOtroUsuarioAntes(
            new RepositorioMateriales(contexto),
            () => AgregarConContextoNuevoAsync("Caño de cobre"));
        var registrarMaterial = new RegistrarMaterial(repositorio, new RepositorioProveedores(contexto));

        List<string> errores = await registrarMaterial.EjecutarAsync("caño DE cobre", UnidadMedida.Metro, _proveedorId, 100m, Moneda.UYU);

        Assert.Equal(new[] { RegistrarMaterial.MensajeNombreDuplicado }, errores);
        Assert.Equal((1, 1), await ContarAsync());
    }

    [FactConSqlServer]
    public async Task MuchasAltasSimultaneasConElMismoNombre_SoloSeGuardaUna_YLasDemasRecibenElMotivo()
    {
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        List<Task<List<string>>> altas = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(async () =>
            {
                await largada.Task;
                return await RegistrarConContextoNuevoAsync("Caño de cobre");
            }))
            .ToList();

        largada.SetResult();
        List<string>[] resultados = await Task.WhenAll(altas);

        Assert.Single(resultados, errores => errores.Count == 0);
        Assert.All(
            resultados.Where(errores => errores.Count > 0),
            errores => Assert.Equal(new[] { RegistrarMaterial.MensajeNombreDuplicado }, errores));
        Assert.Equal((1, 1), await ContarAsync());
    }

    // Simula la carrera: la consulta previa dice que el nombre está libre y, justo antes de
    // guardar, otro usuario registra el mismo material.
    private sealed class RepositorioQueDejaGuardarAOtroUsuarioAntes(IRepositorioMateriales repositorioReal, Func<Task> otroUsuarioGuarda)
        : IRepositorioMateriales
    {
        public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default) =>
            repositorioReal.ExistePorNombreNormalizadoAsync(nombreNormalizado, cancellationToken);

        public async Task<bool> AgregarSiNombreDisponibleAsync(Material material, MaterialProveedor primeraRelacion, CancellationToken cancellationToken = default)
        {
            await otroUsuarioGuarda();
            return await repositorioReal.AgregarSiNombreDisponibleAsync(material, primeraRelacion, cancellationToken);
        }

        public Task<bool> ExistePorIdAsync(int materialId, CancellationToken cancellationToken = default) =>
            repositorioReal.ExistePorIdAsync(materialId, cancellationToken);

        public Task<bool> ExisteRelacionConProveedorAsync(int materialId, int proveedorId, CancellationToken cancellationToken = default) =>
            repositorioReal.ExisteRelacionConProveedorAsync(materialId, proveedorId, cancellationToken);

        public Task<bool> AgregarRelacionSiNoExisteAsync(MaterialProveedor relacion, CancellationToken cancellationToken = default) =>
            repositorioReal.AgregarRelacionSiNoExisteAsync(relacion, cancellationToken);
    }
}
