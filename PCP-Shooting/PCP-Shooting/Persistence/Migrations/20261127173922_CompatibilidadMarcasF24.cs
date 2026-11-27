using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompatibilidadMarcasF24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "VisibleEnCarrusel",
                table: "MarcasAliadas",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "MarcasAliadas",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValueSql: "'marca-' || replace(gen_random_uuid()::text,'-','')",
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "VisibleEnCarrusel",
                table: "MarcasAliadas",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "MarcasAliadas",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldDefaultValueSql: "'marca-' || replace(gen_random_uuid()::text,'-','')");
        }
    }
}
