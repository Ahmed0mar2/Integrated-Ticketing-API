using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDiscountRulesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountRules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscountRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetTrips = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_IsActive",
                table: "DiscountRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_TargetTrips",
                table: "DiscountRules",
                column: "TargetTrips");
        }
    }
}
