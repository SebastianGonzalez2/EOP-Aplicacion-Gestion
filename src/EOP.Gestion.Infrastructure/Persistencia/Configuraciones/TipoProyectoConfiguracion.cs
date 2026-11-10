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

        // OJO: estos dos tipos son PROVISORIOS, para poder crear presupuestos y hacer pruebas.
        // La lista definitiva sigue PENDIENTE DE VALIDAR CON EOP (RF-01). Cuando EOP la confirme,
        // se reemplazan con una migración nueva.
        // Llevan Id fijo porque así funciona HasData; la base sigue numerando desde el 3.
        builder.HasData(
            new TipoProyecto { Id = 1, Nombre = "Obra" },
            new TipoProyecto { Id = 2, Nombre = "Eficiencia energética" });
    }
}
