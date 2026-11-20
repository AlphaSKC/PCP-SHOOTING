using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracionOperativaYLineas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionOperativa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DuracionActividadMinutos = table.Column<int>(type: "integer", nullable: false),
                    MargenMinutos = table.Column<int>(type: "integer", nullable: false),
                    ApartadoPagoMinutos = table.Column<int>(type: "integer", nullable: false),
                    AnticipacionMinimaMinutos = table.Column<int>(type: "integer", nullable: false),
                    AnticipacionMaximaDias = table.Column<int>(type: "integer", nullable: false),
                    LlegadaAnticipadaMinutos = table.Column<int>(type: "integer", nullable: false),
                    ToleranciaLlegadaMinutos = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionOperativa", x => x.Id);
                    table.CheckConstraint("CK_Operacion_Anticipacion", "\"AnticipacionMinimaMinutos\" BETWEEN 0 AND 10080 AND \"AnticipacionMaximaDias\" BETWEEN 1 AND 365 AND \"AnticipacionMinimaMinutos\" < \"AnticipacionMaximaDias\" * 1440");
                    table.CheckConstraint("CK_Operacion_Llegadas", "\"LlegadaAnticipadaMinutos\" BETWEEN 0 AND 60 AND \"ToleranciaLlegadaMinutos\" BETWEEN 0 AND 60");
                    table.CheckConstraint("CK_Operacion_Tiempos", "\"DuracionActividadMinutos\" BETWEEN 1 AND 240 AND \"MargenMinutos\" BETWEEN 0 AND 60 AND \"ApartadoPagoMinutos\" BETWEEN 1 AND 60");
                    table.CheckConstraint("CK_Operacion_Unica", "\"Id\" = 'e4ec8df0-0000-4000-8000-000000000001'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "HorariosOperacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    Cerrado = table.Column<bool>(type: "boolean", nullable: false),
                    Apertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Cierre = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    PrimerTurno = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    UltimoTurno = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosOperacion", x => x.Id);
                    table.CheckConstraint("CK_Horario_Dia", "\"DiaSemana\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_Horario_Rango", "\"Cerrado\" OR (\"Apertura\" < \"Cierre\" AND \"PrimerTurno\" >= \"Apertura\" AND \"UltimoTurno\" >= \"PrimerTurno\" AND \"UltimoTurno\" < \"Cierre\")");
                });

            migrationBuilder.CreateTable(
                name: "LineasTiro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasTiro", x => x.Id);
                    table.CheckConstraint("CK_Linea_Capacidad", "\"Capacidad\" = 1");
                    table.CheckConstraint("CK_Linea_Nombre", "length(trim(\"Nombre\")) > 0");
                    table.CheckConstraint("CK_Linea_Numero", "\"Numero\" BETWEEN 1 AND 15");
                });

            migrationBuilder.CreateTable(
                name: "BloqueosOperacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LineaId = table.Column<Guid>(type: "uuid", nullable: true),
                    InicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueosOperacion", x => x.Id);
                    table.CheckConstraint("CK_Bloqueo_Motivo", "length(trim(\"Motivo\")) > 0");
                    table.CheckConstraint("CK_Bloqueo_Rango", "\"FinUtc\" > \"InicioUtc\"");
                    table.ForeignKey(
                        name: "FK_BloqueosOperacion_LineasTiro_LineaId",
                        column: x => x.LineaId,
                        principalTable: "LineasTiro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BloqueosOperacion_PerfilesUsuario_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionOperativa",
                columns: new[] { "Id", "AnticipacionMaximaDias", "AnticipacionMinimaMinutos", "ApartadoPagoMinutos", "DuracionActividadMinutos", "LlegadaAnticipadaMinutos", "MargenMinutos", "ToleranciaLlegadaMinutos", "Version" },
                values: new object[] { new Guid("e4ec8df0-0000-4000-8000-000000000001"), 2, 60, 10, 10, 3, 5, 3, new Guid("e4ec8df1-0000-4000-8000-000000000001") });

            migrationBuilder.InsertData(
                table: "HorariosOperacion",
                columns: new[] { "Id", "Apertura", "Cerrado", "Cierre", "DiaSemana", "PrimerTurno", "UltimoTurno" },
                values: new object[,]
                {
                    { new Guid("e4ec8df2-0000-4000-8000-000000000001"), new TimeOnly(8, 0, 0), false, new TimeOnly(18, 0, 0), 0, new TimeOnly(11, 0, 0), new TimeOnly(15, 30, 0) },
                    { new Guid("e4ec8df2-0000-4000-8000-000000000002"), new TimeOnly(8, 0, 0), true, new TimeOnly(18, 0, 0), 1, new TimeOnly(11, 0, 0), new TimeOnly(15, 30, 0) },
                    { new Guid("e4ec8df2-0000-4000-8000-000000000003"), new TimeOnly(8, 0, 0), false, new TimeOnly(18, 0, 0), 2, new TimeOnly(11, 0, 0), new TimeOnly(15, 30, 0) },
                    { new Guid("e4ec8df2-0000-4000-8000-000000000004"), new TimeOnly(8, 0, 0), false, new TimeOnly(18, 0, 0), 3, new TimeOnly(11, 0, 0), new TimeOnly(15, 30, 0) },
                    { new Guid("e4ec8df2-0000-4000-8000-000000000005"), new TimeOnly(8, 0, 0), false, new TimeOnly(18, 0, 0), 4, new TimeOnly(11, 0, 0), new TimeOnly(15, 30, 0) },
                    { new Guid("e4ec8df2-0000-4000-8000-000000000006"), new TimeOnly(8, 0, 0), false, new TimeOnly(18, 0, 0), 5, new TimeOnly(11, 0, 0), new TimeOnly(15, 30, 0) },
                    { new Guid("e4ec8df2-0000-4000-8000-000000000007"), new TimeOnly(8, 0, 0), false, new TimeOnly(18, 0, 0), 6, new TimeOnly(11, 0, 0), new TimeOnly(15, 30, 0) }
                });

            migrationBuilder.InsertData(
                table: "LineasTiro",
                columns: new[] { "Id", "Activa", "Capacidad", "Nombre", "Numero", "Version" },
                values: new object[,]
                {
                    { new Guid("e4ec8df3-0000-4000-8000-000000000001"), true, 1, "Línea 1", 1, new Guid("e4ec8df4-0000-4000-8000-000000000001") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000002"), true, 1, "Línea 2", 2, new Guid("e4ec8df4-0000-4000-8000-000000000002") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000003"), true, 1, "Línea 3", 3, new Guid("e4ec8df4-0000-4000-8000-000000000003") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000004"), true, 1, "Línea 4", 4, new Guid("e4ec8df4-0000-4000-8000-000000000004") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000005"), true, 1, "Línea 5", 5, new Guid("e4ec8df4-0000-4000-8000-000000000005") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000006"), true, 1, "Línea 6", 6, new Guid("e4ec8df4-0000-4000-8000-000000000006") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000007"), true, 1, "Línea 7", 7, new Guid("e4ec8df4-0000-4000-8000-000000000007") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000008"), true, 1, "Línea 8", 8, new Guid("e4ec8df4-0000-4000-8000-000000000008") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000009"), true, 1, "Línea 9", 9, new Guid("e4ec8df4-0000-4000-8000-000000000009") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000010"), true, 1, "Línea 10", 10, new Guid("e4ec8df4-0000-4000-8000-000000000010") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000011"), true, 1, "Línea 11", 11, new Guid("e4ec8df4-0000-4000-8000-000000000011") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000012"), true, 1, "Línea 12", 12, new Guid("e4ec8df4-0000-4000-8000-000000000012") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000013"), true, 1, "Línea 13", 13, new Guid("e4ec8df4-0000-4000-8000-000000000013") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000014"), true, 1, "Línea 14", 14, new Guid("e4ec8df4-0000-4000-8000-000000000014") },
                    { new Guid("e4ec8df3-0000-4000-8000-000000000015"), true, 1, "Línea 15", 15, new Guid("e4ec8df4-0000-4000-8000-000000000015") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosOperacion_Activo_LineaId_InicioUtc_FinUtc",
                table: "BloqueosOperacion",
                columns: new[] { "Activo", "LineaId", "InicioUtc", "FinUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosOperacion_CreadoPorId",
                table: "BloqueosOperacion",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosOperacion_LineaId",
                table: "BloqueosOperacion",
                column: "LineaId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosOperacion_DiaSemana",
                table: "HorariosOperacion",
                column: "DiaSemana",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasTiro_Numero",
                table: "LineasTiro",
                column: "Numero",
                unique: true);
            migrationBuilder.Sql("""
                ALTER TABLE "ConfiguracionOperativa" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "HorariosOperacion" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "LineasTiro" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "BloqueosOperacion" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "ConfiguracionOperativa", "HorariosOperacion", "LineasTiro", "BloqueosOperacion" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueosOperacion");

            migrationBuilder.DropTable(
                name: "ConfiguracionOperativa");

            migrationBuilder.DropTable(
                name: "HorariosOperacion");

            migrationBuilder.DropTable(
                name: "LineasTiro");
        }
    }
}
