using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EOP.Gestion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class S1_T27_RestriccionesClientesYUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Al generarla, EF avisa que esto "puede perder datos" porque achica la columna de
            // nvarchar(max) a 200. En la práctica no se pierde nada en silencio: si hubiera un
            // nombre más largo, SQL Server cancelaría la migración con un error de truncado.
            // Lo mismo pasa con los CHECK: si alguna fila no los cumple, la migración no se
            // aplica. Antes de crearla comprobamos que EopGestionDev no tuviera filas así.
            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Clientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_CorreoCorporativoNoVacio",
                table: "Usuarios",
                sql: "TRIM([CorreoCorporativo]) <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_NombreCompletoNoVacio",
                table: "Usuarios",
                sql: "TRIM([NombreCompleto]) <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Clientes_AlMenosUnCorreo",
                table: "Clientes",
                sql: "([CorreoContacto] IS NOT NULL AND TRIM([CorreoContacto]) <> '') OR ([CorreoFacturacion] IS NOT NULL AND TRIM([CorreoFacturacion]) <> '')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Clientes_NombreNoVacio",
                table: "Clientes",
                sql: "TRIM([Nombre]) <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Clientes_RutFormato",
                table: "Clientes",
                sql: "[Rut] LIKE '____________' AND UNICODE(SUBSTRING([Rut], 1, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 2, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 3, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 4, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 5, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 6, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 7, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 8, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 9, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 10, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 11, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 12, 1)) BETWEEN 48 AND 57");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Clientes_WhatsAppNoVacio",
                table: "Clientes",
                sql: "TRIM([NumeroWhatsApp]) <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_CorreoCorporativoNoVacio",
                table: "Usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_NombreCompletoNoVacio",
                table: "Usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Clientes_AlMenosUnCorreo",
                table: "Clientes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Clientes_NombreNoVacio",
                table: "Clientes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Clientes_RutFormato",
                table: "Clientes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Clientes_WhatsAppNoVacio",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
