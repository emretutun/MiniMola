using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundEstimateHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundEstimateSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketAssetId = table.Column<int>(type: "int", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    BasePriceObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedChangePercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ConfidenceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DistributionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActualPrice = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    ActualChangePercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ActualObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AbsoluteErrorPercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundEstimateSnapshots", x => x.Id);
                    table.CheckConstraint("CK_FundEstimateSnapshots_ActualPrice", "[ActualPrice] IS NULL OR [ActualPrice] > 0");
                    table.CheckConstraint("CK_FundEstimateSnapshots_BasePrice", "[BasePrice] > 0");
                    table.CheckConstraint("CK_FundEstimateSnapshots_CoveragePercent", "[CoveragePercent] >= 0 AND [CoveragePercent] <= 100");
                    table.CheckConstraint("CK_FundEstimateSnapshots_EstimatedPrice", "[EstimatedPrice] > 0");
                    table.ForeignKey(
                        name: "FK_FundEstimateSnapshots_MarketAssets_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundEstimateSnapshots_MarketAssetId_TargetDate_ModelVersion",
                table: "FundEstimateSnapshots",
                columns: new[] { "MarketAssetId", "TargetDate", "ModelVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundEstimateSnapshots_TargetDate_EvaluatedAtUtc",
                table: "FundEstimateSnapshots",
                columns: new[] { "TargetDate", "EvaluatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundEstimateSnapshots");
        }
    }
}
