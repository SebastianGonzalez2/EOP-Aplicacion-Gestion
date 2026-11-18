using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EOP.Gestion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class S3_T05_CrearConceptosPresupuesto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiposConcepto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposConcepto", x => x.Id);
                    table.CheckConstraint("CK_TiposConcepto_NombreNoVacio", "TRIM([Nombre]) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "ConceptosPresupuesto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PresupuestoId = table.Column<int>(type: "int", nullable: false),
                    TipoConceptoId = table.Column<int>(type: "int", nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConceptoDetectado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    MaterialProveedorId = table.Column<int>(type: "int", nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    Unidad = table.Column<int>(type: "int", nullable: true),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    Moneda = table.Column<int>(type: "int", nullable: true),
                    PorcentajeMargen = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptosPresupuesto", x => x.Id);
                    table.CheckConstraint("CK_ConceptosPresupuesto_CantidadPositiva", "[Cantidad] IS NULL OR [Cantidad] > 0");
                    table.CheckConstraint("CK_ConceptosPresupuesto_ConceptoNoVacio", "[Concepto] IS NULL OR TRIM([Concepto]) <> ''");
                    table.CheckConstraint("CK_ConceptosPresupuesto_MargenConPorcentaje", "([TipoConceptoId] = 6 AND [PorcentajeMargen] IS NOT NULL) OR ([TipoConceptoId] <> 6 AND [PorcentajeMargen] IS NULL)");
                    table.CheckConstraint("CK_ConceptosPresupuesto_MonedaValida", "[Moneda] IS NULL OR [Moneda] IN (1, 2)");
                    table.CheckConstraint("CK_ConceptosPresupuesto_PorcentajeNoNegativo", "[PorcentajeMargen] IS NULL OR [PorcentajeMargen] >= 0");
                    table.CheckConstraint("CK_ConceptosPresupuesto_PrecioYMonedaJuntos", "([PrecioUnitario] IS NULL AND [Moneda] IS NULL) OR ([PrecioUnitario] IS NOT NULL AND [Moneda] IS NOT NULL)");
                    table.CheckConstraint("CK_ConceptosPresupuesto_ReferenciaCompleta", "([MaterialId] IS NULL AND [MaterialProveedorId] IS NULL) OR ([MaterialId] IS NOT NULL AND [MaterialProveedorId] IS NOT NULL)");
                    table.CheckConstraint("CK_ConceptosPresupuesto_UnidadValida", "[Unidad] IS NULL OR [Unidad] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)");
                    table.ForeignKey(
                        name: "FK_ConceptosPresupuesto_MaterialProveedores_MaterialProveedorId",
                        column: x => x.MaterialProveedorId,
                        principalTable: "MaterialProveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConceptosPresupuesto_Materiales_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materiales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConceptosPresupuesto_Presupuestos_PresupuestoId",
                        column: x => x.PresupuestoId,
                        principalTable: "Presupuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConceptosPresupuesto_TiposConcepto_TipoConceptoId",
                        column: x => x.TipoConceptoId,
                        principalTable: "TiposConcepto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MargenesBaseTipo",
                columns: table => new
                {
                    ConceptoPresupuestoId = table.Column<int>(type: "int", nullable: false),
                    TipoConceptoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MargenesBaseTipo", x => new { x.ConceptoPresupuestoId, x.TipoConceptoId });
                    table.CheckConstraint("CK_MargenesBaseTipo_NoIncluyeMargen", "[TipoConceptoId] <> 6");
                    table.ForeignKey(
                        name: "FK_MargenesBaseTipo_ConceptosPresupuesto_ConceptoPresupuestoId",
                        column: x => x.ConceptoPresupuestoId,
                        principalTable: "ConceptosPresupuesto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MargenesBaseTipo_TiposConcepto_TipoConceptoId",
                        column: x => x.TipoConceptoId,
                        principalTable: "TiposConcepto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TiposConcepto",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Material" },
                    { 2, "Mano de obra" },
                    { 3, "Alquiler" },
                    { 4, "Traslado" },
                    { 5, "Costos indirectos" },
                    { 6, "Margen" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosPresupuesto_MaterialId",
                table: "ConceptosPresupuesto",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosPresupuesto_MaterialProveedorId",
                table: "ConceptosPresupuesto",
                column: "MaterialProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosPresupuesto_PresupuestoId",
                table: "ConceptosPresupuesto",
                column: "PresupuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosPresupuesto_PresupuestoId_MaterialProveedorId",
                table: "ConceptosPresupuesto",
                columns: new[] { "PresupuestoId", "MaterialProveedorId" },
                unique: true,
                filter: "[MaterialProveedorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosPresupuesto_TipoConceptoId",
                table: "ConceptosPresupuesto",
                column: "TipoConceptoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptosPresupuesto_UnicoMargen",
                table: "ConceptosPresupuesto",
                columns: new[] { "PresupuestoId", "TipoConceptoId" },
                unique: true,
                filter: "[TipoConceptoId] = 6");

            migrationBuilder.CreateIndex(
                name: "IX_MargenesBaseTipo_TipoConceptoId",
                table: "MargenesBaseTipo",
                column: "TipoConceptoId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposConcepto_Nombre",
                table: "TiposConcepto",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MargenesBaseTipo");

            migrationBuilder.DropTable(
                name: "ConceptosPresupuesto");

            migrationBuilder.DropTable(
                name: "TiposConcepto");
        }
    }
}
