using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaquetesYConsumibles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos");

            migrationBuilder.CreateTable(
                name: "Paquetes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IncluyeMarkdown = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    RequisitosMarkdown = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    PrecioPorPersona = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PrecioSocioCartaq = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paquetes", x => x.Id);
                    table.CheckConstraint("CK_Paquete_Activo", "NOT \"Activo\" OR (\"PrecioPorPersona\" IS NOT NULL AND \"PrecioPorPersona\" > 0 AND (\"PrecioSocioCartaq\" IS NULL OR \"PrecioSocioCartaq\" > 0))");
                    table.CheckConstraint("CK_Paquete_Codigo", "\"Codigo\" ~ '^[A-Z0-9]+(-[A-Z0-9]+)*$'");
                    table.CheckConstraint("CK_Paquete_Precios", "(\"PrecioPorPersona\" IS NULL OR \"PrecioPorPersona\" >= 0) AND (\"PrecioSocioCartaq\" IS NULL OR \"PrecioSocioCartaq\" >= 0)");
                    table.CheckConstraint("CK_Paquete_Textos", "length(trim(\"Nombre\")) > 0 AND length(trim(\"IncluyeMarkdown\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "ConsumiblesPaquete",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaqueteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CantidadPorPersona = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumiblesPaquete", x => x.Id);
                    table.CheckConstraint("CK_ConsumiblePaquete_Cantidad", "\"CantidadPorPersona\" BETWEEN 1 AND 100000");
                    table.ForeignKey(
                        name: "FK_ConsumiblesPaquete_Paquetes_PaqueteId",
                        column: x => x.PaqueteId,
                        principalTable: "Paquetes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumiblesPaquete_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos",
                sql: "\"Categoria\" IN (0, 1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Consumible",
                table: "Productos",
                sql: "\"Categoria\" <> 4 OR NOT \"EsVendible\"");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumiblesPaquete_PaqueteId_ProductoId",
                table: "ConsumiblesPaquete",
                columns: new[] { "PaqueteId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumiblesPaquete_ProductoId",
                table: "ConsumiblesPaquete",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Paquetes_Activo_Nombre",
                table: "Paquetes",
                columns: new[] { "Activo", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Paquetes_Codigo",
                table: "Paquetes",
                column: "Codigo",
                unique: true);
            migrationBuilder.Sql("""
                ALTER TABLE "Paquetes" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ConsumiblesPaquete" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "Paquetes", "ConsumiblesPaquete" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumiblesPaquete");

            migrationBuilder.DropTable(
                name: "Paquetes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Consumible",
                table: "Productos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Categoria",
                table: "Productos",
                sql: "\"Categoria\" IN (0, 1, 2, 3)");
        }
    }
}
