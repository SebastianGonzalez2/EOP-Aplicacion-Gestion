using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EOP.Gestion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class S2_T10_CrearTablasProveedoresYCuentasBancarias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombreNormalizado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rut = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CorreoContacto = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    CorreoFacturacion = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                    table.CheckConstraint("CK_Proveedores_AlMenosUnCorreo", "([CorreoContacto] IS NOT NULL AND TRIM([CorreoContacto]) <> '') OR ([CorreoFacturacion] IS NOT NULL AND TRIM([CorreoFacturacion]) <> '')");
                    table.CheckConstraint("CK_Proveedores_NombreNoVacio", "TRIM([Nombre]) <> '' AND TRIM([NombreNormalizado]) <> ''");
                    table.CheckConstraint("CK_Proveedores_RutFormato", "[Rut] LIKE '____________' AND UNICODE(SUBSTRING([Rut], 1, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 2, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 3, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 4, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 5, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 6, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 7, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 8, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 9, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 10, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 11, 1)) BETWEEN 48 AND 57 AND UNICODE(SUBSTRING([Rut], 12, 1)) BETWEEN 48 AND 57");
                    table.CheckConstraint("CK_Proveedores_TelefonoNoVacio", "TRIM([Telefono]) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "CuentasBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Banco = table.Column<int>(type: "int", nullable: false),
                    NumeroCuenta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Titular = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Moneda = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasBancarias", x => x.Id);
                    table.CheckConstraint("CK_CuentasBancarias_BancoValido", "[Banco] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)");
                    table.CheckConstraint("CK_CuentasBancarias_MonedaValida", "[Moneda] IN (1, 2)");
                    table.CheckConstraint("CK_CuentasBancarias_NumeroCuentaNoVacio", "TRIM([NumeroCuenta]) <> ''");
                    table.CheckConstraint("CK_CuentasBancarias_TitularNoVacio", "TRIM([Titular]) <> ''");
                    table.ForeignKey(
                        name: "FK_CuentasBancarias_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuentasBancarias_ProveedorId",
                table: "CuentasBancarias",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NombreNormalizado",
                table: "Proveedores",
                column: "NombreNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Rut",
                table: "Proveedores",
                column: "Rut",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuentasBancarias");

            migrationBuilder.DropTable(
                name: "Proveedores");
        }
    }
}
