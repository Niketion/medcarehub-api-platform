using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrestazioneBasePriceAndEconomics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                schema: "public",
                table: "Prestazioni",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasePrice",
                schema: "public",
                table: "Prestazioni");
        }
    }
}
