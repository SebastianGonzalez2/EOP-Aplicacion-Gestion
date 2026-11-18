using EOP.Gestion.Core.Entidades.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class TipoConceptoConfiguracion : IEntityTypeConfiguration<TipoConcepto>
{
    public void Configure(EntityTypeBuilder<TipoConcepto> builder)
    {
        builder.ToTable("TiposConcepto", tabla =>
        {
            // IsRequired solo prohíbe NULL: un nombre con solo espacios entraría igual.
            tabla.HasCheckConstraint("CK_TiposConcepto_NombreNoVacio", "TRIM([Nombre]) <> ''");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Nombre)
            .IsUnique();

        // Los seis tipos definidos en RF-16 y RF-27. Llevan Id fijo porque así
        // funciona HasData y porque las reglas de negocio los identifican por Id.
        // La base sigue numerando desde el 7 por si en el futuro EOP agrega otro.
        builder.HasData(
            new TipoConcepto { Id = TipoConcepto.MaterialId, Nombre = "Material" },
            new TipoConcepto { Id = TipoConcepto.ManoDeObraId, Nombre = "Mano de obra" },
            new TipoConcepto { Id = TipoConcepto.AlquilerId, Nombre = "Alquiler" },
            new TipoConcepto { Id = TipoConcepto.TrasladoId, Nombre = "Traslado" },
            new TipoConcepto { Id = TipoConcepto.CostosIndirectosId, Nombre = "Costos indirectos" },
            new TipoConcepto { Id = TipoConcepto.MargenId, Nombre = "Margen" });
    }
}
