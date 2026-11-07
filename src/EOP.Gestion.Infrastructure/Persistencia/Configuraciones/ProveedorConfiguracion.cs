using EOP.Gestion.Core.Entidades.Proveedores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class ProveedorConfiguracion : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores", tabla =>
        {
            // IsRequired solo prohíbe NULL, y en C# estos campos arrancan como texto vacío.
            // Sin estas restricciones, un Proveedor sin nombre o sin teléfono entraría igual.
            tabla.HasCheckConstraint(
                "CK_Proveedores_NombreNoVacio",
                "TRIM([Nombre]) <> '' AND TRIM([NombreNormalizado]) <> ''");

            tabla.HasCheckConstraint(
                "CK_Proveedores_TelefonoNoVacio",
                "TRIM([Telefono]) <> ''");

            // El índice único del RUT solo sirve de verdad si todos los RUT están escritos
            // igual: si entrara "21.552.175-0017" no chocaría con "215521750017". Por eso la
            // base exige lo que pide RF-23: 12 caracteres y todos dígitos. El dígito
            // verificador no se controla acá, eso lo valida ValidadorRut.
            tabla.HasCheckConstraint(
                "CK_Proveedores_RutFormato",
                RestriccionesRut.CrearCondicionRutDeDoceDigitos());

            // Los IS NOT NULL no están de adorno: comparar NULL da "desconocido", y una
            // restricción CHECK deja pasar lo desconocido. Sin ellos, un Proveedor con los
            // dos correos en NULL se guardaría igual.
            tabla.HasCheckConstraint(
                "CK_Proveedores_AlMenosUnCorreo",
                "([CorreoContacto] IS NOT NULL AND TRIM([CorreoContacto]) <> '') OR ([CorreoFacturacion] IS NOT NULL AND TRIM([CorreoFacturacion]) <> '')");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.NombreNormalizado)
            .IsRequired()
            .HasMaxLength(200);

        // El índice único va sobre la columna normalizada y no sobre Nombre, que dejaría
        // pasar "Fivisa  SA" y "Fivisa SA" como si fueran proveedores distintos.
        // Además, es lo que frena a dos usuarios que registran el mismo nombre a la vez: los
        // dos pasarían la validación de la aplicación, pero solo uno lograría guardar.
        builder.HasIndex(p => p.NombreNormalizado)
            .IsUnique();

        builder.Property(p => p.Rut)
            .IsRequired()
            .HasMaxLength(12);

        builder.HasIndex(p => p.Rut)
            .IsUnique();

        // Se guarda como texto en formato internacional. Como máximo E.164 son el "+" y 15 dígitos.
        builder.Property(p => p.Telefono)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(p => p.CorreoContacto)
            .HasMaxLength(254)
            .IsRequired(false);

        builder.Property(p => p.CorreoFacturacion)
            .HasMaxLength(254)
            .IsRequired(false);

        // La relación uno a muchos: un Proveedor tiene muchas cuentas, y cada cuenta apunta a
        // su Proveedor mediante ProveedorId. WithOne() va vacío porque CuentaBancaria no
        // necesita una propiedad que lleve al Proveedor: alcanza con guardar su Id.
        //
        // Restrict hace que la base no deje borrar un Proveedor que tiene cuentas. Con
        // Cascade se borrarían las cuentas sin avisar, y RF-102 y RF-109 no permiten eliminarlas.
        builder.HasMany(p => p.CuentasBancarias)
            .WithOne()
            .HasForeignKey(c => c.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        // "Todo Proveedor debe tener al menos una cuenta" no se puede exigir desde acá: una
        // restricción CHECK solo mira su propia fila, y las cuentas están en otra tabla. Esa
        // regla la tienen que controlar las validaciones de S2-T11, y S2-T13 guardar el
        // Proveedor junto con sus cuentas en una sola operación.
    }
}
