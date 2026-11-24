using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GaleriaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlbumsGaleria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Publicado = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumsGaleria", x => x.Id);
                    table.CheckConstraint("CK_Album_Nombre", "length(trim(\"Nombre\")) > 0");
                    table.CheckConstraint("CK_Album_Orden", "\"Orden\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FotosGaleria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TextoAlternativo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Publicada = table.Column<bool>(type: "boolean", nullable: false),
                    ImagenUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosGaleria", x => x.Id);
                    table.CheckConstraint("CK_Foto_Orden", "\"Orden\" >= 0");
                    table.CheckConstraint("CK_Foto_Publicada", "NOT \"Publicada\" OR \"ImagenUrl\" IS NOT NULL");
                    table.CheckConstraint("CK_Foto_Textos", "length(trim(\"Titulo\")) > 0 AND length(trim(\"TextoAlternativo\")) > 0");
                    table.ForeignKey(
                        name: "FK_FotosGaleria_AlbumsGaleria_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "AlbumsGaleria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CargasFotosGaleria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FotoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_CargasFotosGaleria", x => x.Id);
                    table.CheckConstraint("CK_CargaGaleria_Tamano", "\"Tamano\" > 0 AND \"Tamano\" <= 5242880");
                    table.ForeignKey(
                        name: "FK_CargasFotosGaleria_FotosGaleria_FotoId",
                        column: x => x.FotoId,
                        principalTable: "FotosGaleria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CargasFotosGaleria_PerfilesUsuario_ActorId",
                        column: x => x.ActorId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumsGaleria_Publicado_Orden_Id",
                table: "AlbumsGaleria",
                columns: new[] { "Publicado", "Orden", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CargasFotosGaleria_ActorId",
                table: "CargasFotosGaleria",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_CargasFotosGaleria_FotoId",
                table: "CargasFotosGaleria",
                column: "FotoId");

            migrationBuilder.CreateIndex(
                name: "IX_CargasFotosGaleria_Ruta",
                table: "CargasFotosGaleria",
                column: "Ruta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FotosGaleria_AlbumId_Publicada_Orden_Id",
                table: "FotosGaleria",
                columns: new[] { "AlbumId", "Publicada", "Orden", "Id" });
            migrationBuilder.Sql("""
                ALTER TABLE "AlbumsGaleria" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FotosGaleria" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "CargasFotosGaleria" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "AlbumsGaleria", "FotosGaleria", "CargasFotosGaleria" FROM anon, authenticated;
                """);
            migrationBuilder.Sql("""
                INSERT INTO storage.buckets(id,name,public,file_size_limit,allowed_mime_types)
                VALUES ('pcp-galeria','pcp-galeria',true,5242880,ARRAY['image/jpeg','image/png','image/webp']::text[])
                ON CONFLICT(id) DO NOTHING;
                CREATE FUNCTION public.pcp_puede_subir_galeria(nombre text)
                RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = ''
                AS $func$
                    SELECT EXISTS (
                        SELECT 1 FROM public."CargasFotosGaleria" c
                        JOIN public."FotosGaleria" f ON f."Id"=c."FotoId"
                        JOIN public."PerfilesUsuario" u ON u."Id"=auth.uid()
                        WHERE c."Ruta"=nombre AND NOT c."Disponible" AND c."ActorId"=auth.uid()
                            AND u."Activo" AND u."Rol"=1 AND f."Version"=c."VersionDestino"
                    );
                $func$;
                REVOKE ALL ON FUNCTION public.pcp_puede_subir_galeria(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION public.pcp_puede_subir_galeria(text) TO authenticated;
                CREATE POLICY pcp_galeria_insertar ON storage.objects FOR INSERT TO authenticated
                    WITH CHECK (bucket_id='pcp-galeria' AND public.pcp_puede_subir_galeria(name));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS pcp_galeria_insertar ON storage.objects;
                DROP FUNCTION IF EXISTS public.pcp_puede_subir_galeria(text);
                """);
            migrationBuilder.DropTable(
                name: "CargasFotosGaleria");

            migrationBuilder.DropTable(
                name: "FotosGaleria");

            migrationBuilder.DropTable(
                name: "AlbumsGaleria");
        }
    }
}
