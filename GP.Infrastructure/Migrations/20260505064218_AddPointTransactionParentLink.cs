using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointTransactionParentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentTransactionId",
                table: "PointTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_ParentTransactionId",
                table: "PointTransactions",
                column: "ParentTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PointTransactions_PointTransactions_ParentTransactionId",
                table: "PointTransactions",
                column: "ParentTransactionId",
                principalTable: "PointTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointTransactions_PointTransactions_ParentTransactionId",
                table: "PointTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PointTransactions_ParentTransactionId",
                table: "PointTransactions");

            migrationBuilder.DropColumn(
                name: "ParentTransactionId",
                table: "PointTransactions");
        }
    }
}
