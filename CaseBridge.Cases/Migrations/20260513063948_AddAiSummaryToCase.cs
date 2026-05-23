using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseBridge_Cases.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSummaryToCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Cases");
        }
    }
}
