
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EOP.Gestion.Tests.Unitarias;

public class MaterialTests
{
    [Fact]
    public void ExistenExactamenteCincoUnidadesFijas()
    {
        // Obtenemos todas las unidades que definimos en el enum.
        var unidades = Enum.GetValues<UnidadMedida>();

        // Comprobamos que sean exactamente las cinco permitidas.
        Assert.Equal(5, unidades.Length);

        Assert.Equal(1, (int)UnidadMedida.Metro);
        Assert.Equal(2, (int)UnidadMedida.MetroCuadrado);
        Assert.Equal(3, (int)UnidadMedida.MetroCubico);
        Assert.Equal(4, (int)UnidadMedida.Kilogramo);
        Assert.Equal(5, (int)UnidadMedida.Litro);
    }

    [Fact]
    public void Material_PermiteAsignarNombreYUnidad()
    {
        // Creamos un material con una unidad fija.
        var material = new Material
        {
            Nombre = "Cable de cobre",
            Unidad = UnidadMedida.Metro
        };

        // Comprobamos que los datos se asignaron correctamente.
        Assert.Equal("Cable de cobre", material.Nombre);
        Assert.Equal(UnidadMedida.Metro, material.Unidad);
    }


    [Fact]
    public void ConfiguracionMaterial_RestringeLasUnidades()
    {
        // Creamos las opciones del contexto usando SQL Server.
        // No necesitamos conectarnos a una base de datos real.
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer("Server=localhost;Database=SoloPruebas")
            .Options;

        using var contexto = new EopDbContext(opciones);

        // Obtenemos el modelo de Material configurado por EF Core.
        var entidad = contexto.GetService<IDesignTimeModel>()
            .Model.FindEntityType(typeof(Material));

        Assert.NotNull(entidad);

        // Comprobamos que el nombre sea obligatorio.
        var nombre = entidad.FindProperty(nameof(Material.Nombre));

        Assert.NotNull(nombre);
        Assert.False(nombre.IsNullable);

        // Comprobamos que la unidad sea obligatoria.
        var unidad = entidad.FindProperty(nameof(Material.Unidad));

        Assert.NotNull(unidad);
        Assert.False(unidad.IsNullable);

        // Verificamos que exista la restricción de las cinco unidades.
        Assert.Contains(
            entidad.GetCheckConstraints(),
            restriccion =>
                restriccion.Name == "CK_Materiales_UnidadValida" &&
                restriccion.Sql == "[Unidad] IN (1, 2, 3, 4, 5)");
    }


}
