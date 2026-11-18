using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EOP.Gestion.Tests.Unitarias;

// S1-T34: los repositorios reconocen un duplicado por el nombre del índice que figura en
// IndicesUnicos. Si alguien cambia la configuración y EF Core genera otro nombre, el repositorio
// dejaría de reconocerlo sin que nada falle; esta prueba lo detecta sin conectarse a una base.
public class IndicesUnicosTests
{
    public static TheoryData<Type, string, string> IndicesQueUsanLosRepositorios => new()
    {
        { typeof(Cliente), nameof(Cliente.Rut), IndicesUnicos.ClientesRut },
        { typeof(Proveedor), nameof(Proveedor.NombreNormalizado), IndicesUnicos.ProveedoresNombreNormalizado },
        { typeof(Proveedor), nameof(Proveedor.Rut), IndicesUnicos.ProveedoresRut },
        { typeof(Material), nameof(Material.NombreNormalizado), IndicesUnicos.MaterialesNombreNormalizado },
        { typeof(MaterialProveedor), nameof(MaterialProveedor.MaterialId), IndicesUnicos.MaterialProveedoresMaterialIdProveedorId },
        { typeof(Usuario), nameof(Usuario.CorreoCorporativo), IndicesUnicos.UsuariosCorreoCorporativo },
    };

    [Theory]
    [MemberData(nameof(IndicesQueUsanLosRepositorios))]
    public void NombreDelIndice_CoincideConElQueGeneraEfCore(Type entidad, string propiedad, string nombreEsperado)
    {
        using var contexto = new EopDbContext(new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer("Server=.;Database=SinConexion")
            .Options);

        IModel modelo = contexto.GetService<IDesignTimeModel>().Model;

        IIndex indice = modelo.FindEntityType(entidad)!
            .GetIndexes()
            .Single(i => i.Properties[0].Name == propiedad);

        Assert.True(indice.IsUnique);
        Assert.Equal(nombreEsperado, indice.GetDatabaseName());
    }

    [Fact]
    public void IndiceDeConceptosPorMaterialProveedor_CoincideConElQueUsaElRepositorio()
    {
        using var contexto = new EopDbContext(new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer("Server=.;Database=SinConexion")
            .Options);
        var entidad = contexto.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ConceptoPresupuesto))!;
        var indice = Assert.Single(entidad.GetIndexes(), indice =>
            indice.Properties.Select(propiedad => propiedad.Name).SequenceEqual(
                [nameof(ConceptoPresupuesto.PresupuestoId), nameof(ConceptoPresupuesto.MaterialProveedorId)]));

        Assert.True(indice.IsUnique);
        Assert.Equal(IndicesUnicos.ConceptosPresupuestoMaterialProveedor, indice.GetDatabaseName());
    }
}
