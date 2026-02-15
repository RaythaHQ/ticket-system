using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulerContactFieldsAndMeetingLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultMeetingLink",
                table: "SchedulerStaffMembers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactAddress",
                table: "Appointments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactFirstName",
                table: "Appointments",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactLastName",
                table: "Appointments",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Appointments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultMeetingLink",
                table: "SchedulerStaffMembers");

            migrationBuilder.DropColumn(
                name: "ContactAddress",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ContactFirstName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ContactLastName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Appointments");
        }
    }
}
