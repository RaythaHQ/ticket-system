using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the TicketUnsnoozed email template if it doesn't exist
            // This template was added with the snooze feature but only gets created during InitialSetup
            // Existing production environments need this migration to add it
            migrationBuilder.Sql(@"
                INSERT INTO ""EmailTemplates"" (""Id"", ""DeveloperName"", ""Subject"", ""Content"", ""IsBuiltInTemplate"", ""Cc"", ""Bcc"", ""CreationTime"")
                SELECT 
                    gen_random_uuid(),
                    'email_ticket_unsnoozed',
                    '[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} has been unsnoozed',
                    '<p>Hello {{ Target.RecipientName }},</p>

<p>Ticket <strong>#{{ Target.TicketId }}</strong> has been unsnoozed and is now active again.</p>

<table style=""border-collapse: collapse; width: 100%; margin: 20px 0;"">
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold; width: 150px;"">Ticket ID</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">#{{ Target.TicketId }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Title</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.TicketTitle }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Unsnoozed</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{% if Target.WasAutoUnsnooze %}Automatically (snooze duration expired){% else %}By {{ Target.UnsnoozedByName }}{% endif %}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Current Status</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.Status }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Assignee</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AssigneeName }}</td>
    </tr>
</table>

<p><a href=""{{ Target.TicketUrl }}"" style=""background-color: #0d6efd; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">View Ticket</a></p>

<p>Thank you,<br>{{ CurrentOrganization.OrganizationName }}</p>',
                    true,
                    NULL,
                    NULL,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""EmailTemplates"" WHERE ""DeveloperName"" = 'email_ticket_unsnoozed'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the template if it was added by this migration
            migrationBuilder.Sql(@"
                DELETE FROM ""EmailTemplates"" 
                WHERE ""DeveloperName"" = 'email_ticket_unsnoozed'
                AND ""IsBuiltInTemplate"" = true;
            ");
        }
    }
}
