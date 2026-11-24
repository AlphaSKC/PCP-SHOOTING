using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImagenesProductos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImagenesProducto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ruta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TipoContenido = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Tamano = table.Column<long>(type: "bigint", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    CreadaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubidaEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagenesProducto", x => x.Id);
                    table.CheckConstraint("CK_Imagen_Tamano", "\"Tamano\" > 0 AND \"Tamano\" <= 5242880");
                    table.ForeignKey(
                        name: "FK_ImagenesProducto_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesProducto_ProductoId",
                table: "ImagenesProducto",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesProducto_Ruta",
                table: "ImagenesProducto",
                column: "Ruta",
                unique: true);
            migrationBuilder.Sql("""
                INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
                VALUES ('pcp-productos', 'pcp-productos', true, 5242880,
                    ARRAY['image/jpeg','image/png','image/webp']::text[])
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE public."ImagenesProducto" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE public."ImagenesProducto" FROM anon, authenticated;

                CREATE FUNCTION public.pcp_puede_subir_imagen_producto(nombre text)
                RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = ''
                AS $func$
                    SELECT EXISTS (
                        SELECT 1 FROM public."ImagenesProducto" i
                        JOIN public."Productos" p ON p."Id" = i."ProductoId"
                        JOIN public."PerfilesUsuario" u ON u."Id" = auth.uid()
                        WHERE i."Ruta" = nombre AND i."Disponible" = false AND u."Activo"
                            AND (u."Rol" = 1 OR (u."Rol" = 2 AND p."Categoria" = 1))
                    );
                $func$;
                REVOKE ALL ON FUNCTION public.pcp_puede_subir_imagen_producto(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION public.pcp_puede_subir_imagen_producto(text) TO authenticated;

                CREATE POLICY pcp_productos_insertar_imagen ON storage.objects
                    FOR INSERT TO authenticated
                    WITH CHECK (bucket_id = 'pcp-productos'
                        AND public.pcp_puede_subir_imagen_producto(name));
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS pcp_productos_insertar_imagen ON storage.objects;
                DROP FUNCTION IF EXISTS public.pcp_puede_subir_imagen_producto(text);
                """);
            // El bucket y sus archivos se conservan durante un rollback.

            migrationBuilder.DropTable(
                name: "ImagenesProducto");
        }
    }
}
