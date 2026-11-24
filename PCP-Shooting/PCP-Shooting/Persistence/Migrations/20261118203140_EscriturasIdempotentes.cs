using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EscriturasIdempotentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_DetallePedidoId",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_Membresias_PagoId",
                table: "Membresias");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_DetallePedidoId",
                table: "MovimientosInventario",
                column: "DetallePedidoId",
                unique: true,
                filter: "\"DetallePedidoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_PagoId",
                table: "Membresias",
                column: "PagoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_DetallePedidoId",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_Membresias_PagoId",
                table: "Membresias");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_DetallePedidoId",
                table: "MovimientosInventario",
                column: "DetallePedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_PagoId",
                table: "Membresias",
                column: "PagoId");
        }
    }
}
