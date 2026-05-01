using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseBridge_Cases.Migrations
{
    /// <inheritdoc />
    public partial class NewFieldInChatMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FirmId",
                table: "ChatMessages",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirmId",
                table: "ChatMessages");
        }
    }
}
