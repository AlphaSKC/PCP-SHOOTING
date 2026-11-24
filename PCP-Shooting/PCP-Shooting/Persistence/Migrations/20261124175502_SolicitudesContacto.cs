using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudesContacto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesContacto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Motivo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SolicitudHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrivacidadAceptadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    NotasInternas = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesContacto", x => x.Id);
                    table.CheckConstraint("CK_Contacto_Estado", "\"Estado\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_Contacto_Mensaje", "length(trim(\"Mensaje\")) BETWEEN 10 AND 2000");
                    table.CheckConstraint("CK_Contacto_Motivo", "\"Motivo\" IN ('reservacion','grupos','tienda','cotizacion','otro')");
                    table.CheckConstraint("CK_Contacto_Nombre", "length(trim(\"Nombre\")) BETWEEN 2 AND 150");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesContacto_CreadoEnUtc_Id",
                table: "SolicitudesContacto",
                columns: new[] { "CreadoEnUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesContacto_Estado_CreadoEnUtc_Id",
                table: "SolicitudesContacto",
                columns: new[] { "Estado", "CreadoEnUtc", "Id" });
            migrationBuilder.Sql("""
                ALTER TABLE "SolicitudesContacto" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "SolicitudesContacto" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesContacto");
        }
    }
}
