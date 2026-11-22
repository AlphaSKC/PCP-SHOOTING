using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations;

public partial class R05MercadoPago : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PreferenciaMercadoPagoId", table: "Pagos", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CheckoutUrl", table: "Pagos", type: "character varying(2048)", maxLength: 2048, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "PagoConfirmadoEnUtc", table: "ReservasWeb", type: "timestamp with time zone", nullable: true);

        migrationBuilder.CreateTable(
            name: "PagosReserva",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                Importe = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                PreferenciaMercadoPagoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CheckoutUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                ReferenciaExterna = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Estado = table.Column<int>(type: "integer", nullable: false),
                CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ConfirmadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PagosReserva", x => x.Id);
                table.CheckConstraint("CK_PagoReserva_Confirmacion", "\"Estado\" <> 1 OR \"ConfirmadoEnUtc\" IS NOT NULL");
                table.CheckConstraint("CK_PagoReserva_Estado", "\"Estado\" IN (0, 1, 2, 3)");
                table.CheckConstraint("CK_PagoReserva_Importe", "\"Importe\" > 0");
                table.ForeignKey("FK_PagosReserva_ReservasWeb_ReservaId", x => x.ReservaId, "ReservasWeb", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Pagos_PreferenciaMercadoPagoId", "Pagos", "PreferenciaMercadoPagoId", unique: true,
            filter: "\"PreferenciaMercadoPagoId\" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_PagosReserva_PreferenciaMercadoPagoId", "PagosReserva", "PreferenciaMercadoPagoId", unique: true,
            filter: "\"PreferenciaMercadoPagoId\" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_PagosReserva_ReferenciaExterna", "PagosReserva", "ReferenciaExterna", unique: true);
        migrationBuilder.CreateIndex("IX_PagosReserva_ReservaId", "PagosReserva", "ReservaId", unique: true);
        migrationBuilder.CreateIndex("IX_ReservasWeb_PagoConfirmadoEnUtc", "ReservasWeb", "PagoConfirmadoEnUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PagosReserva");
        migrationBuilder.DropIndex("IX_Pagos_PreferenciaMercadoPagoId", "Pagos");
        migrationBuilder.DropIndex("IX_ReservasWeb_PagoConfirmadoEnUtc", "ReservasWeb");
        migrationBuilder.DropColumn("PreferenciaMercadoPagoId", "Pagos");
        migrationBuilder.DropColumn("CheckoutUrl", "Pagos");
        migrationBuilder.DropColumn("PagoConfirmadoEnUtc", "ReservasWeb");
    }
}
