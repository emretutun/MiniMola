using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketAssetProviderMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataProviderCode",
                table: "MarketAssets",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderSymbol",
                table: "MarketAssets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssets_DataProviderCode_ProviderSymbol_QuoteCurrency",
                table: "MarketAssets",
                columns: new[] { "DataProviderCode", "ProviderSymbol", "QuoteCurrency" },
                unique: true,
                filter: "[DataProviderCode] IS NOT NULL AND [ProviderSymbol] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MarketAssets_ProviderMapping",
                table: "MarketAssets",
                sql: "([DataProviderCode] IS NULL AND [ProviderSymbol] IS NULL) OR ([DataProviderCode] IS NOT NULL AND [ProviderSymbol] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketAssets_DataProviderCode_ProviderSymbol_QuoteCurrency",
                table: "MarketAssets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MarketAssets_ProviderMapping",
                table: "MarketAssets");

            migrationBuilder.DropColumn(
                name: "DataProviderCode",
                table: "MarketAssets");

            migrationBuilder.DropColumn(
                name: "ProviderSymbol",
                table: "MarketAssets");
        }
    }
}
