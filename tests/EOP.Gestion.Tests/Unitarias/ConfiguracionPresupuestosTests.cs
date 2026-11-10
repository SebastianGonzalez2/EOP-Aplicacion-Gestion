using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EOP.Gestion.Tests.Unitarias;

// S2-T23: revisa el modelo de Presupuesto que armó Entity Framework, sin conectarse a ninguna base.
public class ConfiguracionPresupuestosTests
{
    private static IModel CrearModelo()
    {
        using var contexto = new EopDbContext(new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer("Server=.;Database=SinConexion")
            .Options);

        return contexto.GetService<IDesignTimeModel>().Model;
    }

    private static IEntityType ObtenerEntidad<T>() => CrearModelo().FindEntityType(typeof(T))!;

    [Fact]
    public void Entidades_UsanLasTablasEsperadas()
    {
        Assert.Equal("Presupuestos", ObtenerEntidad<Presupuesto>().GetTableName());
        Assert.Equal("TiposProyecto", ObtenerEntidad<TipoProyecto>().GetTableName());
    }

    [Theory]
    [InlineData(nameof(Presupuesto.Codigo))]
    [InlineData(nameof(Presupuesto.NumeroCorrelativo))]
    public void Presupuesto_TieneIndiceUnicoEn(string propiedad)
    {
        IIndex indice = ObtenerEntidad<Presupuesto>().GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == propiedad);

        Assert.True(indice.IsUnique);
    }

    [Theory]
    [InlineData(nameof(Presupuesto.Codigo), 250)]
    [InlineData(nameof(Presupuesto.NombreProyecto), 200)]
    public void Presupuesto_TextosObligatoriosYConLargoMaximo(string propiedad, int longitudMaxima)
    {
        IProperty columna = ObtenerEntidad<Presupuesto>().FindProperty(propiedad)!;

        Assert.False(columna.IsNullable);
        Assert.Equal(longitudMaxima, columna.GetMaxLength());
    }

    [Fact]
    public void Presupuesto_GuardaElEstadoComoEntero()
    {
        IProperty estado = ObtenerEntidad<Presupuesto>().FindProperty(nameof(Presupuesto.Estado))!;

        Assert.Equal(typeof(int), estado.GetProviderClrType());
    }

    [Theory]
    [InlineData(nameof(Presupuesto.ClienteId), "Clientes")]
    [InlineData(nameof(Presupuesto.TipoProyectoId), "TiposProyecto")]
    [InlineData(nameof(Presupuesto.ResponsableId), "Usuarios")]
    public void Presupuesto_TieneClaveForaneaQueNoBorraEnCascada(string propiedad, string tablaPrincipal)
    {
        IForeignKey clave = ObtenerEntidad<Presupuesto>().GetForeignKeys()
            .Single(fk => fk.Properties.Single().Name == propiedad);

        Assert.Equal(tablaPrincipal, clave.PrincipalEntityType.GetTableName());
        Assert.Equal(DeleteBehavior.Restrict, clave.DeleteBehavior);
    }

    [Fact]
    public void Presupuesto_TieneLasRestriccionesCheckEsperadas()
    {
        string[] restricciones = ObtenerEntidad<Presupuesto>().GetCheckConstraints().Select(r => r.Name!).Order().ToArray();

        Assert.Equal(
            [
                "CK_Presupuestos_CodigoNoVacio",
                "CK_Presupuestos_CorrelativoDeCuatroCifras",
                "CK_Presupuestos_EstadoValido",
                "CK_Presupuestos_NombreProyectoNoVacio",
                "CK_Presupuestos_PlazoVigenciaPositivo"
            ],
            restricciones);
    }

    [Fact]
    public void SecuenciaDePresupuestos_ArrancaEn1_LlegaA9999_YNoEsCiclica()
    {
        ISequence secuencia = CrearModelo().FindSequence(EopDbContext.NombreSecuenciaPresupuestos)!;

        Assert.Equal(1, secuencia.StartValue);
        Assert.Equal(1, secuencia.IncrementBy);
        Assert.Equal(1, secuencia.MinValue);
        Assert.Equal(9999, secuencia.MaxValue);
        Assert.False(secuencia.IsCyclic);
    }
}
