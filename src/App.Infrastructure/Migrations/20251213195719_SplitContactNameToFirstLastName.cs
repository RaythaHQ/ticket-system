using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitContactNameToFirstLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new columns (FirstName as nullable initially)
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Contacts",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Contacts",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            // Step 2: Migrate data - split Name on first space into FirstName and LastName
            // If no space, the entire Name becomes FirstName
            migrationBuilder.Sql(@"
                UPDATE ""Contacts""
                SET 
                    ""FirstName"" = CASE 
                        WHEN POSITION(' ' IN ""Name"") > 0 THEN LEFT(""Name"", POSITION(' ' IN ""Name"") - 1)
                        ELSE ""Name""
                    END,
                    ""LastName"" = CASE 
                        WHEN POSITION(' ' IN ""Name"") > 0 THEN SUBSTRING(""Name"" FROM POSITION(' ' IN ""Name"") + 1)
                        ELSE NULL
                    END
            ");

            // Step 3: Make FirstName non-nullable now that data is migrated
            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Contacts",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            // Step 4: Drop the old Name column and its index
            migrationBuilder.DropIndex(
                name: "IX_Contacts_Name",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Contacts");

            // Step 5: Create new indexes
            migrationBuilder.CreateIndex(
                name: "IX_Contacts_FirstName",
                table: "Contacts",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_LastName",
                table: "Contacts",
                column: "LastName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the Name column
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Contacts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Migrate data back - combine FirstName and LastName into Name
            migrationBuilder.Sql(@"
                UPDATE ""Contacts""
                SET ""Name"" = CASE 
                    WHEN ""LastName"" IS NOT NULL AND ""LastName"" <> '' THEN ""FirstName"" || ' ' || ""LastName""
                    ELSE ""FirstName""
                END
            ");

            // Make Name non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Contacts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            // Drop new indexes
            migrationBuilder.DropIndex(
                name: "IX_Contacts_FirstName",
                table: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_LastName",
                table: "Contacts");

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Contacts");

            // Recreate Name index
            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Name",
                table: "Contacts",
                column: "Name");
        }
    }
}
