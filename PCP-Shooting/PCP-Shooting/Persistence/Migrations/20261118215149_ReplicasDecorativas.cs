using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplicasDecorativas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos",
                sql: "\"Categoria\" IN (0, 1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos",
                sql: "\"Categoria\" IN (0, 1, 2)");
        }
    }
}
