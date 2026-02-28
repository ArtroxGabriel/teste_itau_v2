using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItauCompraProgramada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClientAndGraphicAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Saldo",
                table: "ContasGraficas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiaExecucao",
                table: "Clientes",
                type: "int",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Saldo",
                table: "ContasGraficas");

            migrationBuilder.DropColumn(
                name: "DiaExecucao",
                table: "Clientes");
        }
    }
}