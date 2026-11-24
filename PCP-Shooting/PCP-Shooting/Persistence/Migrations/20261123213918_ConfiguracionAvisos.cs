using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracionAvisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionAvisos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotifyEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    WaOrderReady = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    WaReminder = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionAvisos", x => x.Id);
                    table.CheckConstraint("CK_Avisos_Correo", "\"NotifyEmail\" IS NULL OR length(trim(\"NotifyEmail\")) > 0");
                    table.CheckConstraint("CK_Avisos_Unica", "\"Id\" = 'e4ec8df0-0000-4000-8000-000000000010'::uuid");
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionAvisos",
                columns: new[] { "Id", "ActualizadoEnUtc", "NotifyEmail", "Version", "WaOrderReady", "WaReminder" },
                values: new object[] { new Guid("e4ec8df0-0000-4000-8000-000000000010"), new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("e4ec8df0-0000-4000-8000-000000000011"), "", "" });
            migrationBuilder.Sql("""
                ALTER TABLE "ConfiguracionAvisos" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "ConfiguracionAvisos" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionAvisos");
        }
    }
}
