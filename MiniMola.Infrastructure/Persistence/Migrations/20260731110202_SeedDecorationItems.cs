using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDecorationItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DecorationItems",
                columns: new[] { "Id", "AssetKey", "Category", "CreatedAtUtc", "Description", "DisplayScale", "IsActive", "Name", "Price", "RequiredAquariumLevel", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, "curved-water-plant", "Plant", new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Akvaryuma doğal ve sakin bir görünüm kazandırır.", 1f, true, "Kıvrımlı Su Bitkisi", 100, 1, null },
                    { 2, "pink-coral", "Coral", new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Akvaryumun tabanına canlı bir renk katar.", 0.9f, true, "Pembe Mercan", 160, 1, null },
                    { 3, "volcanic-rock", "Rock", new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Balıkların çevresinde dolaşabileceği koyu renkli kaya.", 1.1f, true, "Volkan Taşı", 140, 1, null },
                    { 4, "treasure-chest", "Ornament", new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Denizin dibinde unutulmuş küçük bir hazine.", 0.85f, true, "Hazine Sandığı", 260, 1, null },
                    { 5, "mini-lighthouse", "Structure", new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Akvaryumuna kıyı kasabası havası veren özel yapı.", 1f, true, "Mini Deniz Feneri", 500, 2, null },
                    { 6, "moon-light", "Lighting", new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Akvaryuma yumuşak ve gizemli bir gece ışığı verir.", 1f, true, "Ay Işığı Lambası", 750, 3, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DecorationItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DecorationItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "DecorationItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "DecorationItems",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "DecorationItems",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "DecorationItems",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
