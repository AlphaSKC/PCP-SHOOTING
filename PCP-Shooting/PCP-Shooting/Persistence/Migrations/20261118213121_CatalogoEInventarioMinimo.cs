using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoEInventarioMinimo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Productos",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "StockMinimo",
                table: "Productos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Activo_Nombre",
                table: "Productos",
                columns: new[] { "Activo", "Nombre" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_StockMinimo",
                table: "Productos",
                sql: "\"StockMinimo\" IS NULL OR \"StockMinimo\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_Activo_Nombre",
                table: "Productos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_StockMinimo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "Productos");
        }
    }
}
