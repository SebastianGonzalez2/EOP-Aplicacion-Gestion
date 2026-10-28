using EOP.Gestion.Core.Entidades.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class ClienteConfiguracion : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {

        builder.ToTable("Clientes"); //le indicamos el nombre de la tabla en la base de datos
        builder.HasKey(c => c.Id); //le indicamos que la propiedad Id es la clave primaria
        builder.Property(c => c.Nombre).IsRequired(); //le indicamos que la propiedad Nombre es obligatoria
        builder.Property(c => c.Rut).IsRequired().HasMaxLength(12);
        builder.HasIndex(c=>c.Rut).IsUnique(); 
        builder.Property(c=>c.CorreoContacto).HasMaxLength(254).IsRequired(false); //le indicamos que la propiedad CorreoContacto es opcional
        builder.Property(c=>c.CorreoFacturacion).HasMaxLength(254).IsRequired(false);
        builder.Property(c => c.NumeroWhatsApp).IsRequired().HasMaxLength(16);
        builder.Property(c => c.Activo).IsRequired();



    }
}