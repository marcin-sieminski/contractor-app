using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimerPauseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccumulatedSeconds",
                table: "TimeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaused",
                table: "TimeEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccumulatedSeconds",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "IsPaused",
                table: "TimeEntries");
        }
    }
}
