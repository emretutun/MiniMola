using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyWordGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyWordPuzzles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PuzzleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RewardPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 6),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWordPuzzles", x => x.Id);
                    table.CheckConstraint("CK_DailyWordPuzzles_MaxAttempts_Range", "[MaxAttempts] >= 1 AND [MaxAttempts] <= 10");
                    table.CheckConstraint("CK_DailyWordPuzzles_Reward_Positive", "[RewardPoints] > 0");
                    table.CheckConstraint("CK_DailyWordPuzzles_Word_Length", "LEN([Word]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "WordGameSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    DailyWordPuzzleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RewardGranted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordGameSessions", x => x.Id);
                    table.CheckConstraint("CK_WordGameSessions_AttemptCount_NonNegative", "[AttemptCount] >= 0");
                    table.ForeignKey(
                        name: "FK_WordGameSessions_DailyWordPuzzles_DailyWordPuzzleId",
                        column: x => x.DailyWordPuzzleId,
                        principalTable: "DailyWordPuzzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WordGameSessions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WordGameGuesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WordGameSessionId = table.Column<int>(type: "int", nullable: false),
                    Guess = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ResultPattern = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordGameGuesses", x => x.Id);
                    table.CheckConstraint("CK_WordGameGuesses_AttemptNumber_Range", "[AttemptNumber] >= 1 AND [AttemptNumber] <= 10");
                    table.CheckConstraint("CK_WordGameGuesses_Guess_Length", "LEN([Guess]) = 5");
                    table.CheckConstraint("CK_WordGameGuesses_Pattern_Valid", "LEN([ResultPattern]) = 5 AND [ResultPattern] NOT LIKE '%[^012]%'");
                    table.ForeignKey(
                        name: "FK_WordGameGuesses_WordGameSessions_WordGameSessionId",
                        column: x => x.WordGameSessionId,
                        principalTable: "WordGameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DailyWordPuzzles",
                columns: new[] { "Id", "CreatedAtUtc", "Hint", "IsActive", "MaxAttempts", "PuzzleDate", "RewardPoints", "UpdatedAtUtc", "Word" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Dalgalar ve kıyılarla birlikte düşün.", true, 6, new DateOnly(2026, 7, 29), 30, null, "DENİZ" },
                    { 2, new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Utc), "MiniMola akvaryumunun sakinlerinden biri.", true, 6, new DateOnly(2026, 7, 30), 30, null, "BALIK" },
                    { 3, new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Kısa bir molada yavaşlatmak iyi gelir.", true, 6, new DateOnly(2026, 7, 31), 30, null, "NEFES" },
                    { 4, new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Sakinliğin bıraktığı güzel his.", true, 6, new DateOnly(2026, 8, 1), 30, null, "HUZUR" },
                    { 5, new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Utc), "İş molalarının klasik eşlikçisi.", true, 6, new DateOnly(2026, 8, 2), 30, null, "KAHVE" },
                    { 6, new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Gökyüzünde yavaşça süzülür.", true, 6, new DateOnly(2026, 8, 3), 30, null, "BULUT" },
                    { 7, new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Utc), "Gündüz gökyüzünün en parlak misafiri.", true, 6, new DateOnly(2026, 8, 4), 30, null, "GÜNEŞ" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWordPuzzles_PuzzleDate",
                table: "DailyWordPuzzles",
                column: "PuzzleDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordGameGuesses_WordGameSessionId_AttemptNumber",
                table: "WordGameGuesses",
                columns: new[] { "WordGameSessionId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordGameSessions_DailyWordPuzzleId",
                table: "WordGameSessions",
                column: "DailyWordPuzzleId");

            migrationBuilder.CreateIndex(
                name: "IX_WordGameSessions_UserProfileId_DailyWordPuzzleId",
                table: "WordGameSessions",
                columns: new[] { "UserProfileId", "DailyWordPuzzleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordGameGuesses");

            migrationBuilder.DropTable(
                name: "WordGameSessions");

            migrationBuilder.DropTable(
                name: "DailyWordPuzzles");
        }
    }
}
