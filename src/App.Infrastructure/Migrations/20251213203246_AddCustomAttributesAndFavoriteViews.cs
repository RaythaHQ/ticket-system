using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomAttributesAndFavoriteViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomAttribute1",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomAttribute2",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomAttribute3",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomAttribute4",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomAttribute5",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserFavoriteViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketViewId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFavoriteViews_TicketViews_TicketViewId",
                        column: x => x.TicketViewId,
                        principalTable: "TicketViews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavoriteViews_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserFavoriteViews_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserFavoriteViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_CreatorUserId",
                table: "UserFavoriteViews",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_LastModifierUserId",
                table: "UserFavoriteViews",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_TicketViewId",
                table: "UserFavoriteViews",
                column: "TicketViewId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_UserId",
                table: "UserFavoriteViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteViews_UserId_TicketViewId",
                table: "UserFavoriteViews",
                columns: new[] { "UserId", "TicketViewId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFavoriteViews");

            migrationBuilder.DropColumn(
                name: "CustomAttribute1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CustomAttribute2",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CustomAttribute3",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CustomAttribute4",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CustomAttribute5",
                table: "Users");
        }
    }
}
