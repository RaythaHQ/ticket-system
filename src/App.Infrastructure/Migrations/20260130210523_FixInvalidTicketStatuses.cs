using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixInvalidTicketStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix tickets that have a Status value that doesn't match any configured TicketStatusConfig
            // This can happen when tickets are imported without specifying a status, and the system
            // defaulted to a hardcoded value that doesn't exist in the configuration.
            //
            // The fix: Update invalid status values to use the first active open status (by SortOrder)
            migrationBuilder.Sql(@"
                UPDATE ""Tickets""
                SET ""Status"" = (
                    SELECT ""DeveloperName"" 
                    FROM ""TicketStatusConfigs"" 
                    WHERE ""IsActive"" = true 
                      AND ""StatusType"" = 'open'
                    ORDER BY ""SortOrder"" ASC 
                    LIMIT 1
                )
                WHERE ""Status"" NOT IN (
                    SELECT ""DeveloperName"" FROM ""TicketStatusConfigs""
                )
                AND ""IsDeleted"" = false;
            ");
            
            // Also fix any tickets with invalid Priority values
            migrationBuilder.Sql(@"
                UPDATE ""Tickets""
                SET ""Priority"" = (
                    SELECT ""DeveloperName"" 
                    FROM ""TicketPriorityConfigs"" 
                    WHERE ""IsActive"" = true 
                    ORDER BY ""IsDefault"" DESC, ""SortOrder"" ASC 
                    LIMIT 1
                )
                WHERE ""Priority"" NOT IN (
                    SELECT ""DeveloperName"" FROM ""TicketPriorityConfigs""
                )
                AND ""IsDeleted"" = false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot reliably reverse this data migration - the original invalid values are lost
        }
    }
}
