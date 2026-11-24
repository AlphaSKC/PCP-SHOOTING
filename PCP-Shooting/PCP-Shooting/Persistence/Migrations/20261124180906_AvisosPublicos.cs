using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AvisosPublicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvisosPublicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    InicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisosPublicos", x => x.Id);
                    table.CheckConstraint("CK_AvisoPublico_Orden", "\"Orden\" >= 0");
                    table.CheckConstraint("CK_AvisoPublico_Texto", "length(trim(\"Texto\")) BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_AvisoPublico_Vigencia", "\"InicioUtc\" IS NULL OR \"FinUtc\" IS NULL OR \"FinUtc\" > \"InicioUtc\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosPublicos_Activo_Orden_Id",
                table: "AvisosPublicos",
                columns: new[] { "Activo", "Orden", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosPublicos_FinUtc",
                table: "AvisosPublicos",
                column: "FinUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AvisosPublicos_InicioUtc",
                table: "AvisosPublicos",
                column: "InicioUtc");
            migrationBuilder.Sql("""
                ALTER TABLE "AvisosPublicos" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "AvisosPublicos" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvisosPublicos");
        }
    }
}
