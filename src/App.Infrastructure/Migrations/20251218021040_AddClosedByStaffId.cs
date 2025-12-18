using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClosedByStaffId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByStaffId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ClosedByStaffId",
                table: "Tickets",
                column: "ClosedByStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_ClosedByStaffId",
                table: "Tickets",
                column: "ClosedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_ClosedByStaffId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ClosedByStaffId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ClosedByStaffId",
                table: "Tickets");
        }
    }
}
