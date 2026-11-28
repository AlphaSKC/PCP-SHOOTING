using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GaleriaImagenesProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AgregarGaleria",
                table: "ImagenesProducto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FotosProducto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosProducto", x => x.Id);
                    table.CheckConstraint("CK_FotoProducto_Orden", "\"Orden\" BETWEEN 0 AND 7");
                    table.ForeignKey(
                        name: "FK_FotosProducto_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosProducto_ProductoId_Orden",
                table: "FotosProducto",
                columns: new[] { "ProductoId", "Orden" });
            migrationBuilder.Sql("""
                INSERT INTO "FotosProducto" ("Id","ProductoId","Url","Orden")
                SELECT gen_random_uuid(),"Id","ImagenUrl",0 FROM "Productos"
                WHERE "ImagenUrl" IS NOT NULL AND length(trim("ImagenUrl"))>0;
                ALTER TABLE "FotosProducto" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE "FotosProducto" FROM anon, authenticated;
                """);
            migrationBuilder.Sql(ProteccionFotos);
            migrationBuilder.Sql(CompatibilidadImagenPrincipal);
            migrationBuilder.Sql("""
                CREATE TRIGGER pcp_proteger_imagen_producto BEFORE INSERT OR UPDATE ON "FotosProducto"
                FOR EACH ROW EXECUTE FUNCTION pcp_proteger_imagen_producto();
                """);
        }

        // La API publicada anterior aún escribe ImagenUrl. Se concilia al COMMIT, después de los cambios de galería de la nueva API.
        public const string CompatibilidadImagenPrincipal = """
            CREATE FUNCTION pcp_sincronizar_fotos_producto() RETURNS trigger LANGUAGE plpgsql SET search_path FROM CURRENT AS $fn$
            DECLARE actual text; principal uuid; principal_url text; elegida uuid; posicion integer;
            BEGIN
                SELECT "ImagenUrl" INTO actual FROM "Productos" WHERE "Id"=NEW."Id";
                IF NOT FOUND THEN RETURN NULL; END IF;
                IF actual IS NULL OR length(trim(actual))=0 THEN
                    IF EXISTS(SELECT 1 FROM "FotosProducto" WHERE "ProductoId"=NEW."Id") THEN
                        RAISE EXCEPTION 'Conserva una imagen principal' USING ERRCODE='23514',CONSTRAINT='CK_FotoProducto_Principal';
                    END IF;
                    RETURN NULL;
                END IF;
                SELECT "Id","Url" INTO principal,principal_url FROM "FotosProducto"
                    WHERE "ProductoId"=NEW."Id" ORDER BY "Orden","Id" LIMIT 1;
                IF principal_url=actual THEN RETURN NULL; END IF;
                SELECT "Id","Orden" INTO elegida,posicion FROM "FotosProducto"
                    WHERE "ProductoId"=NEW."Id" AND "Url"=actual ORDER BY "Orden","Id" LIMIT 1;
                IF elegida IS NULL THEN
                    DELETE FROM "FotosProducto" WHERE "Id"=principal;
                    INSERT INTO "FotosProducto"("Id","ProductoId","Url","Orden") VALUES(gen_random_uuid(),NEW."Id",actual,0);
                ELSE
                    UPDATE "FotosProducto" SET "Orden"="Orden"+1 WHERE "ProductoId"=NEW."Id" AND "Orden"<posicion;
                    UPDATE "FotosProducto" SET "Orden"=0 WHERE "Id"=elegida;
                END IF;
                RETURN NULL;
            END $fn$;
            CREATE CONSTRAINT TRIGGER pcp_sincronizar_fotos_producto AFTER INSERT OR UPDATE ON "Productos"
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION pcp_sincronizar_fotos_producto();
            REVOKE ALL ON FUNCTION pcp_sincronizar_fotos_producto() FROM PUBLIC;
            """;

        public static string ProteccionFotos => CuerpoProteccion()
            .Replace("'CategoriasTienda','FotosGaleria'", "'CategoriasTienda','FotosProducto','FotosGaleria'")
            .Replace("CASE WHEN tabla='Productos' THEN 'AND t.\"Id\"<>$2' ELSE '' END",
                "CASE WHEN tabla='Productos' THEN 'AND t.\"Id\"<>$2' WHEN tabla='FotosProducto' THEN 'AND t.\"ProductoId\"<>$2' ELSE '' END");

        private static string CuerpoProteccion()
        {
            var sql=EliminacionSeguraProductos.ProteccionReferencias;
            return sql[sql.IndexOf("CREATE FUNCTION pcp_proteger_imagen_producto",StringComparison.Ordinal)..sql.IndexOf("DO $block$",StringComparison.Ordinal)]
                .Replace("CREATE FUNCTION pcp_proteger_imagen_producto", "CREATE OR REPLACE FUNCTION pcp_proteger_imagen_producto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER pcp_sincronizar_fotos_producto ON \"Productos\"; DROP FUNCTION pcp_sincronizar_fotos_producto();");
            migrationBuilder.Sql(CuerpoProteccion());
            migrationBuilder.DropTable(
                name: "FotosProducto");

            migrationBuilder.DropColumn(
                name: "AgregarGaleria",
                table: "ImagenesProducto");
        }
    }
}
