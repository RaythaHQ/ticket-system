using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDryRun = table.Column<bool>(type: "boolean", nullable: false),
                    SourceMediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProgressStage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    RowsProcessed = table.Column<int>(type: "integer", nullable: false),
                    RowsInserted = table.Column<int>(type: "integer", nullable: false),
                    RowsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RowsSkipped = table.Column<int>(type: "integer", nullable: false),
                    RowsWithErrors = table.Column<int>(type: "integer", nullable: false),
                    ErrorMediaItemId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobs_BackgroundTasks_BackgroundTaskId",
                        column: x => x.BackgroundTaskId,
                        principalTable: "BackgroundTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ImportJobs_MediaItems_ErrorMediaItemId",
                        column: x => x.ErrorMediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ImportJobs_MediaItems_SourceMediaItemId",
                        column: x => x.SourceMediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportJobs_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ImportJobs_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ImportJobs_Users_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_BackgroundTaskId",
                table: "ImportJobs",
                column: "BackgroundTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatorUserId",
                table: "ImportJobs",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_EntityType",
                table: "ImportJobs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ErrorMediaItemId",
                table: "ImportJobs",
                column: "ErrorMediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ExpiresAt",
                table: "ImportJobs",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ExpiresAt_IsCleanedUp",
                table: "ImportJobs",
                columns: new[] { "ExpiresAt", "IsCleanedUp" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_LastModifierUserId",
                table: "ImportJobs",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_RequesterUserId",
                table: "ImportJobs",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_SourceMediaItemId",
                table: "ImportJobs",
                column: "SourceMediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                table: "ImportJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportJobs");
        }
    }
}
