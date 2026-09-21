using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateFundEstimateSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FundEstimateSnapshots_MarketAssetId_TargetDate_ModelVersion",
                table: "FundEstimateSnapshots");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "FundEstimateSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FundEstimateSnapshots_MarketAssetId_TargetDate_ModelVersion_Kind",
                table: "FundEstimateSnapshots",
                columns: new[] { "MarketAssetId", "TargetDate", "ModelVersion", "Kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM FundEstimateSnapshots
                    GROUP BY MarketAssetId, TargetDate, ModelVersion
                    HAVING COUNT(*) > 1
                )
                    THROW 51000, 'Cannot merge intraday and closing estimates on rollback. Preserve or export both records first.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_FundEstimateSnapshots_MarketAssetId_TargetDate_ModelVersion_Kind",
                table: "FundEstimateSnapshots");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "FundEstimateSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_FundEstimateSnapshots_MarketAssetId_TargetDate_ModelVersion",
                table: "FundEstimateSnapshots",
                columns: new[] { "MarketAssetId", "TargetDate", "ModelVersion" },
                unique: true);
        }
    }
}
