using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TallasPlayeras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsaTallas",
                table: "Productos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Talla",
                table: "MovimientosInventario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Talla",
                table: "DetallesPedido",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Tallas",
                table: "Productos",
                sql: "NOT \"UsaTallas\" OR \"Categoria\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_ProductoId_Talla_RegistradoEnUtc",
                table: "MovimientosInventario",
                columns: new[] { "ProductoId", "Talla", "RegistradoEnUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movimiento_Talla",
                table: "MovimientosInventario",
                sql: "\"Talla\" IS NULL OR \"Talla\" IN (0,1,2,3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Detalle_Talla",
                table: "DetallesPedido",
                sql: "\"Talla\" IS NULL OR \"Talla\" IN (0,1,2,3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Tallas",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_ProductoId_Talla_RegistradoEnUtc",
                table: "MovimientosInventario");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movimiento_Talla",
                table: "MovimientosInventario");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Detalle_Talla",
                table: "DetallesPedido");

            migrationBuilder.DropColumn(
                name: "UsaTallas",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Talla",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "Talla",
                table: "DetallesPedido");
        }
    }
}
