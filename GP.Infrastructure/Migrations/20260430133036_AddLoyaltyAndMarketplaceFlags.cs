using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyAndMarketplaceFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_distance_traveled",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "loyalty_points_balance",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsMarketplacePurchase",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "loyalty_points_balance",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsMarketplacePurchase",
                table: "Bookings");

            migrationBuilder.AddColumn<decimal>(
                name: "total_distance_traveled",
                table: "users",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
