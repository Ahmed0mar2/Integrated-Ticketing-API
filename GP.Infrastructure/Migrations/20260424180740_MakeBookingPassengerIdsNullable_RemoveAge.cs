using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeBookingPassengerIdsNullable_RemoveAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingPassenger_UniquePassengerPerOccurrence_Active",
                table: "BookingPassengers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ValidAge",
                table: "BookingPassengers");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "BookingPassengers");

            migrationBuilder.AlterColumn<int>(
                name: "IdType",
                table: "BookingPassengers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "IdNumber",
                table: "BookingPassengers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_BookingPassenger_UniquePassengerPerOccurrence_Active",
                table: "BookingPassengers",
                columns: new[] { "OccurrenceId", "IdNumber" },
                unique: true,
                filter: "[BookingStatus] IN (1, 2) AND [IdNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingPassenger_UniquePassengerPerOccurrence_Active",
                table: "BookingPassengers");

            migrationBuilder.AlterColumn<int>(
                name: "IdType",
                table: "BookingPassengers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdNumber",
                table: "BookingPassengers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "BookingPassengers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BookingPassenger_UniquePassengerPerOccurrence_Active",
                table: "BookingPassengers",
                columns: new[] { "OccurrenceId", "IdNumber" },
                unique: true,
                filter: "[BookingStatus] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ValidAge",
                table: "BookingPassengers",
                sql: "[Age] >= 0");
        }
    }
}
