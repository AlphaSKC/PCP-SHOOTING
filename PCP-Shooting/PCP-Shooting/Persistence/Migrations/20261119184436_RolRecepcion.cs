using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCP_Shooting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RolRecepcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Perfil_Rol",
                table: "PerfilesUsuario");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Perfil_Rol",
                table: "PerfilesUsuario",
                sql: "\"Rol\" IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Perfil_Rol",
                table: "PerfilesUsuario");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Perfil_Rol",
                table: "PerfilesUsuario",
                sql: "\"Rol\" IN (0, 1)");
        }
    }
}
