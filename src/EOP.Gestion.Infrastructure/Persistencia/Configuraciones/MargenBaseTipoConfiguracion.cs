using EOP.Gestion.Core.Entidades.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class MargenBaseTipoConfiguracion : IEntityTypeConfiguration<MargenBaseTipo>
{
    public void Configure(EntityTypeBuilder<MargenBaseTipo> builder)
    {
        builder.ToTable("MargenesBaseTipo", tabla =>
        {
            // RF-27: el concepto Margen no puede usarse como base de otro margen.
            tabla.HasCheckConstraint(
                "CK_MargenesBaseTipo_NoIncluyeMargen",
                $"[TipoConceptoId] <> {TipoConcepto.MargenId}");
        });

        // La clave compuesta impide repetir el mismo tipo en la base del margen.
        builder.HasKey(m => new { m.ConceptoPresupuestoId, m.TipoConceptoId });

        // Las filas de la base pertenecen al concepto Margen: si algún día se
        // elimina esa fila, sus selecciones se van con ella. Los tipos fijos del
        // sistema, en cambio, nunca se borran si están en uso.
        builder.HasOne<ConceptoPresupuesto>()
            .WithMany()
            .HasForeignKey(m => m.ConceptoPresupuestoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TipoConcepto>()
            .WithMany()
            .HasForeignKey(m => m.TipoConceptoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
