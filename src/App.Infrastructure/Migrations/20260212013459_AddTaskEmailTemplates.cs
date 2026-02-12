using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add task notification email templates for existing (live) environments.
            // These templates are created automatically during InitialSetup for new installs,
            // but existing environments need them added via migration.
            // Notification preferences default to enabled when no row exists, so no seeding needed there.

            // 1. Task Assigned to User
            migrationBuilder.Sql(@"
                INSERT INTO ""EmailTemplates"" (""Id"", ""DeveloperName"", ""Subject"", ""Content"", ""IsBuiltInTemplate"", ""Cc"", ""Bcc"", ""CreationTime"")
                SELECT 
                    gen_random_uuid(),
                    'email_task_assigned_user',
                    '[{{ CurrentOrganization.OrganizationName }}] Task assigned to you on ticket #{{ Target.TicketId }}',
                    '<p>Hello {{ Target.RecipientName }},</p>

<p>A task has been assigned to you on ticket #{{ Target.TicketId }} - <strong>{{ Target.TicketTitle }}</strong>.</p>

<div style=""background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0;"">
  <p style=""margin: 0 0 5px 0; color: #333;"">
    <strong>Task:</strong> {{ Target.TaskTitle }}
  </p>
  {% if Target.DueAt %}
  <p style=""margin: 0; color: #6c757d;"">
    <strong>Due:</strong> {{ Target.DueAt }}
  </p>
  {% endif %}
</div>

<p>
  <a href=""{{ Target.TicketUrl }}"" style=""background-color: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">View Ticket</a>
</p>

<p>Thank you,<br/>
{{ CurrentOrganization.OrganizationName }}</p>',
                    true,
                    NULL,
                    NULL,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""EmailTemplates"" WHERE ""DeveloperName"" = 'email_task_assigned_user'
                );
            ");

            // 2. Task Assigned to Team
            migrationBuilder.Sql(@"
                INSERT INTO ""EmailTemplates"" (""Id"", ""DeveloperName"", ""Subject"", ""Content"", ""IsBuiltInTemplate"", ""Cc"", ""Bcc"", ""CreationTime"")
                SELECT 
                    gen_random_uuid(),
                    'email_task_assigned_team',
                    '[{{ CurrentOrganization.OrganizationName }}] Task assigned to {{ Target.TeamName }} on ticket #{{ Target.TicketId }}',
                    '<p>Hello {{ Target.RecipientName }},</p>

<p>A task has been assigned to your team (<strong>{{ Target.TeamName }}</strong>) on ticket #{{ Target.TicketId }} - <strong>{{ Target.TicketTitle }}</strong>.</p>

<div style=""background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0;"">
  <p style=""margin: 0 0 5px 0; color: #333;"">
    <strong>Task:</strong> {{ Target.TaskTitle }}
  </p>
  {% if Target.DueAt %}
  <p style=""margin: 0; color: #6c757d;"">
    <strong>Due:</strong> {{ Target.DueAt }}
  </p>
  {% endif %}
</div>

<p>
  <a href=""{{ Target.TicketUrl }}"" style=""background-color: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">View Ticket</a>
</p>

<p>Thank you,<br/>
{{ CurrentOrganization.OrganizationName }}</p>',
                    true,
                    NULL,
                    NULL,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""EmailTemplates"" WHERE ""DeveloperName"" = 'email_task_assigned_team'
                );
            ");

            // 3. Task Completed
            migrationBuilder.Sql(@"
                INSERT INTO ""EmailTemplates"" (""Id"", ""DeveloperName"", ""Subject"", ""Content"", ""IsBuiltInTemplate"", ""Cc"", ""Bcc"", ""CreationTime"")
                SELECT 
                    gen_random_uuid(),
                    'email_task_completed',
                    '[{{ CurrentOrganization.OrganizationName }}] Task completed on ticket #{{ Target.TicketId }}',
                    '<p>Hello {{ Target.RecipientName }},</p>

<p>A task has been completed on ticket #{{ Target.TicketId }} - <strong>{{ Target.TicketTitle }}</strong>.</p>

<div style=""background-color: #f8f9fa; border-left: 4px solid #198754; padding: 15px; margin: 20px 0;"">
  <p style=""margin: 0 0 5px 0; color: #333;"">
    <strong>Task:</strong> {{ Target.TaskTitle }}
  </p>
  <p style=""margin: 0; color: #6c757d;"">
    Completed by <strong>{{ Target.CompletedBy }}</strong>
  </p>
</div>

<p>
  <a href=""{{ Target.TicketUrl }}"" style=""background-color: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">View Ticket</a>
</p>

<p>Thank you,<br/>
{{ CurrentOrganization.OrganizationName }}</p>',
                    true,
                    NULL,
                    NULL,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""EmailTemplates"" WHERE ""DeveloperName"" = 'email_task_completed'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""EmailTemplates"" 
                WHERE ""DeveloperName"" IN ('email_task_assigned_user', 'email_task_assigned_team', 'email_task_completed')
                AND ""IsBuiltInTemplate"" = true;
            ");
        }
    }
}
