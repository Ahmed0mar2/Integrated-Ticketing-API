using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingsCartAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Trips_TripId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TripId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Bookings");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TripOccurrenceClassInventories",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoldExpiresAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfferedForResale",
                table: "BookingPassengers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_OriginStationId_DestinationStationId",
                table: "Trips",
                columns: new[] { "OriginStationId", "DestinationStationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TripOccurrences_IsActive_OccurrenceDate_TripId",
                table: "TripOccurrences",
                columns: new[] { "IsActive", "OccurrenceDate", "TripId" });

            migrationBuilder.CreateIndex(
                name: "IX_TripOccurrenceClassInventories_TripOccurrenceId_CoachClassId_RemainingSeats",
                table: "TripOccurrenceClassInventories",
                columns: new[] { "TripOccurrenceId", "CoachClassId", "RemainingSeats" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_HoldExpiresAt",
                table: "Bookings",
                columns: new[] { "Status", "HoldExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_OriginStationId_DestinationStationId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_TripOccurrences_IsActive_OccurrenceDate_TripId",
                table: "TripOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_TripOccurrenceClassInventories_TripOccurrenceId_CoachClassId_RemainingSeats",
                table: "TripOccurrenceClassInventories");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_HoldExpiresAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TripOccurrenceClassInventories");

            migrationBuilder.DropColumn(
                name: "HoldExpiresAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsOfferedForResale",
                table: "BookingPassengers");

            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TripId",
                table: "Bookings",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Trips_TripId",
                table: "Bookings",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "TripId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
