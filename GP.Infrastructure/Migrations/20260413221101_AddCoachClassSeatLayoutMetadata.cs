using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachClassSeatLayoutMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeckCount",
                table: "CoachClasses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "LayoutType",
                table: "CoachClasses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatMapJson",
                table: "CoachClasses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeckCount",
                table: "CoachClasses");

            migrationBuilder.DropColumn(
                name: "LayoutType",
                table: "CoachClasses");

            migrationBuilder.DropColumn(
                name: "SeatMapJson",
                table: "CoachClasses");
        }
    }
}
