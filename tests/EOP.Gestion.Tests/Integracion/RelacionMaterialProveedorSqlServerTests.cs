using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S1-T25 (US-040): la relación material-proveedor es la que guarda el precio, la moneda y la fecha
// de cada combinación. Las reglas que la cuidan están en la base (clave foránea, índice único y
// restricciones CHECK), y algunas no se pueden comprobar en SQLite (por ejemplo, ahí el precio se
// guarda como texto y "[PrecioUnitario] > 0" no se comporta igual). Por eso estas pruebas usan un
// SQL Server real, con las tablas creadas por las migraciones.
public class RelacionMaterialProveedorSqlServerTests : IAsyncLifetime
{
    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    private int _materialId;
    private int _otroMaterialId;
    private int _proveedorId;
    private int _otroProveedorId;

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();

        var proveedor = DatosDePruebaProveedores.CrearProveedor("Fivisa", "215521750017");
        var otroProveedor = DatosDePruebaProveedores.CrearProveedor("Barraca Central", "210000000019");
        var material = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        var otroMaterial = new Material { Nombre = "Pintura blanca", Unidad = UnidadMedida.Litro };

        contexto.Proveedores.AddRange(proveedor, otroProveedor);
        contexto.Materiales.AddRange(material, otroMaterial);
        await contexto.SaveChangesAsync();

        _proveedorId = proveedor.Id;
        _otroProveedorId = otroProveedor.Id;
        _materialId = material.Id;
        _otroMaterialId = otroMaterial.Id;
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

    private static MaterialProveedor CrearRelacion(
        int materialId,
        int proveedorId,
        decimal precio = 100m,
        Moneda moneda = Moneda.UYU,
        DateTime? fecha = null) => new()
        {
            MaterialId = materialId,
            ProveedorId = proveedorId,
            PrecioUnitario = precio,
            Moneda = moneda,
            FechaUltimaActualizacion = fecha ?? new DateTime(2026, 11, 1, 10, 0, 0)
        };

    private static async Task<string> GuardarYObtenerMensajeDeErrorAsync(EopDbContext contexto)
    {
        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        return excepcion.GetBaseException().Message;
    }

