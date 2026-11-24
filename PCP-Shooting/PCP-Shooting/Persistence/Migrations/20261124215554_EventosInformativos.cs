using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EventosInformativos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Resumen = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    DescripcionMarkdown = table.Column<string>(type: "character varying(30000)", maxLength: 30000, nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ImagenUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    InicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                    table.CheckConstraint("CK_Evento_Estado", "\"Estado\" IN (0, 1, 2)");
                    table.CheckConstraint("CK_Evento_Fechas", "\"FinUtc\" > \"InicioUtc\"");
                    table.CheckConstraint("CK_Evento_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("CK_Evento_Textos", "length(trim(\"Titulo\")) > 0 AND length(trim(\"Resumen\")) > 0 AND length(trim(\"DescripcionMarkdown\")) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_Estado_InicioUtc_Id",
                table: "Eventos",
                columns: new[] { "Estado", "InicioUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_FinUtc",
                table: "Eventos",
                column: "FinUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_Slug",
                table: "Eventos",
                column: "Slug",
                unique: true);
            migrationBuilder.Sql("""
                ALTER TABLE "Eventos" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "Eventos" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Eventos");
        }
    }
}
