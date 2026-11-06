using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EOP.Gestion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class S2_T16_RegistrarMaterialProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreNormalizado",
                table: "Materiales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");


            // Los materiales guardados antes de la T16 no tenían NombreNormalizado.
            // Completamos el nuevo campo utilizando el nombre que ya tenían,
            // antes de crear el índice único y la restricción de nombres vacíos.
            //
            // De esta manera conservamos los materiales existentes.
            // Si hay nombres que terminan siendo duplicados, el índice único
            // impedirá aplicar la migración sin que se borren esos materiales.
            migrationBuilder.Sql("""
                UPDATE [Materiales]
                SET [NombreNormalizado] = UPPER(
                    TRIM(
                        REPLACE(
                            REPLACE(
                                REPLACE([Nombre], CHAR(9), ' '),
                                CHAR(10), ' '),
                            CHAR(13), ' ')
                    )
                );

                WHILE EXISTS (
                    SELECT 1
                    FROM [Materiales]
                    WHERE CHARINDEX('  ', [NombreNormalizado]) > 0
                )
                BEGIN
                    UPDATE [Materiales]
                    SET [NombreNormalizado] =
                        REPLACE([NombreNormalizado], '  ', ' ')
                    WHERE CHARINDEX('  ', [NombreNormalizado]) > 0;
                END;
                """);


            migrationBuilder.CreateTable(
                name: "MaterialProveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: false),
                    Moneda = table.Column<int>(type: "int", nullable: false),
                    FechaUltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialProveedores", x => x.Id);
                    table.CheckConstraint("CK_MaterialProveedores_MonedaValida", "[Moneda] IN (1, 2)");
                    table.CheckConstraint("CK_MaterialProveedores_PrecioPositivo", "[PrecioUnitario] > 0");
                    table.ForeignKey(
                        name: "FK_MaterialProveedores_Materiales_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materiales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialProveedores_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Materiales_NombreNormalizado",
                table: "Materiales",
                column: "NombreNormalizado",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Materiales_NombreNoVacio",
                table: "Materiales",
                sql: "TRIM([Nombre]) <> '' AND TRIM([NombreNormalizado]) <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialProveedores_MaterialId_ProveedorId",
                table: "MaterialProveedores",
                columns: new[] { "MaterialId", "ProveedorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialProveedores_ProveedorId",
                table: "MaterialProveedores",
                column: "ProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialProveedores");

            migrationBuilder.DropIndex(
                name: "IX_Materiales_NombreNormalizado",
                table: "Materiales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Materiales_NombreNoVacio",
                table: "Materiales");

            migrationBuilder.DropColumn(
                name: "NombreNormalizado",
                table: "Materiales");
        }
    }
}
