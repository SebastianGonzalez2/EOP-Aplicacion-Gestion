
using EOP.Gestion.Core.Entidades.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class MaterialConfiguracion : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        // Definimos el nombre de la tabla y restringimos
        // las unidades a las 14 opciones permitidas.
        builder.ToTable("Materiales", tabla =>
        {
            // Solo permitimos las unidades definidas
            // en el enum UnidadMedida.
            tabla.HasCheckConstraint(
                "CK_Materiales_UnidadValida",
                "[Unidad] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)");

            // No permitimos nombres vacíos ni compuestos
            // solamente por espacios.
            tabla.HasCheckConstraint(
                "CK_Materiales_NombreNoVacio",
                "TRIM([Nombre]) <> '' AND TRIM([NombreNormalizado]) <> ''");
        });

        // El Id identifica de forma única cada material.
        builder.HasKey(m => m.Id);

        // Todos los materiales deben tener un nombre.
        //
        // S1-T27: el largo de 200 es el mismo que el de Proveedor y Cliente. Antes era
        // nvarchar(max) mientras NombreNormalizado admitía 450, así que un nombre muy largo
        // pasaba la validación y SQL Server lo rechazaba con un error de truncado. Como la
        // normalización nunca alarga el texto, con 200 acá NombreNormalizado siempre entra.
        builder.Property(m => m.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        // Guardamos el nombre normalizado para detectar duplicados.
        builder.Property(m => m.NombreNormalizado)
            .IsRequired()
            .HasMaxLength(450);

        // No permitimos dos materiales con el mismo nombre normalizado.
        builder.HasIndex(m => m.NombreNormalizado)
            .IsUnique();

        // Guardamos la unidad como número entero.
        // Solo se permiten los valores definidos en UnidadMedida.
        builder.Property(m => m.Unidad)
            .HasConversion<int>()
            .IsRequired();
    }
}
