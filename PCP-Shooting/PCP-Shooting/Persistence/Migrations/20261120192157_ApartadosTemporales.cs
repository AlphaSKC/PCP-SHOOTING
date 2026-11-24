using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApartadosTemporales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Apartados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaqueteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaqueteVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfiguracionVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ZonaHoraria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    InicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinActividadUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinOcupacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VenceEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apartados", x => x.Id);
                    table.CheckConstraint("CK_Apartado_Ocupacion", "\"FinActividadUtc\" > \"InicioUtc\" AND \"FinOcupacionUtc\" >= \"FinActividadUtc\"");
                    table.CheckConstraint("CK_Apartado_Vencimiento", "\"VenceEnUtc\" > \"CreadoEnUtc\"");
                    table.ForeignKey(
                        name: "FK_Apartados_Paquetes_PaqueteId",
                        column: x => x.PaqueteId,
                        principalTable: "Paquetes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Apartados_PerfilesUsuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineasApartado",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApartadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasApartado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasApartado_Apartados_ApartadoId",
                        column: x => x.ApartadoId,
                        principalTable: "Apartados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasApartado_LineasTiro_LineaId",
                        column: x => x.LineaId,
                        principalTable: "LineasTiro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Apartados_PaqueteId",
                table: "Apartados",
                column: "PaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_Apartados_UsuarioId_CreadoEnUtc",
                table: "Apartados",
                columns: new[] { "UsuarioId", "CreadoEnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartados_VenceEnUtc_InicioUtc_FinOcupacionUtc",
                table: "Apartados",
                columns: new[] { "VenceEnUtc", "InicioUtc", "FinOcupacionUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LineasApartado_ApartadoId_LineaId",
                table: "LineasApartado",
                columns: new[] { "ApartadoId", "LineaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasApartado_LineaId_ApartadoId",
                table: "LineasApartado",
                columns: new[] { "LineaId", "ApartadoId" });
            migrationBuilder.Sql("""
                ALTER TABLE "Apartados" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "LineasApartado" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "Apartados", "LineasApartado" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineasApartado");

            migrationBuilder.DropTable(
                name: "Apartados");
        }
    }
}
