using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialMarketAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MarketAssets",
                columns: new[] { "Id", "AssetType", "CreatedAtUtc", "DataProviderCode", "IsActive", "MarketCode", "Name", "ProviderSymbol", "QuoteCurrency", "Symbol", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, 5, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "COINGECKO", true, "CRYPTO", "Bitcoin", "bitcoin", "TRY", "BTC", null },
                    { 2, 5, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "COINGECKO", true, "CRYPTO", "Ethereum", "ethereum", "TRY", "ETH", null },
                    { 3, 4, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "BIST", "BIST 100", null, "TRY", "XU100", null },
                    { 4, 4, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "BIST", "BIST 30", null, "TRY", "XU030", null },
                    { 5, 6, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "PRECIOUS_METAL", "Gram Altın", null, "TRY", "GRAM_ALTIN", null },
                    { 6, 7, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "TCMB_EVDS", true, "TCMB", "ABD Doları / Türk Lirası", "TP.DK.USD.A.YTL", "TRY", "USDTRY", null },
                    { 7, 7, new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "TCMB_EVDS", true, "TCMB", "Euro / Türk Lirası", "TP.DK.EUR.A.YTL", "TRY", "EURTRY", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
