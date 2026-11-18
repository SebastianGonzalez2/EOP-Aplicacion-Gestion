using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T05 (US-011): revisa el modelo de conceptos del presupuesto que armó Entity Framework,
// sin conectarse a ninguna base. La cadena de conexión está solo porque hay que indicar que
// es SQL Server. Las restricciones CHECK solo aparecen en el modelo "de diseño", el que usan
// las migraciones, y no en el modelo reducido que se usa al ejecutar consultas.
public class ConfiguracionConceptosPresupuestoTests
{
    private static EopDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer("Server=localhost;Database=EopPruebas;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

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

        Assert.False(contexto.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Entidades_DebenUsarLasTablasEsperadas()
    {
        using EopDbContext contexto = CrearContexto();

        Assert.Equal("ConceptosPresupuesto", ObtenerEntidad<ConceptoPresupuesto>(contexto).GetTableName());
        Assert.Equal("TiposConcepto", ObtenerEntidad<TipoConcepto>(contexto).GetTableName());
        Assert.Equal("MargenesBaseTipo", ObtenerEntidad<MargenBaseTipo>(contexto).GetTableName());
    }

    [Theory]
    [InlineData(nameof(ConceptoPresupuesto.Concepto), 200)]
    [InlineData(nameof(ConceptoPresupuesto.ConceptoDetectado), 200)]
    public void Concepto_TextosOpcionalesConLargoMaximo(string propiedad, int longitudMaxima)
    {
        using EopDbContext contexto = CrearContexto();
        IProperty columna = ObtenerEntidad<ConceptoPresupuesto>(contexto).FindProperty(propiedad)!;

        Assert.True(columna.IsNullable);
        Assert.Equal(longitudMaxima, columna.GetMaxLength());
    }

    [Theory]
    [InlineData(nameof(ConceptoPresupuesto.Cantidad), 28, 10)]
    [InlineData(nameof(ConceptoPresupuesto.PrecioUnitario), 28, 10)]
    [InlineData(nameof(ConceptoPresupuesto.PorcentajeMargen), 9, 4)]
    public void Concepto_DecimalesConservanLaPrecisionEsperada(string propiedad, int precision, int escala)
    {
        using EopDbContext contexto = CrearContexto();
        IProperty columna = ObtenerEntidad<ConceptoPresupuesto>(contexto).FindProperty(propiedad)!;

        Assert.True(columna.IsNullable);
        Assert.Equal(precision, columna.GetPrecision());
        Assert.Equal(escala, columna.GetScale());
    }

    [Theory]
    [InlineData(nameof(ConceptoPresupuesto.Unidad))]
    [InlineData(nameof(ConceptoPresupuesto.Moneda))]
    public void Concepto_EnumsOpcionalesSeGuardanComoEntero(string propiedad)
    {
        using EopDbContext contexto = CrearContexto();
        IProperty columna = ObtenerEntidad<ConceptoPresupuesto>(contexto).FindProperty(propiedad)!;

        Assert.True(columna.IsNullable);
        Assert.Equal("int", columna.GetColumnType());
    }

    [Theory]
    [InlineData(nameof(ConceptoPresupuesto.PresupuestoId), "Presupuestos")]
    [InlineData(nameof(ConceptoPresupuesto.TipoConceptoId), "TiposConcepto")]
    [InlineData(nameof(ConceptoPresupuesto.MaterialId), "Materiales")]
    public void Concepto_TieneClaveForaneaQueNoBorraEnCascada(string propiedad, string tablaPrincipal)
    {
        using EopDbContext contexto = CrearContexto();
        IForeignKey clave = ObtenerEntidad<ConceptoPresupuesto>(contexto).GetForeignKeys()
            .Single(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == propiedad);

        Assert.Equal(tablaPrincipal, clave.PrincipalEntityType.GetTableName());
        Assert.Equal(DeleteBehavior.Restrict, clave.DeleteBehavior);
    }

    [Fact]
    public void Concepto_ReferenciaAlMismoMaterialDeLaRelacionConElProveedor()
    {
        using EopDbContext contexto = CrearContexto();
        IForeignKey clave = ObtenerEntidad<ConceptoPresupuesto>(contexto).GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(MaterialProveedor));

        Assert.Equal(
            [nameof(ConceptoPresupuesto.MaterialProveedorId), nameof(ConceptoPresupuesto.MaterialId)],
            clave.Properties.Select(propiedad => propiedad.Name));
        Assert.Equal(
            [nameof(MaterialProveedor.Id), nameof(MaterialProveedor.MaterialId)],
            clave.PrincipalKey.Properties.Select(propiedad => propiedad.Name));
        Assert.Equal(DeleteBehavior.Restrict, clave.DeleteBehavior);
    }

    [Fact]
    public void Concepto_TieneLasRestriccionesCheckEsperadas()
    {
        using EopDbContext contexto = CrearContexto();
        string[] restricciones = ObtenerEntidad<ConceptoPresupuesto>(contexto)
            .GetCheckConstraints()
            .Select(restriccion => restriccion.Name!)
            .Order()
            .ToArray();

        Assert.Equal(
            [
                "CK_ConceptosPresupuesto_CantidadPositiva",
                "CK_ConceptosPresupuesto_ConceptoNoVacio",
                "CK_ConceptosPresupuesto_MargenConPorcentaje",
                "CK_ConceptosPresupuesto_MonedaValida",
                "CK_ConceptosPresupuesto_PorcentajeNoNegativo",
                "CK_ConceptosPresupuesto_PrecioYMonedaJuntos",
                "CK_ConceptosPresupuesto_ReferenciaCompleta",
                "CK_ConceptosPresupuesto_UnidadValida"
            ],
            restricciones);
    }

    [Fact]
    public void Concepto_TieneIndiceComunSobreElPresupuesto()
    {
        using EopDbContext contexto = CrearContexto();
        IEntityType entidad = ObtenerEntidad<ConceptoPresupuesto>(contexto);
        IIndex indice = entidad.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(ConceptoPresupuesto.PresupuestoId));

        Assert.False(indice.IsUnique);
    }

