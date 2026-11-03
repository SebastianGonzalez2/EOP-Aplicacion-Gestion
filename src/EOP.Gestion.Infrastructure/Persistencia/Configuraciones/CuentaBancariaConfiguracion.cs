using EOP.Gestion.Core.Entidades.Proveedores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class CuentaBancariaConfiguracion : IEntityTypeConfiguration<CuentaBancaria>
{
    public void Configure(EntityTypeBuilder<CuentaBancaria> builder)
    {
        // RF-23: no se puede guardar una cuenta incompleta. IsRequired solo prohíbe NULL, y
        // en C# estos campos arrancan en 0 o en texto vacío, así que sin estas restricciones
        // una cuenta vacía entraría igual. Banco y Moneda además solo admiten las listas fijas.
        builder.ToTable("CuentasBancarias", tabla =>
        {
            tabla.HasCheckConstraint(
                "CK_CuentasBancarias_BancoValido",
                "[Banco] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)");

            tabla.HasCheckConstraint(
                "CK_CuentasBancarias_MonedaValida",
                "[Moneda] IN (1, 2)");

            tabla.HasCheckConstraint(
                "CK_CuentasBancarias_NumeroCuentaNoVacio",
                "TRIM([NumeroCuenta]) <> ''");

            tabla.HasCheckConstraint(
                "CK_CuentasBancarias_TitularNoVacio",
                "TRIM([Titular]) <> ''");
        });

        builder.HasKey(c => c.Id);

        // Banco y Moneda se guardan como número, igual que el Rol de Usuario.
        builder.Property(c => c.Banco)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.NumeroCuenta)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Titular)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Moneda)
            .HasConversion<int>()
            .IsRequired();
    }
}
