using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrestazioniReportsOutcomeAndBookingConstraintcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_SlotId",
                schema: "public",
                table: "Bookings");

            migrationBuilder.AddColumn<Guid>(
                name: "PrestazioneId",
                schema: "public",
                table: "Slots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorRole",
                schema: "public",
                table: "Reports",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorSub",
                schema: "public",
                table: "Reports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DocumentDate",
                schema: "public",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportType",
                schema: "public",
                table: "Reports",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedAt",
                schema: "public",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                schema: "public",
                table: "AuditLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Prestazioni",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestazioni", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Slots_PrestazioneId",
                schema: "public",
                table: "Slots",
                column: "PrestazioneId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SlotId",
                schema: "public",
                table: "Bookings",
                column: "SlotId",
                unique: true,
                filter: "\"Status\" <> 'cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_Prestazioni_Name",
                schema: "public",
                table: "Prestazioni",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Slots_Prestazioni_PrestazioneId",
                schema: "public",
                table: "Slots",
                column: "PrestazioneId",
                principalSchema: "public",
                principalTable: "Prestazioni",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Slots_Prestazioni_PrestazioneId",
                schema: "public",
                table: "Slots");

            migrationBuilder.DropTable(
                name: "Prestazioni",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Slots_PrestazioneId",
                schema: "public",
                table: "Slots");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_SlotId",
                schema: "public",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PrestazioneId",
                schema: "public",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "AuthorRole",
                schema: "public",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "AuthorSub",
                schema: "public",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "DocumentDate",
                schema: "public",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReportType",
                schema: "public",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                schema: "public",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Outcome",
                schema: "public",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SlotId",
                schema: "public",
                table: "Bookings",
                column: "SlotId");
        }
    }
}
