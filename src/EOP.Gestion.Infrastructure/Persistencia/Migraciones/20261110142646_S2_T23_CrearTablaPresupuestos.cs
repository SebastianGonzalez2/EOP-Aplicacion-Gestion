using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EOP.Gestion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class S2_T23_CrearTablaPresupuestos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "SecuenciaPresupuestos",
                minValue: 1L,
                maxValue: 9999L);

            migrationBuilder.CreateTable(
                name: "TiposProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposProyecto", x => x.Id);
                    table.CheckConstraint("CK_TiposProyecto_NombreNoVacio", "TRIM([Nombre]) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "Presupuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NumeroCorrelativo = table.Column<int>(type: "int", nullable: false),
                    NombreProyecto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    TipoProyectoId = table.Column<int>(type: "int", nullable: false),
                    PlazoVigenciaDias = table.Column<int>(type: "int", nullable: false),
                    ResponsableId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presupuestos", x => x.Id);
                    table.CheckConstraint("CK_Presupuestos_CodigoNoVacio", "TRIM([Codigo]) <> ''");
                    table.CheckConstraint("CK_Presupuestos_CorrelativoDeCuatroCifras", "[NumeroCorrelativo] BETWEEN 1 AND 9999");
                    table.CheckConstraint("CK_Presupuestos_EstadoValido", "[Estado] IN (1, 2, 3, 4, 5, 6, 7, 8)");
                    table.CheckConstraint("CK_Presupuestos_NombreProyectoNoVacio", "TRIM([NombreProyecto]) <> ''");
                    table.CheckConstraint("CK_Presupuestos_PlazoVigenciaPositivo", "[PlazoVigenciaDias] > 0");
                    table.ForeignKey(
                        name: "FK_Presupuestos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Presupuestos_TiposProyecto_TipoProyectoId",
                        column: x => x.TipoProyectoId,
                        principalTable: "TiposProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Presupuestos_Usuarios_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_ClienteId",
                table: "Presupuestos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_Codigo",
                table: "Presupuestos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_NumeroCorrelativo",
                table: "Presupuestos",
                column: "NumeroCorrelativo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_ResponsableId",
                table: "Presupuestos",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_TipoProyectoId",
                table: "Presupuestos",
                column: "TipoProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposProyecto_Nombre",
                table: "TiposProyecto",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Presupuestos");

            migrationBuilder.DropTable(
                name: "TiposProyecto");

            migrationBuilder.DropSequence(
                name: "SecuenciaPresupuestos");
        }
    }
}
