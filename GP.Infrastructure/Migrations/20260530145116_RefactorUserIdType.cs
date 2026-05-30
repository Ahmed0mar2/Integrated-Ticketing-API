using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "id_type",
                table: "users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "id_type",
                table: "users");
        }
    }
}
