using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketWatchlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetType = table.Column<int>(type: "int", nullable: false),
                    MarketCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuoteCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAssets", x => x.Id);
                    table.CheckConstraint("CK_MarketAssets_AssetType", "[AssetType] >= 1 AND [AssetType] <= 10");
                });

            migrationBuilder.CreateTable(
                name: "MarketPriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketAssetId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    DailyChangePercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    PriceKind = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPriceSnapshots", x => x.Id);
                    table.CheckConstraint("CK_MarketPriceSnapshots_DailyChangePercent", "[DailyChangePercent] IS NULL OR [DailyChangePercent] >= -100");
                    table.CheckConstraint("CK_MarketPriceSnapshots_Price", "[Price] > 0");
                    table.CheckConstraint("CK_MarketPriceSnapshots_PriceKind", "[PriceKind] >= 1 AND [PriceKind] <= 4");
                    table.ForeignKey(
                        name: "FK_MarketPriceSnapshots_MarketAssets_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFavoriteAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    MarketAssetId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteAssets", x => x.Id);
                    table.CheckConstraint("CK_UserFavoriteAssets_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_UserFavoriteAssets_MarketAssets_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserFavoriteAssets_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssets_AssetType_IsActive",
                table: "MarketAssets",
                columns: new[] { "AssetType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssets_MarketCode_Symbol_QuoteCurrency",
                table: "MarketAssets",
                columns: new[] { "MarketCode", "Symbol", "QuoteCurrency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketPriceSnapshots_MarketAssetId_ObservedAtUtc_PriceKind_Source",
                table: "MarketPriceSnapshots",
                columns: new[] { "MarketAssetId", "ObservedAtUtc", "PriceKind", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteAssets_MarketAssetId",
                table: "UserFavoriteAssets",
                column: "MarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteAssets_UserProfileId_MarketAssetId",
                table: "UserFavoriteAssets",
                columns: new[] { "UserProfileId", "MarketAssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteAssets_UserProfileId_SortOrder",
                table: "UserFavoriteAssets",
                columns: new[] { "UserProfileId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketPriceSnapshots");

            migrationBuilder.DropTable(
                name: "UserFavoriteAssets");

            migrationBuilder.DropTable(
                name: "MarketAssets");
        }
    }
}
