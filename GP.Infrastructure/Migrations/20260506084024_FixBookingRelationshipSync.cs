using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBookingRelationshipSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_TripOccurrences_TripOccurrenceId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TripOccurrenceId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripOccurrenceId",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TripOccurrenceId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TripOccurrenceId",
                table: "Bookings",
                column: "TripOccurrenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_TripOccurrences_TripOccurrenceId",
                table: "Bookings",
                column: "TripOccurrenceId",
                principalTable: "TripOccurrences",
                principalColumn: "TripOccurrenceId");
        }
    }
}
