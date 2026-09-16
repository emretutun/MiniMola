using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketAssetFeaturedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "MarketAssets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsFeatured",
                value: true);

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsFeatured",
                value: true);

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsFeatured",
                value: true);

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsFeatured",
                value: true);

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsFeatured",
                value: true);

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsFeatured",
                value: true);

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: 7,
                column: "IsFeatured",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssets_IsActive_IsFeatured",
                table: "MarketAssets",
                columns: new[] { "IsActive", "IsFeatured" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketAssets_IsActive_IsFeatured",
                table: "MarketAssets");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "MarketAssets");
        }
    }
}
