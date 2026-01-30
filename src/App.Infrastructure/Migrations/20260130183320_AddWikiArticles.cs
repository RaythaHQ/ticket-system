using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWikiArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WikiArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    Excerpt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WikiArticles_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WikiArticles_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_Category",
                table: "WikiArticles",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_Category_SortOrder",
                table: "WikiArticles",
                columns: new[] { "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_CreatorUserId",
                table: "WikiArticles",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_IsPinned",
                table: "WikiArticles",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_IsPublished",
                table: "WikiArticles",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_LastModifierUserId",
                table: "WikiArticles",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiArticles_Slug",
                table: "WikiArticles",
                column: "Slug",
                unique: true);

            // Grant EditWikiArticles permission (1024) to all existing roles
            // Using bitwise OR to add the permission without removing existing ones
            migrationBuilder.Sql(
                @"UPDATE ""Roles"" SET ""SystemPermissions"" = ""SystemPermissions"" | 1024"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WikiArticles");
        }
    }
}
