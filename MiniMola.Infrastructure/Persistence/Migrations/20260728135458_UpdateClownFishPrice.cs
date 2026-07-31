using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClownFishPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "FishSpecies",
                keyColumn: "Id",
                keyValue: 2,
                column: "Price",
                value: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "FishSpecies",
                keyColumn: "Id",
                keyValue: 2,
                column: "Price",
                value: 300);
        }
    }
}
