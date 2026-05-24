using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPasaditaWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "configuracion_tienda",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "configuracion_tienda");
        }
    }
}
