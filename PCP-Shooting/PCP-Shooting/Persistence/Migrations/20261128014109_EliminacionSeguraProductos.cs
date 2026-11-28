using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EliminacionSeguraProductos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EliminacionesProducto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Categoria = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EliminacionesProducto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchivosProductoEliminado",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EliminacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ruta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EliminadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivosProductoEliminado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivosProductoEliminado_EliminacionesProducto_Eliminacion~",
                        column: x => x.EliminacionId,
                        principalTable: "EliminacionesProducto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosProductoEliminado_EliminacionId",
                table: "ArchivosProductoEliminado",
                column: "EliminacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosProductoEliminado_Ruta",
                table: "ArchivosProductoEliminado",
                column: "Ruta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EliminacionesProducto_CompletadaEnUtc",
                table: "EliminacionesProducto",
                column: "CompletadaEnUtc");
            migrationBuilder.Sql("""
                ALTER TABLE "EliminacionesProducto" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ArchivosProductoEliminado" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "EliminacionesProducto", "ArchivosProductoEliminado" FROM anon, authenticated;
                """);
            migrationBuilder.Sql(ProteccionReferencias);
            migrationBuilder.Sql("""
                CREATE FUNCTION public.pcp_puede_eliminar_imagen_producto(nombre text)
                RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = ''
                AS $func$
                    SELECT EXISTS (
                        SELECT 1 FROM public."ArchivosProductoEliminado" a
                        JOIN public."EliminacionesProducto" e ON e."Id"=a."EliminacionId"
                        JOIN public."PerfilesUsuario" u ON u."Id"=auth.uid()
                        WHERE a."Ruta"=nombre AND a."EliminadoEnUtc" IS NULL AND u."Activo"
                            AND (u."Rol"=1 OR u."Rol"=2 AND e."Categoria"=1)
                    );
                $func$;
                REVOKE ALL ON FUNCTION public.pcp_puede_eliminar_imagen_producto(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION public.pcp_puede_eliminar_imagen_producto(text) TO authenticated;
                CREATE POLICY pcp_productos_eliminar_imagen ON storage.objects FOR DELETE TO authenticated
                    USING (bucket_id='pcp-productos' AND public.pcp_puede_eliminar_imagen_producto(name));
                CREATE POLICY pcp_productos_consultar_eliminacion ON storage.objects FOR SELECT TO authenticated
                    USING (bucket_id='pcp-productos' AND public.pcp_puede_eliminar_imagen_producto(name));
                """);
        }

        // Funciones locales al esquema: las pruebas pueden instalarlas en su esquema aislado.
        public const string ProteccionReferencias = """
            CREATE FUNCTION pcp_normalizar_referencia(texto text) RETURNS text
            LANGUAGE plpgsql IMMUTABLE SET search_path = '' AS $fn$
            DECLARE vuelta integer; codigo integer;
            BEGIN
                FOR vuelta IN 1..3 LOOP
                    FOR codigo IN 32..126 LOOP
                        texto := replace(replace(texto, '%' || lpad(to_hex(codigo),2,'0'), chr(codigo)),
                            '%' || upper(lpad(to_hex(codigo),2,'0')), chr(codigo));
                    END LOOP;
                END LOOP;
                RETURN replace(replace(texto, E'\\/', '/'), E'\\u002F', '/');
            END $fn$;
            CREATE FUNCTION pcp_proteger_imagen_producto() RETURNS trigger
            LANGUAGE plpgsql SET search_path FROM CURRENT AS $fn$
            DECLARE tabla text; compartida boolean; texto text;
            BEGIN
                -- Un mismo cerrojo para escritores de referencias y borrados; la transacción de API es SERIALIZABLE.
                PERFORM pg_advisory_xact_lock(72222026);
                IF TG_TABLE_NAME='ArchivosProductoEliminado' THEN
                    FOREACH tabla IN ARRAY ARRAY['Productos','ArticulosBlog','Eventos','MarcasAliadas',
                        'CategoriasTienda','FotosGaleria','PortadasPagina','ConfiguracionContenidoSitio','Paquetes','ReservasWeb'] LOOP
                        IF to_regclass(format('%I.%I',TG_TABLE_SCHEMA,tabla)) IS NULL THEN CONTINUE; END IF;
                        EXECUTE format('SELECT EXISTS(SELECT 1 FROM %I.%I t WHERE strpos(pcp_normalizar_referencia(to_jsonb(t)::text), $1)>0 %s)',
                            TG_TABLE_SCHEMA,tabla,CASE WHEN tabla='Productos' THEN 'AND t."Id"<>$2' ELSE '' END)
                            INTO compartida USING NEW."Ruta",NEW."EliminacionId";
                        IF compartida THEN
                            RAISE EXCEPTION 'Imagen compartida' USING ERRCODE='23514',CONSTRAINT='CK_ImagenProducto_Eliminada';
                        END IF;
                    END LOOP;
                ELSE
                    texto := pcp_normalizar_referencia(to_jsonb(NEW)::text);
                    IF EXISTS (SELECT 1 FROM "ArchivosProductoEliminado" a WHERE strpos(texto,a."Ruta")>0)
                        OR TG_TABLE_NAME='Productos' AND EXISTS(SELECT 1 FROM "EliminacionesProducto" e WHERE e."Id"=NEW."Id") THEN
                        RAISE EXCEPTION 'Imagen eliminada' USING ERRCODE='23514',CONSTRAINT='CK_ImagenProducto_Eliminada';
                    END IF;
                END IF;
                RETURN NEW;
            END $fn$;
            DO $block$
            DECLARE tabla text;
            BEGIN
                FOREACH tabla IN ARRAY ARRAY['Productos','ArticulosBlog','Eventos','MarcasAliadas',
                    'CategoriasTienda','FotosGaleria','PortadasPagina','ConfiguracionContenidoSitio','Paquetes','ReservasWeb','ArchivosProductoEliminado'] LOOP
                    IF to_regclass(format('%I.%I',current_schema(),tabla)) IS NOT NULL THEN
                        EXECUTE format('CREATE TRIGGER pcp_proteger_imagen_producto BEFORE INSERT OR UPDATE ON %I.%I FOR EACH ROW EXECUTE FUNCTION pcp_proteger_imagen_producto()',current_schema(),tabla);
                    END IF;
                END LOOP;
            END $block$;
            REVOKE ALL ON FUNCTION pcp_normalizar_referencia(text), pcp_proteger_imagen_producto() FROM PUBLIC;
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS pcp_productos_eliminar_imagen ON storage.objects;
                DROP POLICY IF EXISTS pcp_productos_consultar_eliminacion ON storage.objects;
                DROP FUNCTION IF EXISTS public.pcp_puede_eliminar_imagen_producto(text);
                DROP FUNCTION IF EXISTS pcp_proteger_imagen_producto() CASCADE;
                DROP FUNCTION IF EXISTS pcp_normalizar_referencia(text);
                """);
            migrationBuilder.DropTable(
                name: "ArchivosProductoEliminado");

            migrationBuilder.DropTable(
                name: "EliminacionesProducto");
        }
    }
}
