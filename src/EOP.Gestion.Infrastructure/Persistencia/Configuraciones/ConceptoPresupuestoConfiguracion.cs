using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class ConceptoPresupuestoConfiguracion : IEntityTypeConfiguration<ConceptoPresupuesto>
{
    public void Configure(EntityTypeBuilder<ConceptoPresupuesto> builder)
    {
        builder.ToTable("ConceptosPresupuesto", tabla =>
        {
            // RF-16 y RF-27: cantidad y unidad son opcionales, pero si vienen deben
            // ser válidas. Los IS NULL hacen falta porque un CHECK deja pasar lo
            // desconocido cuando compara contra NULL.
            tabla.HasCheckConstraint(
                "CK_ConceptosPresupuesto_CantidadPositiva",
                "[Cantidad] IS NULL OR [Cantidad] > 0");

            tabla.HasCheckConstraint(
                "CK_ConceptosPresupuesto_UnidadValida",
                "[Unidad] IS NULL OR [Unidad] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)");

            // RF-23: las únicas monedas son UYU y USD. En el concepto es opcional
            // porque el Margen se calcula siempre en UYU y no guarda una moneda propia.
            tabla.HasCheckConstraint(
                "CK_ConceptosPresupuesto_MonedaValida",
                "[Moneda] IS NULL OR [Moneda] IN (1, 2)");

            // El precio o importe y su moneda se guardan juntos o no se guardan.
            tabla.HasCheckConstraint(
                "CK_ConceptosPresupuesto_PrecioYMonedaJuntos",
                "([PrecioUnitario] IS NULL AND [Moneda] IS NULL) OR ([PrecioUnitario] IS NOT NULL AND [Moneda] IS NOT NULL)");

            // Una fila o no tiene material, o tiene material y la referencia elegida.
            // No se puede guardar media selección.
            tabla.HasCheckConstraint(
                "CK_ConceptosPresupuesto_ReferenciaCompleta",
                "([MaterialId] IS NULL AND [MaterialProveedorId] IS NULL) OR ([MaterialId] IS NOT NULL AND [MaterialProveedorId] IS NOT NULL)");

            // Si hay un texto de Concepto, no puede ser solo espacios.
            tabla.HasCheckConstraint(
                "CK_ConceptosPresupuesto_ConceptoNoVacio",
                "[Concepto] IS NULL OR TRIM([Concepto]) <> ''");

            // RF-27: únicamente el Margen lleva porcentaje, y el Margen debe llevarlo.
            tabla.HasCheckConstraint(
                "CK_ConceptosPresupuesto_MargenConPorcentaje",
                $"([TipoConceptoId] = {TipoConcepto.MargenId} AND [PorcentajeMargen] IS NOT NULL) OR ([TipoConceptoId] <> {TipoConcepto.MargenId} AND [PorcentajeMargen] IS NULL)");

            tabla.HasCheckConstraint(
                "CK_ConceptosPresupuesto_PorcentajeNoNegativo",
                "[PorcentajeMargen] IS NULL OR [PorcentajeMargen] >= 0");
        });

        builder.HasKey(c => c.Id);

        // La base de cálculo identifica al concepto dueño por su Id y su tipo.
        builder.HasAlternateKey(c => new { c.Id, c.TipoConceptoId });

        // Un presupuesto no se borra en cascada con sus conceptos: primero se
        // administran las filas. El catálogo tampoco se puede borrar si está
        // referenciado (RF-20, RF-21).
        builder.HasOne<Presupuesto>()
            .WithMany()
            .HasForeignKey(c => c.PresupuestoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TipoConcepto>()
            .WithMany()
            .HasForeignKey(c => c.TipoConceptoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(c => c.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MaterialProveedor>()
            .WithMany()
            .HasForeignKey(c => new { c.MaterialProveedorId, c.MaterialId })
            .HasPrincipalKey(referencia => new { referencia.Id, referencia.MaterialId })
            .HasConstraintName("FK_ConceptosPresupuesto_MaterialProveedores_ReferenciaMaterial")
            .OnDelete(DeleteBehavior.Restrict);

        // Buscar los conceptos de un presupuesto es la consulta más común de la tabla.
        builder.HasIndex(c => c.PresupuestoId);

        // RF-20: una misma combinación material-proveedor no puede repetirse dentro
        // del presupuesto. El filtro es obligatorio porque SQL Server considera
        // iguales todos los NULL: sin él, dos filas de mano de obra chocarían.
        builder.HasIndex(c => new { c.PresupuestoId, c.MaterialProveedorId })
            .IsUnique()
            .HasFilter("[MaterialProveedorId] IS NOT NULL")
            .HasDatabaseName(IndicesUnicos.ConceptosPresupuestoMaterialProveedor);

        // RF-27: como máximo un concepto Margen por presupuesto. También necesita
        // filtro para no afectar a los conceptos de otros tipos. Incluimos el tipo
        // en el índice (además del filtro) para que el índice normal de
        // PresupuestoId de arriba pueda existir por separado: un índice filtrado
        // solo sirve para las filas que cumplen el filtro.
        builder.HasIndex(c => new { c.PresupuestoId, c.TipoConceptoId })
            .IsUnique()
            .HasFilter($"[TipoConceptoId] = {TipoConcepto.MargenId}")
            .HasDatabaseName("IX_ConceptosPresupuesto_UnicoMargen");

        builder.Property(c => c.Concepto)
            .HasMaxLength(200);

        builder.Property(c => c.ConceptoDetectado)
            .HasMaxLength(200);

        // Misma precisión que MaterialProveedor.PrecioUnitario, para copiar el
        // precio sin perder decimales (DT-11).
        builder.Property(c => c.PrecioUnitario)
            .HasPrecision(28, 10);

        builder.Property(c => c.Cantidad)
            .HasPrecision(28, 10);

        // Los porcentajes no necesitan los diez decimales de un precio; alcanza
        // con cuatro para representar, por ejemplo, 12,5 %.
        builder.Property(c => c.PorcentajeMargen)
            .HasPrecision(9, 4);

        // Guardamos los enums como números enteros, igual que el resto del sistema.
        builder.Property(c => c.Unidad)
            .HasConversion<int?>();

        builder.Property(c => c.Moneda)
            .HasConversion<int?>();

        // La referencia al tipo de concepto se guarda como número entero.
        builder.Property(c => c.TipoConceptoId)
            .IsRequired();
    }
}
