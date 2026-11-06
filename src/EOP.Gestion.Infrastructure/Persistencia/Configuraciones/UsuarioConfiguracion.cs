
using EOP.Gestion.Core.Entidades.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // Nombre de la tabla en SQL Server.
        // Se restringen los roles a los tres definidos por EOP.
        builder.ToTable("Usuarios", tabla =>
        {
            tabla.HasCheckConstraint(
                "CK_Usuarios_RolValido", /*Esto impide guardar roles fuera de los 3 permitidos*/
                "[Rol] IN (1, 2, 3)");

            // S1-T27: IsRequired solo prohíbe NULL, así que un texto vacío entraba igual. Un
            // correo vacío es peor de lo que parece: el índice único dejaría guardar uno solo,
            // y nunca coincidiría con ninguna identidad de Entra ID en el primer acceso.
            tabla.HasCheckConstraint(
                "CK_Usuarios_NombreCompletoNoVacio",
                "TRIM([NombreCompleto]) <> ''");

            tabla.HasCheckConstraint(
                "CK_Usuarios_CorreoCorporativoNoVacio",
                "TRIM([CorreoCorporativo]) <> ''");
        });

        // Clave primaria: identifica de forma única cada usuario.
        builder.HasKey(u => u.Id);

        // El nombre completo es obligatorio.
        builder.Property(u => u.NombreCompleto)
            .IsRequired()
            .HasMaxLength(150);

        // El correo corporativo es obligatorio.
        builder.Property(u => u.CorreoCorporativo)
            .IsRequired()
            .HasMaxLength(254);

        // No se permiten dos usuarios con el mismo correo.
        builder.HasIndex(u => u.CorreoCorporativo)
            .IsUnique();

        // El rol se almacena como un número entero.
        builder.Property(u => u.Rol)
            .HasConversion<int>()
            .IsRequired();

        // Indica si el usuario está habilitado.
        builder.Property(u => u.Activo)
            .IsRequired();

        // Identificador externo de Microsoft Entra ID.
        // Puede ser null antes del primer inicio de sesión.
        builder.Property(u => u.EntraObjectId)
            .HasMaxLength(36)
            .IsRequired(false);

        // Dos usuarios no pueden tener el mismo identificador Entra.
        // Se permiten múltiples valores null antes de vincularlos.
        builder.HasIndex(u => u.EntraObjectId)
            .IsUnique()
            .HasFilter("[EntraObjectId] IS NOT NULL"); /*Aplicá el índice únicamente a los registros cuyo EntraObjectId NO sea NULL.*/
    }
}
