using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniMola.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpotifyConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpotifyConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    SpotifyAccountId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SpotifyUserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProtectedAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProtectedRefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccessTokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrantedScopes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotifyConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotifyConnections_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpotifyConnections_SpotifyAccountId",
                table: "SpotifyConnections",
                column: "SpotifyAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpotifyConnections_UserProfileId",
                table: "SpotifyConnections",
                column: "UserProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpotifyConnections");
        }
    }
}
