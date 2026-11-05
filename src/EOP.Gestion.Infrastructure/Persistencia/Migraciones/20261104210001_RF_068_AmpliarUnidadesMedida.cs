using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EOP.Gestion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class RF_068_AmpliarUnidadesMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Materiales_UnidadValida",
                table: "Materiales");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Materiales_UnidadValida",
                table: "Materiales",
                sql: "[Unidad] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Materiales_UnidadValida",
                table: "Materiales");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Materiales_UnidadValida",
                table: "Materiales",
                sql: "[Unidad] IN (1, 2, 3, 4, 5)");
        }
    }
}
