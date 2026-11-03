
using EOP.Gestion.Core.Entidades.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class MaterialConfiguracion : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        // Definimos el nombre de la tabla y restringimos
        // las unidades a las cinco opciones permitidas.
        builder.ToTable("Materiales", tabla =>tabla.HasCheckConstraint("CK_Materiales_UnidadValida","[Unidad] IN (1, 2, 3, 4, 5)"));

        // El Id identifica de forma única cada material.
        builder.HasKey(m => m.Id);

        // Todos los materiales deben tener un nombre.
        builder.Property(m => m.Nombre).IsRequired();

        // Guardamos la unidad como número entero.
        // Solo se permiten los valores definidos en UnidadMedida.
        builder.Property(m => m.Unidad).HasConversion<int>().IsRequired();
    }
}
