using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffBlockOutTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffBlockOutTimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAllDay = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffBlockOutTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffBlockOutTimes_SchedulerStaffMembers_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalTable: "SchedulerStaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffBlockOutTimes_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffBlockOutTimes_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffBlockOutTimes_CreatorUserId",
                table: "StaffBlockOutTimes",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBlockOutTimes_LastModifierUserId",
                table: "StaffBlockOutTimes",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBlockOutTimes_StaffMemberId",
                table: "StaffBlockOutTimes",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffBlockOutTimes_StaffMemberId_StartTimeUtc_EndTimeUtc",
                table: "StaffBlockOutTimes",
                columns: new[] { "StaffMemberId", "StartTimeUtc", "EndTimeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffBlockOutTimes");
        }
    }
}
