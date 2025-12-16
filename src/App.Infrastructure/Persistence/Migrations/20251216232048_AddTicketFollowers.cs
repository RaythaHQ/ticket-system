using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketFollowers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketFollowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    StaffAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketFollowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketFollowers_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketFollowers_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketFollowers_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketFollowers_Users_StaffAdminId",
                        column: x => x.StaffAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_CreatorUserId",
                table: "TicketFollowers",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_LastModifierUserId",
                table: "TicketFollowers",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_StaffAdminId",
                table: "TicketFollowers",
                column: "StaffAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_TicketId",
                table: "TicketFollowers",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFollowers_TicketId_StaffAdminId",
                table: "TicketFollowers",
                columns: new[] { "TicketId", "StaffAdminId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketFollowers");
        }
    }
}
