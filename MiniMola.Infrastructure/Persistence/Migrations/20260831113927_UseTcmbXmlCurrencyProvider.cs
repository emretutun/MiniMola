using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseTcmbXmlCurrencyProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { "TCMB_XML", "USD" });

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { "TCMB_XML", "EUR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { "TCMB_EVDS", "TP.DK.USD.A.YTL" });

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "DataProviderCode", "ProviderSymbol" },
                values: new object[] { "TCMB_EVDS", "TP.DK.EUR.A.YTL" });
        }
    }
}
