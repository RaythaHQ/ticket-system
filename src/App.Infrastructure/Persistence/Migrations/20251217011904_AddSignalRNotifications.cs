using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalRNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PlaySoundOnNotification",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InAppEnabled",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaySoundOnNotification",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InAppEnabled",
                table: "NotificationPreferences");
        }
    }
}
