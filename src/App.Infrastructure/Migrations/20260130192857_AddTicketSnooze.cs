using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketSnooze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SnoozedAt",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SnoozedById",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnoozedReason",
                table: "Tickets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SnoozedUntil",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnsnoozedAt",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SnoozedById",
                table: "Tickets",
                column: "SnoozedById");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SnoozedUntil",
                table: "Tickets",
                column: "SnoozedUntil",
                filter: "\"SnoozedUntil\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_SnoozedById",
                table: "Tickets",
                column: "SnoozedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_SnoozedById",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SnoozedById",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SnoozedUntil",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SnoozedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SnoozedById",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SnoozedReason",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SnoozedUntil",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UnsnoozedAt",
                table: "Tickets");
        }
    }
}
