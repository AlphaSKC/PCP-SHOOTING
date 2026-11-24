using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlimentosPlayerasYNotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Categoria",
                table: "Productos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Productos",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotasMarkdown",
                table: "Productos",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Categoria_Activo",
                table: "Productos",
                columns: new[] { "Categoria", "Activo" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos",
                sql: "\"Categoria\" IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_Categoria_Activo",
                table: "Productos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "NotasMarkdown",
                table: "Productos");
        }
    }
}
