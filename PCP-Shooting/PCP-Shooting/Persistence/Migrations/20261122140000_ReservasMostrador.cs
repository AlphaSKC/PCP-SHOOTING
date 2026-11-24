using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PCP_Shooting.Persistence;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations;

[Migration("20261122140000_ReservasMostrador")]
[DbContext(typeof(PcpShootingDbContext))]
public sealed class ReservasMostrador : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(name: "ApartadoId", table: "ReservasWeb", type: "uuid", nullable: true,
            oldClrType: typeof(Guid), oldType: "uuid");
        migrationBuilder.AddColumn<bool>(name: "EsMostrador", table: "ReservasWeb", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.DropIndex(name: "IX_ReservasWeb_ApartadoId", table: "ReservasWeb");
        migrationBuilder.CreateIndex(name: "IX_ReservasWeb_ApartadoId", table: "ReservasWeb", column: "ApartadoId", unique: true,
            filter: "\"ApartadoId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_ReservasWeb_ApartadoId", table: "ReservasWeb");
        migrationBuilder.DropColumn(name: "EsMostrador", table: "ReservasWeb");
        migrationBuilder.AlterColumn<Guid>(name: "ApartadoId", table: "ReservasWeb", type: "uuid", nullable: false,
            oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.CreateIndex(name: "IX_ReservasWeb_ApartadoId", table: "ReservasWeb", column: "ApartadoId", unique: true);
    }
}
