using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementTaxpayerAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Commune",
                table: "AnnualSettlements",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "AnnualSettlements",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Voivodeship",
                table: "AnnualSettlements",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Commune",
                table: "AnnualSettlements");

            migrationBuilder.DropColumn(
                name: "County",
                table: "AnnualSettlements");

            migrationBuilder.DropColumn(
                name: "Voivodeship",
                table: "AnnualSettlements");
        }
    }
}
