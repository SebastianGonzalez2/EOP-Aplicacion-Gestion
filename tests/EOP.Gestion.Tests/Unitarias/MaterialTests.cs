
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EOP.Gestion.Tests.Unitarias;

public class MaterialTests
{
    [Fact]
    public void ExistenExactamenteCatorceUnidadesFijas()
    {
        // Obtenemos todas las unidades que definimos en el enum.
        var unidades = Enum.GetValues<UnidadMedida>();

        // Comprobamos que sean exactamente las catorce permitidas.
        Assert.Equal(14, unidades.Length);

        // Las cinco originales tienen que conservar su número: es lo que
        // ya está guardado en la base para los materiales existentes.
        Assert.Equal(1, (int)UnidadMedida.Metro);
        Assert.Equal(2, (int)UnidadMedida.MetroCuadrado);
        Assert.Equal(3, (int)UnidadMedida.MetroCubico);
        Assert.Equal(4, (int)UnidadMedida.Kilogramo);
        Assert.Equal(5, (int)UnidadMedida.Litro);

        // Las nuevas siguen desde el 6.
        Assert.Equal(6, (int)UnidadMedida.Milimetro);
        Assert.Equal(7, (int)UnidadMedida.Centimetro);
        Assert.Equal(8, (int)UnidadMedida.Kilometro);
        Assert.Equal(9, (int)UnidadMedida.CentimetroCuadrado);
        Assert.Equal(10, (int)UnidadMedida.CentimetroCubico);
        Assert.Equal(11, (int)UnidadMedida.Mililitro);
        Assert.Equal(12, (int)UnidadMedida.Gramo);
        Assert.Equal(13, (int)UnidadMedida.Tonelada);
        Assert.Equal(14, (int)UnidadMedida.Unidad);
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

        // Verificamos que exista la restricción de las catorce unidades.
        Assert.Contains(
            entidad.GetCheckConstraints(),
            restriccion =>
                restriccion.Name == "CK_Materiales_UnidadValida" &&
                restriccion.Sql == "[Unidad] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)");
    }


}