    [FactConSqlServer]
    public async Task UnMaterialPuedeTenerVariosProveedores_CadaUnoConSuPropioPrecioMonedaYFecha()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MaterialProveedores.AddRange(
                CrearRelacion(_materialId, _proveedorId, 150m, Moneda.UYU, new DateTime(2026, 11, 1, 10, 0, 0)),
                CrearRelacion(_materialId, _otroProveedorId, 7.5m, Moneda.USD, new DateTime(2026, 11, 2, 11, 0, 0)));
            await contexto.SaveChangesAsync();
        }

        using EopDbContext lectura = CrearContexto();
        List<MaterialProveedor> relaciones = await lectura.MaterialProveedores
            .Where(relacion => relacion.MaterialId == _materialId)
            .OrderBy(relacion => relacion.PrecioUnitario)
            .ToListAsync();

        Assert.Equal(2, relaciones.Count);

        Assert.Equal(_otroProveedorId, relaciones[0].ProveedorId);
        Assert.Equal(7.5m, relaciones[0].PrecioUnitario);
        Assert.Equal(Moneda.USD, relaciones[0].Moneda);
        Assert.Equal(new DateTime(2026, 11, 2, 11, 0, 0), relaciones[0].FechaUltimaActualizacion);

        Assert.Equal(_proveedorId, relaciones[1].ProveedorId);
        Assert.Equal(150m, relaciones[1].PrecioUnitario);
        Assert.Equal(Moneda.UYU, relaciones[1].Moneda);
        Assert.Equal(new DateTime(2026, 11, 1, 10, 0, 0), relaciones[1].FechaUltimaActualizacion);
    }

    [FactConSqlServer]
    public async Task UnProveedorPuedeTenerVariosMateriales()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MaterialProveedores.AddRange(
                CrearRelacion(_materialId, _proveedorId),
                CrearRelacion(_otroMaterialId, _proveedorId));
            await contexto.SaveChangesAsync();
        }

        using EopDbContext lectura = CrearContexto();

        Assert.Equal(2, await lectura.MaterialProveedores.CountAsync(relacion => relacion.ProveedorId == _proveedorId));
    }

    [FactConSqlServer]
    public async Task ElPrecioConservaSuPrecisionDecimal()
    {
        // La columna es decimal(28,10): un precio con muchos decimales no se redondea a dos.
        const decimal precioConDecimales = 0.1234567891m;

        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MaterialProveedores.Add(CrearRelacion(_materialId, _proveedorId, precioConDecimales));
            await contexto.SaveChangesAsync();
        }

        using EopDbContext lectura = CrearContexto();

        Assert.Equal(precioConDecimales, (await lectura.MaterialProveedores.SingleAsync()).PrecioUnitario);
    }

    [FactConSqlServer]
    public async Task Base_DebeRechazarDosRelacionesDelMismoMaterialConElMismoProveedor()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MaterialProveedores.Add(CrearRelacion(_materialId, _proveedorId));
            await contexto.SaveChangesAsync();
        }

        using EopDbContext otroContexto = CrearContexto();
        otroContexto.MaterialProveedores.Add(CrearRelacion(_materialId, _proveedorId, 999m));

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(otroContexto);

        Assert.Contains("IX_MaterialProveedores_MaterialId_ProveedorId", mensaje);
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public async Task Base_DebeRechazarUnPrecioQueNoSeaMayorQueCero(double precioInvalido)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.MaterialProveedores.Add(CrearRelacion(_materialId, _proveedorId, (decimal)precioInvalido));

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(contexto);

        Assert.Contains("CK_MaterialProveedores_PrecioPositivo", mensaje);
    }

    [TheoryConSqlServer]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(99)]
    public async Task Base_DebeRechazarUnaMonedaQueNoSeaUyuNiUsd(int monedaInvalida)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.MaterialProveedores.Add(CrearRelacion(_materialId, _proveedorId, moneda: (Moneda)monedaInvalida));

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(contexto);

        Assert.Contains("CK_MaterialProveedores_MonedaValida", mensaje);
    }

    [FactConSqlServer]
    public async Task Base_DebeRechazarUnaRelacionConUnMaterialInexistente()
    {
        using EopDbContext contexto = CrearContexto();
        contexto.MaterialProveedores.Add(CrearRelacion(999_999, _proveedorId));

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(contexto);

        Assert.Contains("FK_MaterialProveedores_Materiales_MaterialId", mensaje);
    }

    [FactConSqlServer]
    public async Task Base_DebeRechazarUnaRelacionConUnProveedorInexistente()
    {
        using EopDbContext contexto = CrearContexto();
        contexto.MaterialProveedores.Add(CrearRelacion(_materialId, 999_999));

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(contexto);

        Assert.Contains("FK_MaterialProveedores_Proveedores_ProveedorId", mensaje);
    }

    // La relación se borra con Restrict: no se puede eliminar un material ni un proveedor que
    // tenga precios registrados, así nunca quedan referencias de precio huérfanas.
    [FactConSqlServer]
    public async Task Base_NoDebePermitirBorrarUnMaterialConPreciosRegistrados()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MaterialProveedores.Add(CrearRelacion(_materialId, _proveedorId));
            await contexto.SaveChangesAsync();
        }

        using EopDbContext otroContexto = CrearContexto();
        otroContexto.Materiales.Remove(await otroContexto.Materiales.SingleAsync(material => material.Id == _materialId));

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(otroContexto);

        Assert.Contains("FK_MaterialProveedores_Materiales_MaterialId", mensaje);
    }

    [FactConSqlServer]
    public async Task Base_NoDebePermitirBorrarUnProveedorConPreciosRegistrados()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            contexto.MaterialProveedores.Add(CrearRelacion(_materialId, _proveedorId));
            await contexto.SaveChangesAsync();
        }

        using EopDbContext otroContexto = CrearContexto();

        // Las cuentas bancarias también frenan el borrado del proveedor. Las sacamos antes para
        // que la única razón que quede sea la relación con precios, que es lo que probamos.
        await otroContexto.CuentasBancarias.Where(cuenta => cuenta.ProveedorId == _proveedorId).ExecuteDeleteAsync();

        otroContexto.Proveedores.Remove(await otroContexto.Proveedores.SingleAsync(proveedor => proveedor.Id == _proveedorId));

        string mensaje = await GuardarYObtenerMensajeDeErrorAsync(otroContexto);

        Assert.Contains("FK_MaterialProveedores_Proveedores_ProveedorId", mensaje);
    }
}
