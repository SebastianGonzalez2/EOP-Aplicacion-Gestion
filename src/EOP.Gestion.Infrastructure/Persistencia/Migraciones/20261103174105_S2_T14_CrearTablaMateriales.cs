using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EOP.Gestion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class S2_T14_CrearTablaMateriales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materiales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materiales", x => x.Id);
                    table.CheckConstraint("CK_Materiales_UnidadValida", "[Unidad] IN (1, 2, 3, 4, 5)");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Materiales");
        }
    }
}
