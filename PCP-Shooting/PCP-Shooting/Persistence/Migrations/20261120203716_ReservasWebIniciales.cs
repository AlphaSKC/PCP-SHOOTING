using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReservasWebIniciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservasWeb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApartadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaqueteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaqueteVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoPaquete = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NombrePaquete = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DescripcionPaquete = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IncluyeMarkdown = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    RequisitosMarkdown = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    SolicitudHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrecioPorPersona = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PrecioSocioInformativo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ZonaHoraria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    InicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinActividadUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinOcupacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VencePagoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasWeb", x => x.Id);
                    table.CheckConstraint("CK_ReservaWeb_Precios", "\"PrecioPorPersona\" > 0 AND \"Total\" > 0 AND (\"PrecioSocioInformativo\" IS NULL OR \"PrecioSocioInformativo\" > 0)");
                    table.CheckConstraint("CK_ReservaWeb_Tiempos", "\"FinActividadUtc\" > \"InicioUtc\" AND \"FinOcupacionUtc\" >= \"FinActividadUtc\" AND \"VencePagoEnUtc\" > \"CreadoEnUtc\"");
                    table.ForeignKey(
                        name: "FK_ReservasWeb_Apartados_ApartadoId",
                        column: x => x.ApartadoId,
                        principalTable: "Apartados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasWeb_PerfilesUsuario_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasWeb_PerfilesUsuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsumosReserva",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    CantidadPorPersona = table.Column<int>(type: "integer", nullable: false),
                    CantidadTotal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumosReserva", x => x.Id);
                    table.CheckConstraint("CK_ConsumoReserva_Cantidades", "\"CantidadPorPersona\" BETWEEN 1 AND 100000 AND \"CantidadTotal\" BETWEEN 1 AND 1500000");
                    table.ForeignKey(
                        name: "FK_ConsumosReserva_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumosReserva_ReservasWeb_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "ReservasWeb",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantesReserva",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Nacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    TutorNombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TutorNacimiento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantesReserva", x => x.Id);
                    table.CheckConstraint("CK_Participante_Nombre", "length(trim(\"Nombre\")) > 0");
                    table.ForeignKey(
                        name: "FK_ParticipantesReserva_LineasTiro_LineaId",
                        column: x => x.LineaId,
                        principalTable: "LineasTiro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantesReserva_ReservasWeb_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "ReservasWeb",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosReserva_ProductoId",
                table: "ConsumosReserva",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosReserva_ReservaId_ProductoId",
                table: "ConsumosReserva",
                columns: new[] { "ReservaId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesReserva_LineaId",
                table: "ParticipantesReserva",
                column: "LineaId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantesReserva_ReservaId_LineaId",
                table: "ParticipantesReserva",
                columns: new[] { "ReservaId", "LineaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservasWeb_ApartadoId",
                table: "ReservasWeb",
                column: "ApartadoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservasWeb_CreadoPorId",
                table: "ReservasWeb",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasWeb_UsuarioId_CreadoEnUtc",
                table: "ReservasWeb",
                columns: new[] { "UsuarioId", "CreadoEnUtc" });
            migrationBuilder.Sql("""
                ALTER TABLE "ReservasWeb" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ParticipantesReserva" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ConsumosReserva" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "ReservasWeb", "ParticipantesReserva", "ConsumosReserva" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumosReserva");

            migrationBuilder.DropTable(
                name: "ParticipantesReserva");

            migrationBuilder.DropTable(
                name: "ReservasWeb");
        }
    }
}
