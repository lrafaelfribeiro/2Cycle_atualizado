using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSuggestedRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedRoutes_Users_UserId",
                table: "SuggestedRoutes");

            migrationBuilder.DropIndex(
                name: "IX_SuggestedRoutes_UserId_CreatedAt",
                table: "SuggestedRoutes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SuggestedRoutes");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "SuggestedRoutes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserSuggestedRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestedRouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSuggestedRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSuggestedRoutes_SuggestedRoutes_SuggestedRouteId",
                        column: x => x.SuggestedRouteId,
                        principalTable: "SuggestedRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSuggestedRoutes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedRoutes_CreatedByUserId",
                table: "SuggestedRoutes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSuggestedRoutes_SuggestedRouteId",
                table: "UserSuggestedRoutes",
                column: "SuggestedRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSuggestedRoutes_UserId_SuggestedRouteId",
                table: "UserSuggestedRoutes",
                columns: new[] { "UserId", "SuggestedRouteId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestedRoutes_Users_CreatedByUserId",
                table: "SuggestedRoutes",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedRoutes_Users_CreatedByUserId",
                table: "SuggestedRoutes");

            migrationBuilder.DropTable(
                name: "UserSuggestedRoutes");

            migrationBuilder.DropIndex(
                name: "IX_SuggestedRoutes_CreatedByUserId",
                table: "SuggestedRoutes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "SuggestedRoutes");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SuggestedRoutes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedRoutes_UserId_CreatedAt",
                table: "SuggestedRoutes",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestedRoutes_Users_UserId",
                table: "SuggestedRoutes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
