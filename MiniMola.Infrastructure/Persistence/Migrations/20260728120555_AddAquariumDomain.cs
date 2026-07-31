using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAquariumDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecorationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssetKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    RequiredAquariumLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DisplayScale = table.Column<float>(type: "real", nullable: false, defaultValue: 1f),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorationItems", x => x.Id);
                    table.CheckConstraint("CK_DecorationItems_DisplayScale_Positive", "[DisplayScale] > 0");
                    table.CheckConstraint("CK_DecorationItems_Price_NonNegative", "[Price] >= 0");
                    table.CheckConstraint("CK_DecorationItems_RequiredLevel_Positive", "[RequiredAquariumLevel] >= 1");
                });

            migrationBuilder.CreateTable(
                name: "FishSpecies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssetKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    Rarity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BaseSpeed = table.Column<float>(type: "real", nullable: false, defaultValue: 1f),
                    DisplayScale = table.Column<float>(type: "real", nullable: false, defaultValue: 1f),
                    RequiredAquariumLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FishSpecies", x => x.Id);
                    table.CheckConstraint("CK_FishSpecies_BaseSpeed_Positive", "[BaseSpeed] > 0");
                    table.CheckConstraint("CK_FishSpecies_DisplayScale_Positive", "[DisplayScale] > 0");
                    table.CheckConstraint("CK_FishSpecies_Price_NonNegative", "[Price] >= 0");
                    table.CheckConstraint("CK_FishSpecies_RequiredLevel_Positive", "[RequiredAquariumLevel] >= 1");
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PointBalance = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.CheckConstraint("CK_UserProfiles_PointBalance_NonNegative", "[PointBalance] >= 0");
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Aquariums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ThemeKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Capacity = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aquariums", x => x.Id);
                    table.CheckConstraint("CK_Aquariums_Capacity_Positive", "[Capacity] >= 1");
                    table.CheckConstraint("CK_Aquariums_Level_Positive", "[Level] >= 1");
                    table.ForeignKey(
                        name: "FK_Aquariums_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PointTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTransactions", x => x.Id);
                    table.CheckConstraint("CK_PointTransactions_Amount_NotZero", "[Amount] <> 0");
                    table.CheckConstraint("CK_PointTransactions_BalanceAfter_NonNegative", "[BalanceAfter] >= 0");
                    table.ForeignKey(
                        name: "FK_PointTransactions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDecorations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    DecorationItemId = table.Column<int>(type: "int", nullable: false),
                    AquariumId = table.Column<int>(type: "int", nullable: true),
                    PositionX = table.Column<float>(type: "real", nullable: false),
                    PositionY = table.Column<float>(type: "real", nullable: false),
                    ZIndex = table.Column<int>(type: "int", nullable: false),
                    Rotation = table.Column<float>(type: "real", nullable: false),
                    Scale = table.Column<float>(type: "real", nullable: false, defaultValue: 1f),
                    AcquiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDecorations", x => x.Id);
                    table.CheckConstraint("CK_UserDecorations_PositionX_Range", "[PositionX] >= 0 AND [PositionX] <= 1");
                    table.CheckConstraint("CK_UserDecorations_PositionY_Range", "[PositionY] >= 0 AND [PositionY] <= 1");
                    table.CheckConstraint("CK_UserDecorations_Scale_Positive", "[Scale] > 0");
                    table.ForeignKey(
                        name: "FK_UserDecorations_Aquariums_AquariumId",
                        column: x => x.AquariumId,
                        principalTable: "Aquariums",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserDecorations_DecorationItems_DecorationItemId",
                        column: x => x.DecorationItemId,
                        principalTable: "DecorationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDecorations_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFish",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    FishSpeciesId = table.Column<int>(type: "int", nullable: false),
                    AquariumId = table.Column<int>(type: "int", nullable: true),
                    Nickname = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ColorVariantKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcquiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFish", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFish_Aquariums_AquariumId",
                        column: x => x.AquariumId,
                        principalTable: "Aquariums",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserFish_FishSpecies_FishSpeciesId",
                        column: x => x.FishSpeciesId,
                        principalTable: "FishSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserFish_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aquariums_UserProfileId",
                table: "Aquariums",
                column: "UserProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecorationItems_AssetKey",
                table: "DecorationItems",
                column: "AssetKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FishSpecies_AssetKey",
                table: "FishSpecies",
                column: "AssetKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_UserProfileId_CreatedAtUtc",
                table: "PointTransactions",
                columns: new[] { "UserProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_UserProfileId_ReferenceId",
                table: "PointTransactions",
                columns: new[] { "UserProfileId", "ReferenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDecorations_AquariumId",
                table: "UserDecorations",
                column: "AquariumId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDecorations_DecorationItemId",
                table: "UserDecorations",
                column: "DecorationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDecorations_UserProfileId",
                table: "UserDecorations",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFish_AquariumId",
                table: "UserFish",
                column: "AquariumId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFish_FishSpeciesId",
                table: "UserFish",
                column: "FishSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFish_UserProfileId",
                table: "UserFish",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_IdentityUserId",
                table: "UserProfiles",
                column: "IdentityUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointTransactions");

            migrationBuilder.DropTable(
                name: "UserDecorations");

            migrationBuilder.DropTable(
                name: "UserFish");

            migrationBuilder.DropTable(
                name: "DecorationItems");

            migrationBuilder.DropTable(
                name: "Aquariums");

            migrationBuilder.DropTable(
                name: "FishSpecies");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
