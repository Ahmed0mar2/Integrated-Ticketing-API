using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteSearchLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RouteSearchLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginGov = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DestinationGov = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteSearchLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteSearchLogs");
        }
    }
}
