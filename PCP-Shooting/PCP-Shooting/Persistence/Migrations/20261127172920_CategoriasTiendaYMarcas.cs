using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoriasTiendaYMarcas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CargaContenido_Destino",
                table: "CargasImagenContenido");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaTiendaId",
                table: "Productos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MarcaId",
                table: "Productos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "MarcasAliadas",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "VisibleEnCarrusel",
                table: "MarcasAliadas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaTiendaId",
                table: "CargasImagenContenido",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriasTienda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ImagenUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasTienda", x => x.Id);
                    table.CheckConstraint("CK_CategoriaTienda_Nombre", "length(trim(\"Nombre\")) > 0");
                    table.CheckConstraint("CK_CategoriaTienda_Orden", "\"Orden\" >= 0");
                    table.CheckConstraint("CK_CategoriaTienda_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$' AND \"Slug\" NOT IN ('carrito','checkout','pedido','producto')");
                });

            migrationBuilder.UpdateData(
                table: "MarcasAliadas",
                keyColumn: "Id",
                keyValue: new Guid("e4ec8df0-0000-4000-8000-000000000110"),
                columns: new[] { "Slug", "VisibleEnCarrusel" },
                values: new object[] { "airguns-fv-e4ec8df0000040008000000000000110", true });

            migrationBuilder.UpdateData(
                table: "MarcasAliadas",
                keyColumn: "Id",
                keyValue: new Guid("e4ec8df0-0000-4000-8000-000000000111"),
                columns: new[] { "Slug", "VisibleEnCarrusel" },
                values: new object[] { "altaros-e4ec8df0000040008000000000000111", true });

            migrationBuilder.UpdateData(
                table: "MarcasAliadas",
                keyColumn: "Id",
                keyValue: new Guid("e4ec8df0-0000-4000-8000-000000000112"),
                columns: new[] { "Slug", "VisibleEnCarrusel" },
                values: new object[] { "zan-projectiles-e4ec8df0000040008000000000000112", true });

            migrationBuilder.UpdateData(
                table: "MarcasAliadas",
                keyColumn: "Id",
                keyValue: new Guid("e4ec8df0-0000-4000-8000-000000000113"),
                columns: new[] { "Slug", "VisibleEnCarrusel" },
                values: new object[] { "donny-fl-e4ec8df0000040008000000000000113", true });

            migrationBuilder.UpdateData(
                table: "MarcasAliadas",
                keyColumn: "Id",
                keyValue: new Guid("e4ec8df0-0000-4000-8000-000000000114"),
                columns: new[] { "Slug", "VisibleEnCarrusel" },
                values: new object[] { "aea-e4ec8df0000040008000000000000114", true });

            migrationBuilder.UpdateData(
                table: "MarcasAliadas",
                keyColumn: "Id",
                keyValue: new Guid("e4ec8df0-0000-4000-8000-000000000115"),
                columns: new[] { "Slug", "VisibleEnCarrusel" },
                values: new object[] { "h-v-e4ec8df0000040008000000000000115", true });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaTiendaId",
                table: "Productos",
                column: "CategoriaTiendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_MarcaId",
                table: "Productos",
                column: "MarcaId");

            // Backfill sobre los nombres/estados actuales; no sobrescribir datos editados de las marcas iniciales.
            // El UUID completo evita colisiones al normalizar nombres iguales o caracteres no ASCII.
            migrationBuilder.Sql("""
                UPDATE "MarcasAliadas" SET
                    "Slug" = COALESCE(NULLIF(trim(both '-' from substring(regexp_replace(
                        translate(lower("Nombre"),'áéíóúüñ','aeiouun'),'[^a-z0-9]+','-','g'),1,47)),''),'marca')
                        || '-' || replace("Id"::text,'-',''),
                    "VisibleEnCarrusel" = "Activo",
                    "Version" = gen_random_uuid();
                """);
            migrationBuilder.CreateIndex(
                name: "IX_MarcasAliadas_Slug",
                table: "MarcasAliadas",
                column: "Slug",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Marca_Slug",
                table: "MarcasAliadas",
                sql: "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");

            migrationBuilder.CreateIndex(
                name: "IX_CargasImagenContenido_CategoriaTiendaId",
                table: "CargasImagenContenido",
                column: "CategoriaTiendaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CargaContenido_Destino",
                table: "CargasImagenContenido",
                sql: "num_nonnulls(\"MarcaId\",\"ArticuloBlogId\",\"CategoriaTiendaId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasTienda_Activa_Orden",
                table: "CategoriasTienda",
                columns: new[] { "Activa", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasTienda_Slug",
                table: "CategoriasTienda",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CargasImagenContenido_CategoriasTienda_CategoriaTiendaId",
                table: "CargasImagenContenido",
                column: "CategoriaTiendaId",
                principalTable: "CategoriasTienda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CategoriasTienda_CategoriaTiendaId",
                table: "Productos",
                column: "CategoriaTiendaId",
                principalTable: "CategoriasTienda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_MarcasAliadas_MarcaId",
                table: "Productos",
                column: "MarcaId",
                principalTable: "MarcasAliadas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                ALTER TABLE "CategoriasTienda" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "CategoriasTienda" FROM anon, authenticated;
                """);
            migrationBuilder.Sql("""
                INSERT INTO storage.buckets(id,name,public,file_size_limit,allowed_mime_types)
                VALUES ('pcp-tienda','pcp-tienda',true,5242880,ARRAY['image/jpeg','image/png','image/webp']::text[])
                ON CONFLICT(id) DO NOTHING;

                CREATE FUNCTION public.pcp_puede_subir_categoria_tienda(nombre text)
                RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = ''
                AS $func$
                    SELECT EXISTS (
                        SELECT 1 FROM public."CargasImagenContenido" c
                        JOIN public."PerfilesUsuario" u ON u."Id"=auth.uid()
                        JOIN public."CategoriasTienda" t ON t."Id"=c."CategoriaTiendaId"
                        WHERE c."Ruta"=nombre AND NOT c."Disponible" AND c."CreadorId"=auth.uid()
                            AND u."Activo" AND u."Rol"=1 AND t."Version"=c."VersionDestino"
                    );
                $func$;
                REVOKE ALL ON FUNCTION public.pcp_puede_subir_categoria_tienda(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION public.pcp_puede_subir_categoria_tienda(text) TO authenticated;
                CREATE POLICY pcp_tienda_insertar_imagen ON storage.objects FOR INSERT TO authenticated
                    WITH CHECK (bucket_id='pcp-tienda' AND public.pcp_puede_subir_categoria_tienda(name));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS pcp_tienda_insertar_imagen ON storage.objects;
                DROP FUNCTION IF EXISTS public.pcp_puede_subir_categoria_tienda(text);
                DELETE FROM "CargasImagenContenido" WHERE "CategoriaTiendaId" IS NOT NULL;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_CargasImagenContenido_CategoriasTienda_CategoriaTiendaId",
                table: "CargasImagenContenido");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CategoriasTienda_CategoriaTiendaId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_MarcasAliadas_MarcaId",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "CategoriasTienda");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CategoriaTiendaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_MarcaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_MarcasAliadas_Slug",
                table: "MarcasAliadas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Marca_Slug",
                table: "MarcasAliadas");

            migrationBuilder.DropIndex(
                name: "IX_CargasImagenContenido_CategoriaTiendaId",
                table: "CargasImagenContenido");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CargaContenido_Destino",
                table: "CargasImagenContenido");

            migrationBuilder.DropColumn(
                name: "CategoriaTiendaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "MarcasAliadas");

            migrationBuilder.DropColumn(
                name: "VisibleEnCarrusel",
                table: "MarcasAliadas");

            migrationBuilder.DropColumn(
                name: "CategoriaTiendaId",
                table: "CargasImagenContenido");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CargaContenido_Destino",
                table: "CargasImagenContenido",
                sql: "(\"MarcaId\" IS NOT NULL AND \"ArticuloBlogId\" IS NULL) OR (\"MarcaId\" IS NULL AND \"ArticuloBlogId\" IS NOT NULL)");
        }
    }
}