    [Fact]
    public void Concepto_ImpideRepetirMaterialProveedorDentroDelMismoPresupuesto()
    {
        using EopDbContext contexto = CrearContexto();
        IEntityType entidad = ObtenerEntidad<ConceptoPresupuesto>(contexto);
        IIndex indice = entidad.GetIndexes().Single(i => i.GetDatabaseName() == "IX_ConceptosPresupuesto_PresupuestoId_MaterialProveedorId");

        Assert.True(indice.IsUnique);
        Assert.Equal(
            [nameof(ConceptoPresupuesto.PresupuestoId), nameof(ConceptoPresupuesto.MaterialProveedorId)],
            indice.Properties.Select(propiedad => propiedad.Name));
        // El filtro evita que dos conceptos sin material choquen entre sí: en SQL Server los NULL se consideran iguales.
        Assert.Equal("[MaterialProveedorId] IS NOT NULL", indice.GetFilter());
    }

    [Fact]
    public void Concepto_AdmiteUnSoloMargenPorPresupuesto()
    {
        using EopDbContext contexto = CrearContexto();
        IEntityType entidad = ObtenerEntidad<ConceptoPresupuesto>(contexto);
        IIndex indice = entidad.GetIndexes().Single(i => i.GetDatabaseName() == "IX_ConceptosPresupuesto_UnicoMargen");

        Assert.True(indice.IsUnique);
        Assert.Equal(
            [nameof(ConceptoPresupuesto.PresupuestoId), nameof(ConceptoPresupuesto.TipoConceptoId)],
            indice.Properties.Select(propiedad => propiedad.Name));
        Assert.Equal($"[TipoConceptoId] = {TipoConcepto.MargenId}", indice.GetFilter());
    }

