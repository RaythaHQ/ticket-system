using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwningTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DependsOnTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleterUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketTasks_Teams_OwningTeamId",
                        column: x => x.OwningTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketTasks_TicketTasks_DependsOnTaskId",
                        column: x => x.DependsOnTaskId,
                        principalTable: "TicketTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketTasks_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketTasks_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketTasks_Users_ClosedByStaffId",
                        column: x => x.ClosedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketTasks_Users_CreatedByStaffId",
                        column: x => x.CreatedByStaffId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketTasks_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketTasks_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_AssigneeId",
                table: "TicketTasks",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_ClosedByStaffId",
                table: "TicketTasks",
                column: "ClosedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_CreatedByStaffId",
                table: "TicketTasks",
                column: "CreatedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_CreatorUserId",
                table: "TicketTasks",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_DependsOnTaskId",
                table: "TicketTasks",
                column: "DependsOnTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_DueAt",
                table: "TicketTasks",
                column: "DueAt",
                filter: "\"DueAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_LastModifierUserId",
                table: "TicketTasks",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_OwningTeamId",
                table: "TicketTasks",
                column: "OwningTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_Status",
                table: "TicketTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTasks_TicketId",
                table: "TicketTasks",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketTasks");
        }
    }
}
