using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPasaditaWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddSmtpSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmtpEmail",
                table: "configuracion_tienda",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "configuracion_tienda",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpPassword",
                table: "configuracion_tienda",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "configuracion_tienda",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmtpEmail",
                table: "configuracion_tienda");

            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "configuracion_tienda");

            migrationBuilder.DropColumn(
                name: "SmtpPassword",
                table: "configuracion_tienda");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "configuracion_tienda");
        }
    }
}
