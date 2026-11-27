using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PortadasPagina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortadasPagina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Pagina = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Configurada = table.Column<bool>(type: "boolean", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    ImagenUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Posicion = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Velo = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortadasPagina", x => x.Id);
                    table.CheckConstraint("CK_Portada_Imagen", "NOT (\"Configurada\" AND \"Activa\") OR length(trim(\"ImagenUrl\")) > 0 AND \"ImagenUrl\" IS NOT NULL");
                    table.CheckConstraint("CK_Portada_Pagina", "\"Pagina\" IN ('inicio','nosotros','reservar','catalogo','tienda','eventos','alimentos','membresias','contacto')");
                    table.CheckConstraint("CK_Portada_Posicion", "\"Posicion\" ~ '^(100|[1-9]?[0-9])% (100|[1-9]?[0-9])%$'");
                    table.CheckConstraint("CK_Portada_Velo", "\"Velo\" IN ('suave','normal','fuerte')");
                });

            migrationBuilder.CreateTable(
                name: "CargasPortadaPagina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortadaId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_CargasPortadaPagina", x => x.Id);
                    table.CheckConstraint("CK_CargaPortada_Disponible", "NOT \"Disponible\" OR \"SubidaEnUtc\" IS NOT NULL");
                    table.CheckConstraint("CK_CargaPortada_Tamano", "\"Tamano\" > 0 AND \"Tamano\" <= 8388608");
                    table.ForeignKey(
                        name: "FK_CargasPortadaPagina_PerfilesUsuario_CreadorId",
                        column: x => x.CreadorId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CargasPortadaPagina_PortadasPagina_PortadaId",
                        column: x => x.PortadaId,
                        principalTable: "PortadasPagina",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CargasPortadaPagina_CreadorId",
                table: "CargasPortadaPagina",
                column: "CreadorId");

            migrationBuilder.CreateIndex(
                name: "IX_CargasPortadaPagina_PortadaId",
                table: "CargasPortadaPagina",
                column: "PortadaId");

            migrationBuilder.CreateIndex(
                name: "IX_CargasPortadaPagina_Ruta",
                table: "CargasPortadaPagina",
                column: "Ruta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortadasPagina_Pagina",
                table: "PortadasPagina",
                column: "Pagina",
                unique: true);
            migrationBuilder.Sql("""
                ALTER TABLE "PortadasPagina" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "CargasPortadaPagina" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "PortadasPagina", "CargasPortadaPagina" FROM anon, authenticated;
                """);
            migrationBuilder.Sql("""
                INSERT INTO storage.buckets(id,name,public,file_size_limit,allowed_mime_types)
                VALUES ('pcp-portadas','pcp-portadas',true,8388608,ARRAY['image/jpeg','image/png','image/webp']::text[])
                ON CONFLICT(id) DO NOTHING;
                CREATE FUNCTION public.pcp_puede_subir_portada_pagina(nombre text)
                RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = ''
                AS $func$
                    SELECT EXISTS (
                        SELECT 1 FROM public."CargasPortadaPagina" c
                        JOIN public."PortadasPagina" p ON p."Id"=c."PortadaId"
                        JOIN public."PerfilesUsuario" u ON u."Id"=auth.uid()
                        WHERE c."Ruta"=nombre AND NOT c."Disponible" AND c."CreadorId"=auth.uid()
                            AND u."Activo" AND u."Rol"=1 AND p."Version"=c."VersionDestino"
                    );
                $func$;
                REVOKE ALL ON FUNCTION public.pcp_puede_subir_portada_pagina(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION public.pcp_puede_subir_portada_pagina(text) TO authenticated;
                CREATE POLICY pcp_portadas_insertar_imagen ON storage.objects FOR INSERT TO authenticated
                    WITH CHECK (bucket_id='pcp-portadas' AND public.pcp_puede_subir_portada_pagina(name));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS pcp_portadas_insertar_imagen ON storage.objects;
                DROP FUNCTION IF EXISTS public.pcp_puede_subir_portada_pagina(text);
                """);
            // Conservar bucket y archivos públicos durante una reversión.
            migrationBuilder.DropTable(
                name: "CargasPortadaPagina");

            migrationBuilder.DropTable(
                name: "PortadasPagina");
        }
    }
}
