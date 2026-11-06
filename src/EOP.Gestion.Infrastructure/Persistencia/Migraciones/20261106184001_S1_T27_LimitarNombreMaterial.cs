using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EOP.Gestion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class S1_T27_LimitarNombreMaterial : Migration
    {
        // La misma condición que define MaterialConfiguracion. Va en una constante porque la
        // usamos dos veces (en Up y en Down) y tiene que quedar exactamente igual que antes.
        private const string CondicionNombreNoVacio = "TRIM([Nombre]) <> '' AND TRIM([NombreNormalizado]) <> ''";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF generó solamente el AlterColumn, pero SQL Server no deja cambiar el tipo de una
            // columna que usa una restricción CHECK ("The object 'CK_Materiales_NombreNoVacio'
            // is dependent on column 'Nombre'"). Por eso, a mano: sacamos el CHECK, achicamos la
            // columna y lo volvemos a crear igual. Lo detectó ScriptsDeMigracionSqlServerTests.
            //
            // Achicar de nvarchar(max) a 200 no recorta nada en silencio: si hubiera un nombre
            // más largo, SQL Server cancelaría la migración con un error de truncado.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Materiales_NombreNoVacio",
                table: "Materiales");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Materiales",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Materiales_NombreNoVacio",
                table: "Materiales",
                sql: CondicionNombreNoVacio);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // El mismo problema al revés: para volver a nvarchar(max) también hay que sacar el CHECK.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Materiales_NombreNoVacio",
                table: "Materiales");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Materiales",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Materiales_NombreNoVacio",
                table: "Materiales",
                sql: CondicionNombreNoVacio);
        }
    }
}
