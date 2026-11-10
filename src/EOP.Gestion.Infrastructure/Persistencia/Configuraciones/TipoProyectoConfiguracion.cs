using EOP.Gestion.Core.Entidades.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class TipoProyectoConfiguracion : IEntityTypeConfiguration<TipoProyecto>
{
    public void Configure(EntityTypeBuilder<TipoProyecto> builder)
    {
        builder.ToTable("TiposProyecto", tabla =>
        {
            // IsRequired solo prohíbe NULL: un nombre con solo espacios entraría igual.
            tabla.HasCheckConstraint("CK_TiposProyecto_NombreNoVacio", "TRIM([Nombre]) <> ''");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        // No puede haber dos tipos de proyecto con el mismo nombre.
        builder.HasIndex(t => t.Nombre)
            .IsUnique();
    }
}
