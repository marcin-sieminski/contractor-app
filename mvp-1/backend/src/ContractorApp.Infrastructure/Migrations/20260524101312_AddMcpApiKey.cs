using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "McpApiKey",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """CREATE UNIQUE INDEX "IX_AspNetUsers_McpApiKey" ON "AspNetUsers" ("McpApiKey") WHERE "McpApiKey" IS NOT NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_AspNetUsers_McpApiKey"";");

            migrationBuilder.DropColumn(
                name: "McpApiKey",
                table: "AspNetUsers");
        }
    }
}
