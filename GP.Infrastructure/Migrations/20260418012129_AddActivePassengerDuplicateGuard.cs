using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivePassengerDuplicateGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookingStatus",
                table: "BookingPassengers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
UPDATE bp
SET bp.BookingStatus = b.Status
FROM BookingPassengers bp
INNER JOIN Bookings b ON b.BookingId = bp.BookingId;
");

            migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER [dbo].[TR_BookingPassengers_SyncBookingStatus]
ON [dbo].[BookingPassengers]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE bp
    SET bp.BookingStatus = b.Status
    FROM dbo.BookingPassengers bp
    INNER JOIN inserted i ON i.PassengerId = bp.PassengerId
    INNER JOIN dbo.Bookings b ON b.BookingId = i.BookingId;
END;
");

            migrationBuilder.Sql(@"
CREATE OR ALTER TRIGGER [dbo].[TR_Bookings_PropagateStatusToPassengers]
ON [dbo].[Bookings]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE([Status])
        RETURN;

    UPDATE bp
    SET bp.BookingStatus = i.Status
    FROM dbo.BookingPassengers bp
    INNER JOIN inserted i ON i.BookingId = bp.BookingId;
END;
");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPassenger_UniquePassengerPerOccurrence_Active",
                table: "BookingPassengers",
                columns: new[] { "OccurrenceId", "IdNumber" },
                unique: true,
                filter: "[BookingStatus] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingPassenger_UniquePassengerPerOccurrence_Active",
                table: "BookingPassengers");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[TR_Bookings_PropagateStatusToPassengers]', N'TR') IS NOT NULL
    DROP TRIGGER [dbo].[TR_Bookings_PropagateStatusToPassengers];
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[TR_BookingPassengers_SyncBookingStatus]', N'TR') IS NOT NULL
    DROP TRIGGER [dbo].[TR_BookingPassengers_SyncBookingStatus];
");

            migrationBuilder.DropColumn(
                name: "BookingStatus",
                table: "BookingPassengers");
        }
    }
}
