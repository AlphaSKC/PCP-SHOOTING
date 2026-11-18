using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ValidacionesPersistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesPedido_Pedidos_PedidoId",
                table: "DetallesPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallesPedido_Productos_ProductoId",
                table: "DetallesPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_Membresias_Pagos_PagoId",
                table: "Membresias");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_DetallesPedido_DetallePedidoId",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Productos_ProductoId",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Pedidos_PedidoId",
                table: "Pagos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Precio",
                table: "Productos",
                sql: "\"Precio\" IS NULL OR \"Precio\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Vendible",
                table: "Productos",
                sql: "NOT \"EsVendible\" OR \"Precio\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pedido_Estado",
                table: "Pedidos",
                sql: "\"Estado\" IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pago_Confirmacion",
                table: "Pagos",
                sql: "\"Estado\" <> 1 OR \"ConfirmadoEnUtc\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pago_Estado",
                table: "Pagos",
                sql: "\"Estado\" IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pago_Importe",
                table: "Pagos",
                sql: "\"Importe\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movimiento_Variacion",
                table: "MovimientosInventario",
                sql: "(\"Tipo\" = 0 AND \"VariacionCantidad\" > 0) OR (\"Tipo\" = 1 AND \"VariacionCantidad\" < 0) OR (\"Tipo\" = 2 AND \"VariacionCantidad\" <> 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Membresia_Vigencia",
                table: "Membresias",
                sql: "\"FinVigenciaExclusivo\" > \"InicioVigencia\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Detalle_Cantidad",
                table: "DetallesPedido",
                sql: "\"Cantidad\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Detalle_Precio",
                table: "DetallesPedido",
                sql: "\"PrecioUnitario\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesPedido_Pedidos_PedidoId",
                table: "DetallesPedido",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesPedido_Productos_ProductoId",
                table: "DetallesPedido",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Membresias_Pagos_PagoId",
                table: "Membresias",
                column: "PagoId",
                principalTable: "Pagos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_DetallesPedido_DetallePedidoId",
                table: "MovimientosInventario",
                column: "DetallePedidoId",
                principalTable: "DetallesPedido",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Productos_ProductoId",
                table: "MovimientosInventario",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Pedidos_PedidoId",
                table: "Pagos",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesPedido_Pedidos_PedidoId",
                table: "DetallesPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallesPedido_Productos_ProductoId",
                table: "DetallesPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_Membresias_Pagos_PagoId",
                table: "Membresias");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_DetallesPedido_DetallePedidoId",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Productos_ProductoId",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Pedidos_PedidoId",
                table: "Pagos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Precio",
                table: "Productos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Vendible",
                table: "Productos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pedido_Estado",
                table: "Pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pago_Confirmacion",
                table: "Pagos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pago_Estado",
                table: "Pagos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pago_Importe",
                table: "Pagos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movimiento_Variacion",
                table: "MovimientosInventario");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Membresia_Vigencia",
                table: "Membresias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Detalle_Cantidad",
                table: "DetallesPedido");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Detalle_Precio",
                table: "DetallesPedido");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesPedido_Pedidos_PedidoId",
                table: "DetallesPedido",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesPedido_Productos_ProductoId",
                table: "DetallesPedido",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Membresias_Pagos_PagoId",
                table: "Membresias",
                column: "PagoId",
                principalTable: "Pagos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_DetallesPedido_DetallePedidoId",
                table: "MovimientosInventario",
                column: "DetallePedidoId",
                principalTable: "DetallesPedido",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Productos_ProductoId",
                table: "MovimientosInventario",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Pedidos_PedidoId",
                table: "Pagos",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
