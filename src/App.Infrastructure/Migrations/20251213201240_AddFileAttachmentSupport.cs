using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileAttachmentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_Users_UploadedByStaffId",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "TicketAttachments");

            migrationBuilder.RenameColumn(
                name: "UploadedByStaffId",
                table: "TicketAttachments",
                newName: "UploadedByUserId");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "TicketAttachments",
                newName: "DisplayName");

            migrationBuilder.RenameIndex(
                name: "IX_TicketAttachments_UploadedByStaffId",
                table: "TicketAttachments",
                newName: "IX_TicketAttachments_UploadedByUserId");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TicketAttachments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MediaItemId",
                table: "TicketAttachments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ContactAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactAttachments_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactAttachments_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactAttachments_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactAttachments_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_MediaItemId",
                table: "TicketAttachments",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_ContactId",
                table: "ContactAttachments",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_CreatorUserId",
                table: "ContactAttachments",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_LastModifierUserId",
                table: "ContactAttachments",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_MediaItemId",
                table: "ContactAttachments",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAttachments_UploadedByUserId",
                table: "ContactAttachments",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_MediaItems_MediaItemId",
                table: "TicketAttachments",
                column: "MediaItemId",
                principalTable: "MediaItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_Users_UploadedByUserId",
                table: "TicketAttachments",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_MediaItems_MediaItemId",
                table: "TicketAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_Users_UploadedByUserId",
                table: "TicketAttachments");

            migrationBuilder.DropTable(
                name: "ContactAttachments");

            migrationBuilder.DropIndex(
                name: "IX_TicketAttachments_MediaItemId",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "MediaItemId",
                table: "TicketAttachments");

            migrationBuilder.RenameColumn(
                name: "UploadedByUserId",
                table: "TicketAttachments",
                newName: "UploadedByStaffId");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "TicketAttachments",
                newName: "FileName");

            migrationBuilder.RenameIndex(
                name: "IX_TicketAttachments_UploadedByUserId",
                table: "TicketAttachments",
                newName: "IX_TicketAttachments_UploadedByStaffId");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "TicketAttachments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "TicketAttachments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "TicketAttachments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_Users_UploadedByStaffId",
                table: "TicketAttachments",
                column: "UploadedByStaffId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
