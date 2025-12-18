using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Tickets",
                type: "text",
                nullable: false,
                defaultValue: "english"
            );

            // Set all existing tickets to English
            migrationBuilder.Sql(
                "UPDATE \"Tickets\" SET \"Language\" = 'english' WHERE \"Language\" = ''"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Language", table: "Tickets");
        }
    }
}
