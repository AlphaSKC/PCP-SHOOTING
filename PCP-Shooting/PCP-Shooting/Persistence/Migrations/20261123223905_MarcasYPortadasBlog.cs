using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MarcasYPortadasBlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarcasAliadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarcasAliadas", x => x.Id);
                    table.CheckConstraint("CK_Marca_Nombre", "length(trim(\"Nombre\")) > 0");
                    table.CheckConstraint("CK_Marca_Orden", "\"Orden\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "CargasImagenContenido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarcaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ArticuloBlogId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ruta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TipoContenido = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Tamano = table.Column<long>(type: "bigint", nullable: false),
                    VersionDestino = table.Column<Guid>(type: "uuid", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    CreadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubidaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargasImagenContenido", x => x.Id);
                    table.CheckConstraint("CK_CargaContenido_Destino", "(\"MarcaId\" IS NOT NULL AND \"ArticuloBlogId\" IS NULL) OR (\"MarcaId\" IS NULL AND \"ArticuloBlogId\" IS NOT NULL)");
                    table.CheckConstraint("CK_CargaContenido_Disponible", "NOT \"Disponible\" OR \"SubidaEnUtc\" IS NOT NULL");
                    table.CheckConstraint("CK_CargaContenido_Tamano", "\"Tamano\" > 0 AND \"Tamano\" <= CASE WHEN \"MarcaId\" IS NOT NULL THEN 2097152 ELSE 5242880 END");
                    table.ForeignKey(
                        name: "FK_CargasImagenContenido_ArticulosBlog_ArticuloBlogId",
                        column: x => x.ArticuloBlogId,
                        principalTable: "ArticulosBlog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CargasImagenContenido_MarcasAliadas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "MarcasAliadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CargasImagenContenido_PerfilesUsuario_CreadorId",
                        column: x => x.CreadorId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MarcasAliadas",
                columns: new[] { "Id", "Activo", "ActualizadoEnUtc", "CreadoEnUtc", "Descripcion", "LogoUrl", "Nombre", "Orden", "Url", "Version" },
                values: new object[,]
                {
                    { new Guid("e4ec8df0-0000-4000-8000-000000000110"), true, new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Airguns FV", 0, null, new Guid("e4ec8df0-0000-4000-8000-000000000210") },
                    { new Guid("e4ec8df0-0000-4000-8000-000000000111"), true, new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Altaros", 1, null, new Guid("e4ec8df0-0000-4000-8000-000000000211") },
                    { new Guid("e4ec8df0-0000-4000-8000-000000000112"), true, new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "ZAN Projectiles", 2, null, new Guid("e4ec8df0-0000-4000-8000-000000000212") },
                    { new Guid("e4ec8df0-0000-4000-8000-000000000113"), true, new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Donny FL", 3, null, new Guid("e4ec8df0-0000-4000-8000-000000000213") },
                    { new Guid("e4ec8df0-0000-4000-8000-000000000114"), true, new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "AEA", 4, null, new Guid("e4ec8df0-0000-4000-8000-000000000214") },
                    { new Guid("e4ec8df0-0000-4000-8000-000000000115"), true, new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "H&V", 5, null, new Guid("e4ec8df0-0000-4000-8000-000000000215") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CargasImagenContenido_ArticuloBlogId",
                table: "CargasImagenContenido",
                column: "ArticuloBlogId");

            migrationBuilder.CreateIndex(
                name: "IX_CargasImagenContenido_CreadorId",
                table: "CargasImagenContenido",
                column: "CreadorId");

            migrationBuilder.CreateIndex(
                name: "IX_CargasImagenContenido_MarcaId",
                table: "CargasImagenContenido",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_CargasImagenContenido_Ruta",
                table: "CargasImagenContenido",
                column: "Ruta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarcasAliadas_Activo_Orden",
                table: "MarcasAliadas",
                columns: new[] { "Activo", "Orden" });
            migrationBuilder.Sql("""
                ALTER TABLE "MarcasAliadas" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "CargasImagenContenido" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "MarcasAliadas", "CargasImagenContenido" FROM anon, authenticated;
                """);
            migrationBuilder.Sql("""
                INSERT INTO storage.buckets(id,name,public,file_size_limit,allowed_mime_types)
                VALUES ('pcp-marcas','pcp-marcas',true,2097152,ARRAY['image/jpeg','image/png','image/webp']::text[]),
                    ('pcp-blog','pcp-blog',true,5242880,ARRAY['image/jpeg','image/png','image/webp']::text[])
                ON CONFLICT(id) DO NOTHING;

                CREATE FUNCTION public.pcp_puede_subir_imagen_contenido(bucket text,nombre text)
                RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = ''
                AS $func$
                    SELECT EXISTS (
                        SELECT 1 FROM public."CargasImagenContenido" c
                        JOIN public."PerfilesUsuario" u ON u."Id"=auth.uid()
                        LEFT JOIN public."MarcasAliadas" m ON m."Id"=c."MarcaId"
                        LEFT JOIN public."ArticulosBlog" a ON a."Id"=c."ArticuloBlogId"
                        WHERE c."Ruta"=nombre AND NOT c."Disponible" AND c."CreadorId"=auth.uid() AND u."Activo" AND u."Rol"=1
                            AND ((bucket='pcp-marcas' AND c."MarcaId" IS NOT NULL AND m."Version"=c."VersionDestino")
                                OR (bucket='pcp-blog' AND c."ArticuloBlogId" IS NOT NULL AND a."Version"=c."VersionDestino"))
                    );
                $func$;
                REVOKE ALL ON FUNCTION public.pcp_puede_subir_imagen_contenido(text,text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION public.pcp_puede_subir_imagen_contenido(text,text) TO authenticated;
                CREATE POLICY pcp_contenido_insertar_imagen ON storage.objects FOR INSERT TO authenticated
                    WITH CHECK (bucket_id IN ('pcp-marcas','pcp-blog') AND public.pcp_puede_subir_imagen_contenido(bucket_id,name));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS pcp_contenido_insertar_imagen ON storage.objects;
                DROP FUNCTION IF EXISTS public.pcp_puede_subir_imagen_contenido(text,text);
                """);
            // Conservar los buckets y los archivos públicos durante un rollback.
            migrationBuilder.DropTable(
                name: "CargasImagenContenido");

            migrationBuilder.DropTable(
                name: "MarcasAliadas");
        }
    }
}
