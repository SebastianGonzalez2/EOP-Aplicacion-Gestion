
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Proveedores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia
    .Configuraciones;

public class MaterialProveedorConfiguracion: IEntityTypeConfiguration<MaterialProveedor>
{
    public void Configure(EntityTypeBuilder<MaterialProveedor> builder)
    {
        // Nombre de la tabla y reglas que SQL Server
        // debe comprobar antes de guardar.
        builder.ToTable("MaterialProveedores", tabla =>
        {
            tabla.HasCheckConstraint("CK_MaterialProveedores_PrecioPositivo","[PrecioUnitario] > 0");

            tabla.HasCheckConstraint("CK_MaterialProveedores_MonedaValida","[Moneda] IN (1, 2)");
        });

        // Clave primaria de la relación.
        builder.HasKey(mp => mp.Id);

        // Cada relación debe pertenecer a un material.
        builder.HasOne<Material>().WithMany().HasForeignKey(mp => mp.MaterialId).OnDelete(DeleteBehavior.Restrict);

        // Cada relación debe pertenecer a un proveedor.
        builder.HasOne<Proveedor>().WithMany().HasForeignKey(mp => mp.ProveedorId).OnDelete(DeleteBehavior.Restrict);

        // Un mismo material no puede tener dos relaciones
        // con el mismo proveedor.
        builder.HasIndex(mp => new{mp.MaterialId,mp.ProveedorId}).IsUnique();

        // El precio debe estar informado.
        builder.Property(mp => mp.PrecioUnitario).IsRequired();

        // Guardamos la moneda como número entero.
        builder.Property(mp => mp.Moneda).HasConversion<int>().IsRequired();

        // La fecha se asignará automáticamente
        // desde el caso de uso al registrar el precio.
        builder.Property(mp => mp.FechaUltimaActualizacion).IsRequired();
    }
}
