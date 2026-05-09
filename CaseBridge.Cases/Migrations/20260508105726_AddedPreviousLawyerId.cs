using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseBridge_Cases.Migrations
{
    /// <inheritdoc />
    public partial class AddedPreviousLawyerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreviousLawyerId",
                table: "Cases",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousLawyerId",
                table: "Cases");
        }
    }
}
