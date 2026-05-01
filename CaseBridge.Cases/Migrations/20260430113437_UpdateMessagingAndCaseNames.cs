using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseBridge_Cases.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessagingAndCaseNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReceiverId",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LawyerName",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiverId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "LawyerName",
                table: "Cases");
        }
    }
}
