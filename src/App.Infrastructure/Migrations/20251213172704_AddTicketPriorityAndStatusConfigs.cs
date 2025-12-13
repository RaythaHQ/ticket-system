using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketPriorityAndStatusConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketPriorityConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeveloperName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "secondary"),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPriorityConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketPriorityConfigs_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketPriorityConfigs_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketStatusConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeveloperName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "secondary"),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    StatusType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "open"),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketStatusConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketStatusConfigs_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketStatusConfigs_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_CreatorUserId",
                table: "TicketPriorityConfigs",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_DeveloperName",
                table: "TicketPriorityConfigs",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_IsActive",
                table: "TicketPriorityConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_IsDefault",
                table: "TicketPriorityConfigs",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_LastModifierUserId",
                table: "TicketPriorityConfigs",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriorityConfigs_SortOrder",
                table: "TicketPriorityConfigs",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_CreatorUserId",
                table: "TicketStatusConfigs",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_DeveloperName",
                table: "TicketStatusConfigs",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_IsActive",
                table: "TicketStatusConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_LastModifierUserId",
                table: "TicketStatusConfigs",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_SortOrder",
                table: "TicketStatusConfigs",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TicketStatusConfigs_StatusType",
                table: "TicketStatusConfigs",
                column: "StatusType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketPriorityConfigs");

            migrationBuilder.DropTable(
                name: "TicketStatusConfigs");
        }
    }
}
