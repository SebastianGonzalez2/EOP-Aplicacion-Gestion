
using EOP.Gestion.Core.Entidades.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class MaterialConfiguracion : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {

        // Definimos el nombre de la tabla y las restricciones
        // que SQL Server debe comprobar antes de guardar.
        builder.ToTable("Materiales", tabla =>
        {
            // Solo permitimos las cinco unidades de medida.
            tabla.HasCheckConstraint(
                "CK_Materiales_UnidadValida",
                "[Unidad] IN (1, 2, 3, 4, 5)");

            // No permitimos nombres vacíos ni compuestos
            // solamente por espacios.
            tabla.HasCheckConstraint(
                "CK_Materiales_NombreNoVacio",
                "TRIM([Nombre]) <> '' AND TRIM([NombreNormalizado]) <> ''");
        });


        // El Id identifica de forma única cada material.
        builder.HasKey(m => m.Id);

        // Todos los materiales deben tener un nombre.
        builder.Property(m => m.Nombre).IsRequired();


        // Guardamos el nombre normalizado para detectar duplicados.
        builder.Property(m => m.NombreNormalizado).IsRequired().HasMaxLength(450);

        // No permitimos dos materiales con el mismo nombre normalizado.
        builder.HasIndex(m => m.NombreNormalizado).IsUnique();


        // Guardamos la unidad como número entero.
        // Solo se permiten los valores definidos en UnidadMedida.
        builder.Property(m => m.Unidad).HasConversion<int>().IsRequired();
    }
}
