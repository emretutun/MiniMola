using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundPortfolioReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundPortfolioReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundMarketAssetId = table.Column<int>(type: "int", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KapNotificationId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentObjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NotificationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParserVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ParsedWeightPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    MatchedWeightPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundPortfolioReports", x => x.Id);
                    table.CheckConstraint("CK_FundPortfolioReports_MatchedWeightPercent", "[MatchedWeightPercent] >= -100 AND [MatchedWeightPercent] <= 300");
                    table.CheckConstraint("CK_FundPortfolioReports_ParsedWeightPercent", "[ParsedWeightPercent] >= -100 AND [ParsedWeightPercent] <= 300");
                    table.ForeignKey(
                        name: "FK_FundPortfolioReports_MarketAssets_FundMarketAssetId",
                        column: x => x.FundMarketAssetId,
                        principalTable: "MarketAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundPortfolioHoldings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundPortfolioReportId = table.Column<int>(type: "int", nullable: false),
                    SecuritySymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SecurityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    MatchedMarketAssetId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundPortfolioHoldings", x => x.Id);
                    table.CheckConstraint("CK_FundPortfolioHoldings_WeightPercent", "[WeightPercent] >= -100 AND [WeightPercent] <= 300");
                    table.ForeignKey(
                        name: "FK_FundPortfolioHoldings_FundPortfolioReports_FundPortfolioReportId",
                        column: x => x.FundPortfolioReportId,
                        principalTable: "FundPortfolioReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundPortfolioHoldings_MarketAssets_MatchedMarketAssetId",
                        column: x => x.MatchedMarketAssetId,
                        principalTable: "MarketAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundPortfolioHoldings_FundPortfolioReportId_SecuritySymbol",
                table: "FundPortfolioHoldings",
                columns: new[] { "FundPortfolioReportId", "SecuritySymbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundPortfolioHoldings_MatchedMarketAssetId",
                table: "FundPortfolioHoldings",
                column: "MatchedMarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FundPortfolioReports_DocumentObjectId",
                table: "FundPortfolioReports",
                column: "DocumentObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundPortfolioReports_FundMarketAssetId_ReportDate",
                table: "FundPortfolioReports",
                columns: new[] { "FundMarketAssetId", "ReportDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundPortfolioHoldings");

            migrationBuilder.DropTable(
                name: "FundPortfolioReports");
        }
    }
}
