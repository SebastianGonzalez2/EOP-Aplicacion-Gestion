using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EOP.Gestion.Tests.Unitarias;

// Revisa el modelo que armó Entity Framework a partir de las configuraciones. No se conecta a
// ninguna base: la cadena de conexión está solo porque hay que indicar que es SQL Server.
public class ConfiguracionProveedoresTests
{
    private static EopDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer("Server=localhost;Database=EopPruebas;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new EopDbContext(opciones);
    }

    // Las restricciones CHECK solo aparecen en el modelo "de diseño", el que usan las
    // migraciones, y no en el modelo reducido que se usa al ejecutar consultas.
    private static IEntityType ObtenerEntidad<T>(EopDbContext contexto)
    {
        IModel modelo = contexto.GetService<IDesignTimeModel>().Model;
        IEntityType? entidad = modelo.FindEntityType(typeof(T));
        Assert.NotNull(entidad);

        return entidad;
    }

    [Fact]
    public void Migraciones_DebenCorresponderAlModeloActual()
    {
        using EopDbContext contexto = CrearContexto();

        // Si esto da true, alguien cambió una entidad o una configuración y se olvidó de
        // generar la migración, así que la base real no tendría ese cambio.
        Assert.False(contexto.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Entidades_DebenUsarLasTablasEsperadas()
    {
        using EopDbContext contexto = CrearContexto();

        Assert.Equal("Proveedores", ObtenerEntidad<Proveedor>(contexto).GetTableName());
        Assert.Equal("CuentasBancarias", ObtenerEntidad<CuentaBancaria>(contexto).GetTableName());
    }

    [Theory]
    [InlineData(nameof(Proveedor.Nombre), false, 200)]
    [InlineData(nameof(Proveedor.NombreNormalizado), false, 200)]
    [InlineData(nameof(Proveedor.Rut), false, 12)]
    [InlineData(nameof(Proveedor.Telefono), false, 16)]
    [InlineData(nameof(Proveedor.CorreoContacto), true, 254)]
    [InlineData(nameof(Proveedor.CorreoFacturacion), true, 254)]
    public void Proveedor_DebeConfigurarObligatoriedadYLongitud(string propiedad, bool admiteNulo, int longitudMaxima)
    {
        using EopDbContext contexto = CrearContexto();
        IProperty? propiedadDelModelo = ObtenerEntidad<Proveedor>(contexto).FindProperty(propiedad);

        Assert.NotNull(propiedadDelModelo);
        Assert.Equal(admiteNulo, propiedadDelModelo.IsNullable);
        Assert.Equal(longitudMaxima, propiedadDelModelo.GetMaxLength());
    }

    [Theory]
    [InlineData(nameof(CuentaBancaria.NumeroCuenta), 50)]
    [InlineData(nameof(CuentaBancaria.Titular), 200)]
    public void CuentaBancaria_DebeConfigurarObligatoriedadYLongitud(string propiedad, int longitudMaxima)
    {
        using EopDbContext contexto = CrearContexto();
        IProperty? propiedadDelModelo = ObtenerEntidad<CuentaBancaria>(contexto).FindProperty(propiedad);

        Assert.NotNull(propiedadDelModelo);
        Assert.False(propiedadDelModelo.IsNullable);
        Assert.Equal(longitudMaxima, propiedadDelModelo.GetMaxLength());
    }

    [Theory]
    [InlineData(nameof(CuentaBancaria.Banco))]
    [InlineData(nameof(CuentaBancaria.Moneda))]
    public void CuentaBancaria_BancoYMoneda_DebenGuardarseComoNumeroObligatorio(string propiedad)
    {
        using EopDbContext contexto = CrearContexto();
        IProperty? propiedadDelModelo = ObtenerEntidad<CuentaBancaria>(contexto).FindProperty(propiedad);

        Assert.NotNull(propiedadDelModelo);
        Assert.False(propiedadDelModelo.IsNullable);
        Assert.Equal("int", propiedadDelModelo.GetColumnType());
    }

    [Theory]
    [InlineData(nameof(Proveedor.NombreNormalizado))]
    [InlineData(nameof(Proveedor.Rut))]
    public void Proveedor_DebeTenerIndiceUnico(string propiedad)
    {
        using EopDbContext contexto = CrearContexto();
        IEntityType entidad = ObtenerEntidad<Proveedor>(contexto);
        IProperty? propiedadDelModelo = entidad.FindProperty(propiedad);
        Assert.NotNull(propiedadDelModelo);

        IIndex? indice = entidad.FindIndex(propiedadDelModelo);

        Assert.NotNull(indice);
        Assert.True(indice.IsUnique);
    }

    // Un índice único sobre Nombre daría una falsa garantía: dejaría pasar nombres que solo
    // cambian en los espacios y, además, rechazar o no las mayúsculas dependería de la base.
    [Fact]
    public void Proveedor_NoDebeTenerIndiceSobreElNombreSinNormalizar()
    {
        using EopDbContext contexto = CrearContexto();
        IEntityType entidad = ObtenerEntidad<Proveedor>(contexto);
        IProperty? propiedadNombre = entidad.FindProperty(nameof(Proveedor.Nombre));
        Assert.NotNull(propiedadNombre);

        Assert.Null(entidad.FindIndex(propiedadNombre));
    }

    [Fact]
    public void CuentaBancaria_DebeTenerClaveForaneaObligatoriaHaciaProveedorSinBorradoEnCascada()
    {
        using EopDbContext contexto = CrearContexto();
        IForeignKey claveForanea = Assert.Single(ObtenerEntidad<CuentaBancaria>(contexto).GetForeignKeys());

        Assert.Equal(typeof(Proveedor), claveForanea.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(CuentaBancaria.ProveedorId), Assert.Single(claveForanea.Properties).Name);
        Assert.True(claveForanea.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, claveForanea.DeleteBehavior);
        Assert.Equal(nameof(Proveedor.CuentasBancarias), claveForanea.PrincipalToDependent?.Name);
    }

    [Theory]
    [InlineData("CK_Proveedores_NombreNoVacio")]
    [InlineData("CK_Proveedores_TelefonoNoVacio")]
    [InlineData("CK_Proveedores_RutFormato")]
    [InlineData("CK_Proveedores_AlMenosUnCorreo")]
    public void Proveedor_DebeTenerRestriccion(string nombreRestriccion)
    {
        using EopDbContext contexto = CrearContexto();
        IEntityType entidad = ObtenerEntidad<Proveedor>(contexto);

        Assert.NotNull(entidad.FindCheckConstraint(nombreRestriccion));
    }

    [Theory]
    [InlineData("CK_CuentasBancarias_BancoValido")]
    [InlineData("CK_CuentasBancarias_MonedaValida")]
    [InlineData("CK_CuentasBancarias_NumeroCuentaNoVacio")]
    [InlineData("CK_CuentasBancarias_TitularNoVacio")]
    public void CuentaBancaria_DebeTenerRestriccion(string nombreRestriccion)
    {
        using EopDbContext contexto = CrearContexto();
        IEntityType entidad = ObtenerEntidad<CuentaBancaria>(contexto);

        Assert.NotNull(entidad.FindCheckConstraint(nombreRestriccion));
    }
}
