using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConciliacionProgramada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConciliacionEnCurso",
                table: "PagosReserva",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaginaConciliacion",
                table: "PagosReserva",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaConciliacionEnUtc",
                table: "PagosReserva",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsMercadoPago",
                table: "Pagos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaginaConciliacion",
                table: "Pagos",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaConciliacionEnUtc",
                table: "Pagos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosReserva_Estado_UltimaConciliacionEnUtc",
                table: "PagosReserva",
                columns: new[] { "Estado", "UltimaConciliacionEnUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PagoReserva_PaginaConciliacion",
                table: "PagosReserva",
                sql: "\"PaginaConciliacion\" BETWEEN 0 AND 10000");
            // Clasificar solo checkouts anteriores identificables, sin interpretar referencias manuales.
            migrationBuilder.Sql("UPDATE \"Pagos\" SET \"EsMercadoPago\" = TRUE WHERE \"PreferenciaMercadoPagoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_EsMercadoPago_Estado_UltimaConciliacionEnUtc",
                table: "Pagos",
                columns: new[] { "EsMercadoPago", "Estado", "UltimaConciliacionEnUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pago_PaginaConciliacion",
                table: "Pagos",
                sql: "\"PaginaConciliacion\" BETWEEN 0 AND 10000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PagosReserva_Estado_UltimaConciliacionEnUtc",
                table: "PagosReserva");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PagoReserva_PaginaConciliacion",
                table: "PagosReserva");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_EsMercadoPago_Estado_UltimaConciliacionEnUtc",
                table: "Pagos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pago_PaginaConciliacion",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "ConciliacionEnCurso",
                table: "PagosReserva");

            migrationBuilder.DropColumn(
                name: "PaginaConciliacion",
                table: "PagosReserva");

            migrationBuilder.DropColumn(
                name: "UltimaConciliacionEnUtc",
                table: "PagosReserva");

            migrationBuilder.DropColumn(
                name: "EsMercadoPago",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "PaginaConciliacion",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "UltimaConciliacionEnUtc",
                table: "Pagos");
        }
    }
}
