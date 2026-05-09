using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardingAlertSentToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBoardingAlertSent",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBoardingAlertSent",
                table: "Bookings");
        }
    }
}
