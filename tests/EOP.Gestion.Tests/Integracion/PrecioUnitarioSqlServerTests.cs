using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Materiales;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S2-T22: el precio unitario se valida contra los límites de la columna decimal(28,10) en los tres
// casos de uso (registrar material, agregar proveedor y editar precios). Hace falta SQL Server real
// porque SQLite no tiene esa precisión y no mostraría el problema.
public class PrecioUnitarioSqlServerTests : IAsyncLifetime
{
    private const decimal Maximo = ValidadorPrecioUnitario.ValorMaximo;
    private const decimal DemasiadoGrande = 1000000000000000000m;
    private const decimal DemasiadosDecimales = 1.00000000001m;

    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();
    private int _proveedorId;
    private int _otroProveedorId;
    private int _materialId;
    private int _relacionId;

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();

        var proveedor = DatosDePruebaProveedores.CrearProveedor("Proveedor A", "215521750017");
        var otroProveedor = DatosDePruebaProveedores.CrearProveedor("Proveedor B", "210000000019");
        var material = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        contexto.Proveedores.AddRange(proveedor, otroProveedor);
        contexto.Materiales.Add(material);
        await contexto.SaveChangesAsync();

        var relacion = new MaterialProveedor
        {
            MaterialId = material.Id,
            ProveedorId = proveedor.Id,
            PrecioUnitario = 100m,
            Moneda = Moneda.UYU,
            FechaUltimaActualizacion = DateTime.UtcNow
        };
        contexto.MaterialProveedores.Add(relacion);
        await contexto.SaveChangesAsync();

        _proveedorId = proveedor.Id;
        _otroProveedorId = otroProveedor.Id;
        _materialId = material.Id;
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

    private async Task<List<string>> RegistrarAsync(decimal precio)
    {
        using EopDbContext contexto = CrearContexto();

        return await new RegistrarMaterial(new RepositorioMateriales(contexto), new RepositorioProveedores(contexto))
            .EjecutarAsync("Material nuevo", UnidadMedida.Litro, _proveedorId, precio, Moneda.UYU);
    }

    private async Task<List<string>> AgregarProveedorAsync(decimal precio)
    {
        using EopDbContext contexto = CrearContexto();

        return await new AgregarProveedorAMaterial(new RepositorioMateriales(contexto), new RepositorioProveedores(contexto))
            .EjecutarAsync(_materialId, _otroProveedorId, precio, Moneda.UYU);
    }

    private async Task<List<string>> EditarAsync(decimal precio)
    {
        using EopDbContext contexto = CrearContexto();

        return await new EditarMaterial(new RepositorioMateriales(contexto)).EjecutarAsync(
            _materialId, "Cable de cobre", UnidadMedida.Metro,
            [new() { RelacionId = _relacionId, PrecioUnitario = precio, Moneda = Moneda.USD }]);
    }

    private async Task<decimal> LeerPrecioAsync(Func<MaterialProveedor, bool> filtro)
    {
        using EopDbContext lectura = CrearContexto();

        return (await lectura.MaterialProveedores.ToListAsync()).Single(filtro).PrecioUnitario;
    }

    // ---------- El máximo de la columna se guarda sin perder nada ----------

    [FactConSqlServer]
    public async Task Registrar_ConElPrecioMaximo_SeGuardaExacto()
    {
        Assert.Empty(await RegistrarAsync(Maximo));

        using EopDbContext lectura = CrearContexto();
        var materialNuevo = await lectura.Materiales.SingleAsync(m => m.Nombre == "Material nuevo");
        var relacion = await lectura.MaterialProveedores.SingleAsync(r => r.MaterialId == materialNuevo.Id);
        Assert.Equal(Maximo, relacion.PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task AgregarProveedor_ConElPrecioMaximo_SeGuardaExacto()
    {
        Assert.Empty(await AgregarProveedorAsync(Maximo));

        Assert.Equal(Maximo, await LeerPrecioAsync(r => r.ProveedorId == _otroProveedorId));
    }

    [FactConSqlServer]
    public async Task Editar_ConElPrecioMaximo_SeGuardaExacto()
    {
        Assert.Empty(await EditarAsync(Maximo));

        Assert.Equal(Maximo, await LeerPrecioAsync(r => r.Id == _relacionId));
    }

    // ---------- Fuera de rango o con demasiados decimales: se rechaza con mensaje, sin excepción ----------

    [FactConSqlServer]
    public async Task Registrar_ConPrecioFueraDeLaColumna_SeRechazaConMensajeYNoGuardaNada()
    {
        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadoGrande], await RegistrarAsync(DemasiadoGrande));
        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadoGrande], await RegistrarAsync(decimal.MaxValue));
        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadosDecimales], await RegistrarAsync(DemasiadosDecimales));

        using EopDbContext lectura = CrearContexto();
        Assert.Equal(1, await lectura.Materiales.CountAsync());
    }

    [FactConSqlServer]
    public async Task AgregarProveedor_ConPrecioFueraDeLaColumna_SeRechazaConMensajeYNoGuardaNada()
    {
        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadoGrande], await AgregarProveedorAsync(DemasiadoGrande));
        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadosDecimales], await AgregarProveedorAsync(DemasiadosDecimales));

        using EopDbContext lectura = CrearContexto();
        Assert.Equal(1, await lectura.MaterialProveedores.CountAsync());
    }

    [FactConSqlServer]
    public async Task Editar_ConPrecioFueraDeLaColumna_SeRechazaYSeConservaElValorAnterior()
    {
        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadoGrande], await EditarAsync(DemasiadoGrande));
        Assert.Equal([ValidadorPrecioUnitario.MensajeDemasiadosDecimales], await EditarAsync(DemasiadosDecimales));

        Assert.Equal(100m, await LeerPrecioAsync(r => r.Id == _relacionId));
    }
}
