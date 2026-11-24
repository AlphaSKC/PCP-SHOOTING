using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContenidoInstitucional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionContenidoSitio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DatosJson = table.Column<string>(type: "jsonb", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionContenidoSitio", x => x.Id);
                    table.CheckConstraint("CK_ContenidoSitio_Objeto", "jsonb_typeof(\"DatosJson\") = 'object'");
                    table.CheckConstraint("CK_ContenidoSitio_Unico", "\"Id\" = 'e4ec8df0-0000-4000-8000-000000000020'::uuid");
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionContenidoSitio",
                columns: new[] { "Id", "ActualizadoEnUtc", "DatosJson", "Version" },
                values: new object[] { new Guid("e4ec8df0-0000-4000-8000-000000000020"), new DateTime(2026, 11, 24, 0, 0, 0, 0, DateTimeKind.Utc), "{\"nosotrosTitulo\":\"El primer club de tiro exclusivo para armas de aire comprimido en M\\u00E9xico\",\"nosotrosTexto\":\"En la Ruta de los Cenotes, Quintana Roo, reunimos a entusiastas y principiantes del tiro deportivo y recreativo con rifles PCP, en un entorno seguro y conforme a la normativa aplicable.\",\"seguridadTitulo\":\"Un campo seguro para todos\",\"seguridadTexto\":\"Consulta el reglamento del campo y los requisitos de participaci\\u00F3n antes de realizar tu actividad.\",\"testimonio\":null,\"autorTestimonio\":null,\"descripcionPie\":\"El primer club de tiro exclusivo para armas de aire comprimido en M\\u00E9xico. Rifles PCP para tiro deportivo y recreativo, con energ\\u00EDa m\\u00E1xima de 140 joules.\",\"direccion\":\"Km 23 Ruta de los Cenotes, Quintana Roo\",\"horarioPublico\":\"Martes a domingo, 8:00 a 18:00 \\u00B7 lunes cerrado\",\"telefono\":\"998 159 6699\",\"telefonoMarcacion\":\"\\u002B529981596699\",\"whatsAppNumero\":\"529981596699\",\"correo\":\"contacto@pcpshooting.com\",\"ubicacionMapa\":\"PcP Shooting Ruta de los Cenotes\",\"instagramUrl\":\"https://www.instagram.com/pcpshooting/\",\"facebookUrl\":\"https://www.facebook.com/profile.php?id=61594533773594\",\"tikTokUrl\":\"https://www.tiktok.com/@pcp.shooting\",\"youTubeUrl\":\"https://www.youtube.com/@pcpshooting\"}", new Guid("e4ec8df0-0000-4000-8000-000000000021") });
            migrationBuilder.Sql("""
                ALTER TABLE "ConfiguracionContenidoSitio" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "ConfiguracionContenidoSitio" FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionContenidoSitio");
        }
    }
}
