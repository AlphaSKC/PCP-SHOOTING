using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BlogArticulos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticulosBlog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Resumen = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    ContenidoMarkdown = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false),
                    Categoria = table.Column<int>(type: "integer", nullable: false),
                    ImagenUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Destacado = table.Column<bool>(type: "boolean", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    PublicarEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticulosBlog", x => x.Id);
                    table.CheckConstraint("CK_Blog_Categoria", "\"Categoria\" IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_Blog_Estado", "\"Estado\" IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_Blog_FechaPublicacion", "\"Estado\" NOT IN (1, 2) OR \"PublicarEnUtc\" IS NOT NULL");
                    table.CheckConstraint("CK_Blog_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("CK_Blog_Textos", "length(trim(\"Titulo\")) > 0 AND length(trim(\"Resumen\")) > 0 AND length(trim(\"ContenidoMarkdown\")) > 0");
                    table.ForeignKey(
                        name: "FK_ArticulosBlog_PerfilesUsuario_AutorId",
                        column: x => x.AutorId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosBlog_AutorId",
                table: "ArticulosBlog",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosBlog_Categoria_PublicarEnUtc",
                table: "ArticulosBlog",
                columns: new[] { "Categoria", "PublicarEnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosBlog_Estado_PublicarEnUtc",
                table: "ArticulosBlog",
                columns: new[] { "Estado", "PublicarEnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosBlog_Slug",
                table: "ArticulosBlog",
                column: "Slug",
                unique: true);
            // El navegador consume el blog por la API; los borradores nunca se exponen por PostgREST.
            migrationBuilder.Sql("""
                ALTER TABLE "ArticulosBlog" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "ArticulosBlog" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticulosBlog");
        }
    }
}
