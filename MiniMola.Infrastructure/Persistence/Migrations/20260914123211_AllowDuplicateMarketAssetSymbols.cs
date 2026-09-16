using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateMarketAssetSymbols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketAssets_MarketCode_Symbol_QuoteCurrency",
                table: "MarketAssets");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssets_MarketCode_Symbol_QuoteCurrency",
                table: "MarketAssets",
                columns: new[] { "MarketCode", "Symbol", "QuoteCurrency" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketAssets_MarketCode_Symbol_QuoteCurrency",
                table: "MarketAssets");

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssets_MarketCode_Symbol_QuoteCurrency",
                table: "MarketAssets",
                columns: new[] { "MarketCode", "Symbol", "QuoteCurrency" },
                unique: true);
        }
    }
}
