using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookedPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BookedPrice",
                schema: "public",
                table: "Bookings",
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
                name: "BookedPrice",
                schema: "public",
                table: "Bookings");
        }
    }
}
