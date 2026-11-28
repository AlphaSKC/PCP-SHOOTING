using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BaseOrdenesAlimentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DetalleOrdenAlimentosId",
                table: "MovimientosInventario",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrdenesAlimentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreadaPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EsMostrador = table.Column<bool>(type: "boolean", nullable: false),
                    NombreRecogida = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SolicitudHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnPreparacionEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ListaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EntregadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PagoConfirmadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImporteConfirmado = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    MetodoCobro = table.Column<int>(type: "integer", nullable: true),
                    ReferenciaCobro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesAlimentos", x => x.Id);
                    table.CheckConstraint("CK_OrdenAlimentos_Cliente", "\"EsMostrador\" OR \"ClienteId\" IS NOT NULL");
                    table.CheckConstraint("CK_OrdenAlimentos_Entrega", "(\"Estado\"=3 AND \"EntregadaEnUtc\" IS NOT NULL AND \"PagoConfirmadoEnUtc\" IS NOT NULL) OR (\"Estado\"<>3 AND \"EntregadaEnUtc\" IS NULL)");
                    table.CheckConstraint("CK_OrdenAlimentos_Estado", "\"Estado\" IN (0,1,2,3)");
                    table.CheckConstraint("CK_OrdenAlimentos_Nombre", "length(trim(\"NombreRecogida\"))>0");
                    table.CheckConstraint("CK_OrdenAlimentos_Pago", "(\"PagoConfirmadoEnUtc\" IS NULL AND \"ImporteConfirmado\"=0 AND \"MetodoCobro\" IS NULL AND \"ReferenciaCobro\" IS NULL) OR (\"PagoConfirmadoEnUtc\" IS NOT NULL AND \"ImporteConfirmado\"=\"Total\" AND \"MetodoCobro\" IN (0,1) AND \"MetodoCobro\" IS NOT NULL AND length(trim(\"ReferenciaCobro\"))>0 AND \"ReferenciaCobro\" IS NOT NULL)");
                    table.CheckConstraint("CK_OrdenAlimentos_Total", "\"Total\" > 0 AND \"ImporteConfirmado\" >= 0 AND \"ImporteConfirmado\" <= \"Total\"");
                    table.ForeignKey(
                        name: "FK_OrdenesAlimentos_PerfilesUsuario_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesAlimentos_PerfilesUsuario_CreadaPorId",
                        column: x => x.CreadaPorId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesOrdenAlimentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdenId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesOrdenAlimentos", x => x.Id);
                    table.CheckConstraint("CK_DetalleAlimentos_Cantidad", "\"Cantidad\" BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_DetalleAlimentos_Precio", "\"PrecioUnitario\">0");
                    table.ForeignKey(
                        name: "FK_DetallesOrdenAlimentos_OrdenesAlimentos_OrdenId",
                        column: x => x.OrdenId,
                        principalTable: "OrdenesAlimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenAlimentos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperacionesOrdenAlimentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdenId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SolicitudHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacionesOrdenAlimentos", x => x.Id);
                    table.CheckConstraint("CK_OperacionAlimentos_Tipo", "\"Tipo\" IN ('EnPreparacion','Listo','Cobro','Entrega')");
                    table.ForeignKey(
                        name: "FK_OperacionesOrdenAlimentos_OrdenesAlimentos_OrdenId",
                        column: x => x.OrdenId,
                        principalTable: "OrdenesAlimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperacionesOrdenAlimentos_PerfilesUsuario_ActorId",
                        column: x => x.ActorId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_DetalleOrdenAlimentosId",
                table: "MovimientosInventario",
                column: "DetalleOrdenAlimentosId",
                unique: true,
                filter: "\"DetalleOrdenAlimentosId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenAlimentos_OrdenId_ProductoId",
                table: "DetallesOrdenAlimentos",
                columns: new[] { "OrdenId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenAlimentos_ProductoId",
                table: "DetallesOrdenAlimentos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesOrdenAlimentos_ActorId",
                table: "OperacionesOrdenAlimentos",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesOrdenAlimentos_OrdenId_CreadaEnUtc",
                table: "OperacionesOrdenAlimentos",
                columns: new[] { "OrdenId", "CreadaEnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesAlimentos_ClienteId_CreadaEnUtc",
                table: "OrdenesAlimentos",
                columns: new[] { "ClienteId", "CreadaEnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesAlimentos_CreadaPorId",
                table: "OrdenesAlimentos",
                column: "CreadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesAlimentos_Estado_CreadaEnUtc",
                table: "OrdenesAlimentos",
                columns: new[] { "Estado", "CreadaEnUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_DetallesOrdenAlimentos_DetalleOrdenAl~",
                table: "MovimientosInventario",
                column: "DetalleOrdenAlimentosId",
                principalTable: "DetallesOrdenAlimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                ALTER TABLE "OrdenesAlimentos" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "DetallesOrdenAlimentos" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "OperacionesOrdenAlimentos" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "OrdenesAlimentos","DetallesOrdenAlimentos","OperacionesOrdenAlimentos" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_DetallesOrdenAlimentos_DetalleOrdenAl~",
                table: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "DetallesOrdenAlimentos");

            migrationBuilder.DropTable(
                name: "OperacionesOrdenAlimentos");

            migrationBuilder.DropTable(
                name: "OrdenesAlimentos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_DetalleOrdenAlimentosId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "DetalleOrdenAlimentosId",
                table: "MovimientosInventario");
        }
    }
}
