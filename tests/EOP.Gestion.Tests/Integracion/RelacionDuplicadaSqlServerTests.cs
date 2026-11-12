using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Materiales;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S2-T19: dos personas agregan a la vez la misma relación material-proveedor. El índice único
// (MaterialId, ProveedorId) deja entrar a una sola y el repositorio lo informa como duplicado.
// Hace falta SQL Server real: la detección mira el código de error y el nombre del índice.
public class RelacionDuplicadaSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();
    private int _materialId;
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
        var material = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        contexto.Proveedores.Add(proveedor);
        contexto.Materiales.Add(material);
        await contexto.SaveChangesAsync();

        _proveedorId = proveedor.Id;
        _materialId = material.Id;
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

    private MaterialProveedor CrearRelacion(int? proveedorId = null) => new()
    {
        MaterialId = _materialId,
        ProveedorId = proveedorId ?? _proveedorId,
        PrecioUnitario = 100m,
        Moneda = Moneda.UYU,
        FechaUltimaActualizacion = DateTime.UtcNow
    };

    private async Task<bool> AgregarConContextoNuevoAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await new RepositorioMateriales(contexto).AgregarRelacionSiNoExisteAsync(CrearRelacion());
    }

    private async Task<int> ContarAsync()
    {
        using EopDbContext contexto = CrearContexto();
        return await contexto.MaterialProveedores.CountAsync();
    }

    [FactConSqlServer]
    public async Task RelacionNueva_SeGuarda()
    {
        Assert.True(await AgregarConContextoNuevoAsync());
        Assert.Equal(1, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task RelacionRepetida_AunqueNoPaseLaConsultaPrevia_DevuelveFalseSinGuardarOtra()
    {
        await AgregarConContextoNuevoAsync();

        Assert.False(await AgregarConContextoNuevoAsync());
        Assert.Equal(1, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task DespuesDeUnDuplicado_ElMismoContextoSigueSirviendoParaGuardar()
    {
        await AgregarConContextoNuevoAsync();

        using EopDbContext contexto = CrearContexto();
        var otroProveedor = DatosDePruebaProveedores.CrearProveedor("Otro", "210000000019");
        contexto.Proveedores.Add(otroProveedor);
        await contexto.SaveChangesAsync();

        var repositorio = new RepositorioMateriales(contexto);
        Assert.False(await repositorio.AgregarRelacionSiNoExisteAsync(CrearRelacion()));
        Assert.True(await repositorio.AgregarRelacionSiNoExisteAsync(CrearRelacion(otroProveedor.Id)));
        Assert.Equal(2, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task OtroErrorDeLaBase_NoSeInformaComoDuplicado()
    {
        using EopDbContext contexto = CrearContexto();

        // El proveedor no existe: la clave foránea frena el guardado, y eso no es un duplicado.
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new RepositorioMateriales(contexto).AgregarRelacionSiNoExisteAsync(CrearRelacion(proveedorId: 999_999)));

        Assert.Equal(0, await ContarAsync());
    }

    [FactConSqlServer]
    public async Task AltasSimultaneasDeLaMismaRelacion_SoloSeGuardaUna_YLasDemasRecibenElMotivo()
    {
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        List<Task<List<string>>> altas = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(async () =>
            {
                await largada.Task;
                using EopDbContext contexto = CrearContexto();
                var caso = new AgregarProveedorAMaterial(new RepositorioMateriales(contexto), new RepositorioProveedores(contexto));
                return await caso.EjecutarAsync(_materialId, _proveedorId, 100m, Moneda.UYU);
            }))
            .ToList();

        largada.SetResult();
        List<string>[] resultados = await Task.WhenAll(altas);

        Assert.Single(resultados, errores => errores.Count == 0);
        Assert.All(
            resultados.Where(errores => errores.Count > 0),
            errores => Assert.Equal(new[] { AgregarProveedorAMaterial.MensajeRelacionDuplicada }, errores));
        Assert.Equal(1, await ContarAsync());
    }
}