    [Fact]
    public void TipoConcepto_TieneNombreObligatorioUnicoYNoVacio()
    {
        using EopDbContext contexto = CrearContexto();
        IEntityType entidad = ObtenerEntidad<TipoConcepto>(contexto);
        IProperty nombre = entidad.FindProperty(nameof(TipoConcepto.Nombre))!;

        Assert.False(nombre.IsNullable);
        Assert.Equal(100, nombre.GetMaxLength());
        Assert.True(entidad.GetIndexes().Single(indice => indice.Properties.Single().Name == nameof(TipoConcepto.Nombre)).IsUnique);
        Assert.NotNull(entidad.FindCheckConstraint("CK_TiposConcepto_NombreNoVacio"));
    }

    [Fact]
    public void TipoConcepto_TieneLosSeisTiposFijosConSusIds()
    {
        using EopDbContext contexto = CrearContexto();
        Dictionary<string, int> tipos = ObtenerEntidad<TipoConcepto>(contexto)
            .GetSeedData()
            .ToDictionary(fila => (string)fila[nameof(TipoConcepto.Nombre)]!, fila => (int)fila[nameof(TipoConcepto.Id)]!);

        Assert.Equal(
            new Dictionary<string, int>
            {
                ["Material"] = TipoConcepto.MaterialId,
                ["Mano de obra"] = TipoConcepto.ManoDeObraId,
                ["Alquiler"] = TipoConcepto.AlquilerId,
                ["Traslado"] = TipoConcepto.TrasladoId,
                ["Costos indirectos"] = TipoConcepto.CostosIndirectosId,
                ["Margen"] = TipoConcepto.MargenId
            },
            tipos);
    }

    [Fact]
    public void MargenBaseTipo_TieneClaveCompuestaYNoPermiteRepetirElTipo()
    {
        using EopDbContext contexto = CrearContexto();
        IKey clave = ObtenerEntidad<MargenBaseTipo>(contexto).FindPrimaryKey()!;

        Assert.Equal(
            [nameof(MargenBaseTipo.ConceptoPresupuestoId), nameof(MargenBaseTipo.TipoConceptoId)],
            clave.Properties.Select(propiedad => propiedad.Name));
    }

    [Fact]
    public void MargenBaseTipo_NoPuedeIncluirElPropioMargenComoBase()
    {
        using EopDbContext contexto = CrearContexto();

        Assert.NotNull(ObtenerEntidad<MargenBaseTipo>(contexto).FindCheckConstraint("CK_MargenesBaseTipo_NoIncluyeMargen"));
    }

    [Fact]
    public void MargenBaseTipo_BorraSusFilasConElConceptoPeroNoConElTipo()
    {
        using EopDbContext contexto = CrearContexto();
        IEnumerable<IForeignKey> claves = ObtenerEntidad<MargenBaseTipo>(contexto).GetForeignKeys();

        IForeignKey haciaConcepto = claves.Single(fk => fk.PrincipalEntityType.ClrType == typeof(ConceptoPresupuesto));
        Assert.Equal(
            [nameof(MargenBaseTipo.ConceptoPresupuestoId), "TipoConceptoDuenoId"],
            haciaConcepto.Properties.Select(propiedad => propiedad.Name));
        Assert.Equal(
            [nameof(ConceptoPresupuesto.Id), nameof(ConceptoPresupuesto.TipoConceptoId)],
            haciaConcepto.PrincipalKey.Properties.Select(propiedad => propiedad.Name));
        Assert.Equal(DeleteBehavior.Cascade, haciaConcepto.DeleteBehavior);

        IForeignKey haciaTipo = claves.Single(fk => fk.PrincipalEntityType.ClrType == typeof(TipoConcepto));
        Assert.Equal(nameof(MargenBaseTipo.TipoConceptoId), haciaTipo.Properties.Single().Name);
        Assert.Equal(DeleteBehavior.Restrict, haciaTipo.DeleteBehavior);
    }
}
