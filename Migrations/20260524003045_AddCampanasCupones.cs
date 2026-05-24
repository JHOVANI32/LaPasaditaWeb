using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LaPasaditaWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCampanasCupones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRegistro",
                table: "usuarios",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaUltimaActividad",
                table: "sesiones_invitados",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "sesiones_invitados",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaInicio",
                table: "promociones",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaFin",
                table: "promociones",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaPedido",
                table: "pedidos",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "CampanaCuponId",
                table: "pedidos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuponGeneradoId",
                table: "pedidos",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCambio",
                table: "historial_precios",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCambio",
                table: "historial_estados",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaExpiracion",
                table: "cupones",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaAgregado",
                table: "carrito_temporal",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCalificacion",
                table: "calificaciones_productos",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "campanas_cupones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    MontoMinimo = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TipoRecompensa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValorRecompensaFija = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ValoresSorpresa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LimiteDiario = table.Column<int>(type: "integer", nullable: false),
                    LimiteEarlyBird = table.Column<int>(type: "integer", nullable: false),
                    CuponesGeneradosHoy = table.Column<int>(type: "integer", nullable: false),
                    FechaUltimoReinicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MensajeBanner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campanas_cupones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_CampanaCuponId",
                table: "pedidos",
                column: "CampanaCuponId");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_CuponGeneradoId",
                table: "pedidos",
                column: "CuponGeneradoId");

            migrationBuilder.AddForeignKey(
                name: "FK_pedidos_campanas_cupones_CampanaCuponId",
                table: "pedidos",
                column: "CampanaCuponId",
                principalTable: "campanas_cupones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_pedidos_cupones_CuponGeneradoId",
                table: "pedidos",
                column: "CuponGeneradoId",
                principalTable: "cupones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pedidos_campanas_cupones_CampanaCuponId",
                table: "pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_pedidos_cupones_CuponGeneradoId",
                table: "pedidos");

            migrationBuilder.DropTable(
                name: "campanas_cupones");

            migrationBuilder.DropIndex(
                name: "IX_pedidos_CampanaCuponId",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "IX_pedidos_CuponGeneradoId",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "CampanaCuponId",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "CuponGeneradoId",
                table: "pedidos");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRegistro",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaUltimaActividad",
                table: "sesiones_invitados",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "sesiones_invitados",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaInicio",
                table: "promociones",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaFin",
                table: "promociones",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaPedido",
                table: "pedidos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCambio",
                table: "historial_precios",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCambio",
                table: "historial_estados",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaExpiracion",
                table: "cupones",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaAgregado",
                table: "carrito_temporal",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCalificacion",
                table: "calificaciones_productos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }
    }
}
