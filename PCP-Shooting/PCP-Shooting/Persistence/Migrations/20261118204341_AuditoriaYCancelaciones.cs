using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaYCancelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Pago_Estado",
                table: "Pagos");

            migrationBuilder.CreateTable(
                name: "EventosAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Entidad = table.Column<string>(type: "text", nullable: false),
                    EntidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Accion = table.Column<string>(type: "text", nullable: false),
                    Campos = table.Column<string>(type: "text", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SolicitudId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAuditoria", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pago_Estado",
                table: "Pagos",
                sql: "\"Estado\" IN (0, 1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAuditoria_ActorId_FechaUtc",
                table: "EventosAuditoria",
                columns: new[] { "ActorId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosAuditoria_EntidadId_FechaUtc",
                table: "EventosAuditoria",
                columns: new[] { "EntidadId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosAuditoria_FechaUtc",
                table: "EventosAuditoria",
                column: "FechaUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosAuditoria");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pago_Estado",
                table: "Pagos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pago_Estado",
                table: "Pagos",
                sql: "\"Estado\" IN (0, 1, 2)");
        }
    }
}
