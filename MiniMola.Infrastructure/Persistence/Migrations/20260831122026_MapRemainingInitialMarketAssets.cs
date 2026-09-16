using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapRemainingInitialMarketAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { "YAHOO_FINANCE", "XU100.IS" });

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { "YAHOO_FINANCE", "XU030.IS" });

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { "COINGECKO", "pax-gold" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { null, null });
        }
    }
}
