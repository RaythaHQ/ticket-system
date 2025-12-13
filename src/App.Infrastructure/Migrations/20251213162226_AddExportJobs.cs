using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProgressStage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    RowCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SnapshotPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCleanedUp = table.Column<bool>(type: "boolean", nullable: false),
                    BackgroundTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportJobs_BackgroundTasks_BackgroundTaskId",
                        column: x => x.BackgroundTaskId,
                        principalTable: "BackgroundTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExportJobs_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExportJobs_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExportJobs_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExportJobs_Users_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_BackgroundTaskId",
                table: "ExportJobs",
                column: "BackgroundTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_CreatorUserId",
                table: "ExportJobs",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_ExpiresAt",
                table: "ExportJobs",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_ExpiresAt_IsCleanedUp",
                table: "ExportJobs",
                columns: new[] { "ExpiresAt", "IsCleanedUp" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_LastModifierUserId",
                table: "ExportJobs",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_MediaItemId",
                table: "ExportJobs",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_RequesterUserId",
                table: "ExportJobs",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_Status",
                table: "ExportJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportJobs");
        }
    }
}
