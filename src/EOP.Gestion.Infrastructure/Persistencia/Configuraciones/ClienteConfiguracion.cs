using EOP.Gestion.Core.Entidades.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

public class ClienteConfiguracion : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {

        builder.ToTable("Clientes", tabla => //le indicamos el nombre de la tabla en la base de datos
        {
            // S1-T27: hasta acá la base confiaba en que la aplicación siempre mandara datos
            // correctos. Estas restricciones son la segunda línea de defensa: si algún día otro
            // camino guarda un Cliente (por ejemplo, la modificación de US-054) y se olvida de
            // validar, la base lo frena igual. Son las mismas reglas que ya tiene Proveedor.

            // IsRequired solo prohíbe NULL, y en C# estos textos arrancan vacíos. Sin estas
            // dos, un Cliente con el nombre o el WhatsApp en blanco entraría igual.
            tabla.HasCheckConstraint(
                "CK_Clientes_NombreNoVacio",
                "TRIM([Nombre]) <> ''");

            tabla.HasCheckConstraint(
                "CK_Clientes_WhatsAppNoVacio",
                "TRIM([NumeroWhatsApp]) <> ''");

            // RF-89: el RUT se guarda solo con dígitos. El índice único de más abajo solo
            // sirve si todos los RUT están escritos igual: "21552175-001" no chocaría con
            // "215521750017". El dígito verificador lo sigue controlando ValidadorRut.
            tabla.HasCheckConstraint(
                "CK_Clientes_RutFormato",
                RestriccionesRut.CrearCondicionRutDeDoceDigitos());

            // RF-89: al menos uno de los dos correos. Los IS NOT NULL hacen falta porque
            // comparar NULL da "desconocido", y un CHECK deja pasar lo desconocido.
            tabla.HasCheckConstraint(
                "CK_Clientes_AlMenosUnCorreo",
                "([CorreoContacto] IS NOT NULL AND TRIM([CorreoContacto]) <> '') OR ([CorreoFacturacion] IS NOT NULL AND TRIM([CorreoFacturacion]) <> '')");
        });
        builder.HasKey(c => c.Id); //le indicamos que la propiedad Id es la clave primaria

        // El largo de 200 es el mismo que el de Proveedor.Nombre (decisión del equipo en S1-T27).
        // Antes era nvarchar(max): sin límite, y una columna así no se puede indexar.
        builder.Property(c => c.Nombre).IsRequired().HasMaxLength(200); //le indicamos que la propiedad Nombre es obligatoria
        builder.Property(c => c.Rut).IsRequired().HasMaxLength(12);
        builder.HasIndex(c=>c.Rut).IsUnique(); 
        builder.Property(c=>c.CorreoContacto).HasMaxLength(254).IsRequired(false); //le indicamos que la propiedad CorreoContacto es opcional
        builder.Property(c=>c.CorreoFacturacion).HasMaxLength(254).IsRequired(false);
        builder.Property(c => c.NumeroWhatsApp).IsRequired().HasMaxLength(16);
        builder.Property(c => c.Activo).IsRequired();



    }
}