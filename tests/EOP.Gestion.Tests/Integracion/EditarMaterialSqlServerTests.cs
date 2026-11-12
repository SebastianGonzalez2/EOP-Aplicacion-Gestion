using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Materiales;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S2-T20: dos personas dejan a distintos materiales con el mismo nombre a la vez. El índice único
// de Materiales.NombreNormalizado deja pasar a una sola y el repositorio lo informa como duplicado.
// Hace falta SQL Server real: la detección mira el código de error y el nombre del índice.
public class EditarMaterialSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();
    private int _materialId;
    private int _otroMaterialId;
    private int _relacionId;

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
        var otroMaterial = new Material { Nombre = "Caño de PVC", Unidad = UnidadMedida.Metro };
        contexto.Proveedores.Add(proveedor);
        contexto.Materiales.AddRange(material, otroMaterial);
        await contexto.SaveChangesAsync();

        var relacion = new MaterialProveedor
        {
            MaterialId = material.Id,
            ProveedorId = proveedor.Id,
            PrecioUnitario = 100m,
            Moneda = Moneda.UYU,
            FechaUltimaActualizacion = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)
        };
        contexto.MaterialProveedores.Add(relacion);
        await contexto.SaveChangesAsync();

        _materialId = material.Id;
        _otroMaterialId = otroMaterial.Id;
        _relacionId = relacion.Id;
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

    [FactConSqlServer]
    public async Task EdicionValida_ActualizaElMismoMaterialYSuRelacionEnSqlServer()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            var errores = await new EditarMaterial(new RepositorioMateriales(contexto)).EjecutarAsync(
                _materialId, "Cable de cobre 2,5 mm", UnidadMedida.Kilogramo,
                [new() { RelacionId = _relacionId, PrecioUnitario = 150.25m, Moneda = Moneda.USD }]);

            Assert.Empty(errores);
        }

        using EopDbContext lectura = CrearContexto();
        var material = await lectura.Materiales.SingleAsync(m => m.Id == _materialId);
        var relacion = await lectura.MaterialProveedores.SingleAsync();
        Assert.Equal("Cable de cobre 2,5 mm", material.Nombre);
        Assert.Equal(UnidadMedida.Kilogramo, material.Unidad);
        Assert.Equal(_relacionId, relacion.Id);
        Assert.Equal(150.25m, relacion.PrecioUnitario);
        Assert.Equal(Moneda.USD, relacion.Moneda);
        Assert.True(relacion.FechaUltimaActualizacion > new DateTime(2026, 1, 16));
    }

    [FactConSqlServer]
    public async Task NombreTomadoJustoAntesDeGuardar_DevuelveFalseYNoGuardaNingunCambio()
    {
        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioMateriales(contexto);

        // Esta persona prepara su edición: material y relación con cambios pendientes.
        Material material = (await repositorio.ObtenerParaEdicionAsync(_materialId))!;
        MaterialProveedor relacion = (await repositorio.ObtenerRelacionesParaEdicionAsync(_materialId)).Single();
        material.Nombre = "Caño de PVC";
        material.Unidad = UnidadMedida.Litro;
        relacion.PrecioUnitario = 999m;

        // Es la situación de la carrera: el nombre ya lo tiene otro material, y la base lo frena.
        Assert.False(await repositorio.GuardarEdicionSiNombreDisponibleAsync());

        using EopDbContext lectura = CrearContexto();
        var guardado = await lectura.Materiales.SingleAsync(m => m.Id == _materialId);
        Assert.Equal("Cable de cobre", guardado.Nombre);
        Assert.Equal(UnidadMedida.Metro, guardado.Unidad);
        Assert.Equal(100m, (await lectura.MaterialProveedores.SingleAsync()).PrecioUnitario);

        // Y el mismo contexto sigue sirviendo para otra operación.
        Assert.False(await repositorio.ExisteOtroConNombreNormalizadoAsync("CABLE DE COBRE", _materialId));
        Assert.True(await repositorio.ExisteOtroConNombreNormalizadoAsync("CAÑO DE PVC", _materialId));
        Assert.False(await repositorio.ExisteOtroConNombreNormalizadoAsync("CAÑO DE PVC", _otroMaterialId));
    }

    [FactConSqlServer]
    public async Task EjecutarAsync_SiOtroUsuarioTomaElNombreDespuesDeLaValidacion_InformaElMensajeDeSiempre()
    {
        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioQueCambiaElNombreAntesDeGuardar(new RepositorioMateriales(contexto), async () =>
        {
            using EopDbContext otroUsuario = CrearContexto();
            var materialDelOtro = await otroUsuario.Materiales.SingleAsync(m => m.Id == _otroMaterialId);
            materialDelOtro.Nombre = "Cable Nuevo";
            await otroUsuario.SaveChangesAsync();
        });

        var errores = await new EditarMaterial(repositorio).EjecutarAsync(_materialId, "cable NUEVO", UnidadMedida.Metro, []);

        Assert.Equal([RegistrarMaterial.MensajeNombreDuplicado], errores);
    }

    // Simula la carrera: la consulta previa dice que el nombre está libre y, justo antes de
    // guardar, otro usuario toma ese nombre.
    private sealed class RepositorioQueCambiaElNombreAntesDeGuardar(EOP.Gestion.Core.Interfaces.Materiales.IRepositorioMateriales repositorioReal, Func<Task> otroUsuarioGuarda)
        : EOP.Gestion.Core.Interfaces.Materiales.IRepositorioMateriales
    {
        public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default) =>
            repositorioReal.ExistePorNombreNormalizadoAsync(nombreNormalizado, cancellationToken);

        public Task<bool> AgregarSiNombreDisponibleAsync(Material material, MaterialProveedor primeraRelacion, CancellationToken cancellationToken = default) =>
            repositorioReal.AgregarSiNombreDisponibleAsync(material, primeraRelacion, cancellationToken);

        public Task<bool> ExistePorIdAsync(int materialId, CancellationToken cancellationToken = default) =>
            repositorioReal.ExistePorIdAsync(materialId, cancellationToken);

        public Task<bool> ExisteRelacionConProveedorAsync(int materialId, int proveedorId, CancellationToken cancellationToken = default) =>
            repositorioReal.ExisteRelacionConProveedorAsync(materialId, proveedorId, cancellationToken);

        public Task<bool> AgregarRelacionSiNoExisteAsync(MaterialProveedor relacion, CancellationToken cancellationToken = default) =>
            repositorioReal.AgregarRelacionSiNoExisteAsync(relacion, cancellationToken);

        public Task<bool> ExisteOtroConNombreNormalizadoAsync(string nombreNormalizado, int materialId, CancellationToken cancellationToken = default) =>
            repositorioReal.ExisteOtroConNombreNormalizadoAsync(nombreNormalizado, materialId, cancellationToken);

        public Task<Material?> ObtenerParaEdicionAsync(int materialId, CancellationToken cancellationToken = default) =>
            repositorioReal.ObtenerParaEdicionAsync(materialId, cancellationToken);

        public Task<List<MaterialProveedor>> ObtenerRelacionesParaEdicionAsync(int materialId, CancellationToken cancellationToken = default) =>
            repositorioReal.ObtenerRelacionesParaEdicionAsync(materialId, cancellationToken);

        public async Task<bool> GuardarEdicionSiNombreDisponibleAsync(CancellationToken cancellationToken = default)
        {
            await otroUsuarioGuarda();
            return await repositorioReal.GuardarEdicionSiNombreDisponibleAsync(cancellationToken);
        }
    }
}
