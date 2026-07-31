using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialFishSpecies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "FishSpecies",
                columns: new[] { "Id", "AssetKey", "BaseSpeed", "CreatedAtUtc", "Description", "DisplayScale", "IsActive", "Name", "Price", "Rarity", "RequiredAquariumLevel", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, "blue-tang", 0.85f, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Her yeni akvaryumun neşeli başlangıç balığı.", 0.9f, true, "Mavi Tang", 0, "Common", 1, null },
                    { 2, "clown-fish", 1f, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Turuncu ve beyaz çizgileriyle akvaryumun neşeli yüzü.", 0.85f, true, "Palyaço Balığı", 300, "Common", 1, null },
                    { 3, "neon-tetra", 1.25f, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Parlak renkleriyle akvaryumda ışık gibi süzülür.", 0.65f, true, "Neon Tetra", 450, "Uncommon", 1, null },
                    { 4, "betta-fish", 0.7f, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Gösterişli yüzgeçleriyle sakin ve zarif bir balık.", 1f, true, "Beta Balığı", 800, "Rare", 2, null },
                    { 5, "goldfish", 0.8f, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Akvaryumuna sıcaklık ve şans getiren özel balık.", 1.05f, true, "Altın Balık", 1200, "Rare", 2, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FishSpecies",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "FishSpecies",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "FishSpecies",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "FishSpecies",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "FishSpecies",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
