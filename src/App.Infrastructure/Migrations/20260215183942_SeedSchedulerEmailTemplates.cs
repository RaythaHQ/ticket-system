using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSchedulerEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed 6 default scheduler email templates (3 types × 2 channels)
            // Email templates are active; SMS templates are inactive (foundation for future)
            // Uses WHERE NOT EXISTS for idempotent re-runs

            // 1. Confirmation - Email
            migrationBuilder.Sql(@"
                INSERT INTO ""SchedulerEmailTemplates"" (""Id"", ""TemplateType"", ""Channel"", ""Subject"", ""Content"", ""IsActive"", ""CreationTime"")
                SELECT
                    gen_random_uuid(),
                    'confirmation',
                    'email',
                    '[{{ CurrentOrganization.OrganizationName }}] Appointment Confirmation — {{ Target.AppointmentCode }}',
                    '<p>Hello {{ Target.ContactName }},</p>

<p>Your appointment has been confirmed. Here are the details:</p>

<table style=""border-collapse: collapse; width: 100%; margin: 20px 0;"">
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold; width: 150px;"">Appointment</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AppointmentCode }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Type</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AppointmentType }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Date & Time</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.DateTime }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Duration</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.Duration }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Mode</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AppointmentMode }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Staff</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.StaffName }}</td>
    </tr>
    {% if Target.MeetingLink != """" %}
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Meeting Link</td>
        <td style=""padding: 8px; border: 1px solid #ddd;""><a href=""{{ Target.MeetingLink }}"">{{ Target.MeetingLink }}</a></td>
    </tr>
    {% endif %}
    {% if Target.Notes != """" %}
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Notes</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.Notes }}</td>
    </tr>
    {% endif %}
</table>

<p>Thank you,<br>{{ CurrentOrganization.OrganizationName }}</p>',
                    true,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""SchedulerEmailTemplates"" WHERE ""TemplateType"" = 'confirmation' AND ""Channel"" = 'email'
                );
            ");

            // 2. Confirmation - SMS
            migrationBuilder.Sql(@"
                INSERT INTO ""SchedulerEmailTemplates"" (""Id"", ""TemplateType"", ""Channel"", ""Subject"", ""Content"", ""IsActive"", ""CreationTime"")
                SELECT
                    gen_random_uuid(),
                    'confirmation',
                    'sms',
                    NULL,
                    'Your appointment {{ Target.AppointmentCode }} ({{ Target.AppointmentType }}) is confirmed for {{ Target.DateTime }}. Duration: {{ Target.Duration }}.{% if Target.MeetingLink != """" %} Join: {{ Target.MeetingLink }}{% endif %} — {{ CurrentOrganization.OrganizationName }}',
                    false,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""SchedulerEmailTemplates"" WHERE ""TemplateType"" = 'confirmation' AND ""Channel"" = 'sms'
                );
            ");

            // 3. Reminder - Email
            migrationBuilder.Sql(@"
                INSERT INTO ""SchedulerEmailTemplates"" (""Id"", ""TemplateType"", ""Channel"", ""Subject"", ""Content"", ""IsActive"", ""CreationTime"")
                SELECT
                    gen_random_uuid(),
                    'reminder',
                    'email',
                    '[{{ CurrentOrganization.OrganizationName }}] Appointment Reminder — {{ Target.AppointmentCode }}',
                    '<p>Hello {{ Target.ContactName }},</p>

<p>This is a reminder about your upcoming appointment:</p>

<table style=""border-collapse: collapse; width: 100%; margin: 20px 0;"">
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold; width: 150px;"">Appointment</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AppointmentCode }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Type</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AppointmentType }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Date & Time</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.DateTime }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Duration</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.Duration }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Mode</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AppointmentMode }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Staff</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.StaffName }}</td>
    </tr>
    {% if Target.MeetingLink != """" %}
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Meeting Link</td>
        <td style=""padding: 8px; border: 1px solid #ddd;""><a href=""{{ Target.MeetingLink }}"">{{ Target.MeetingLink }}</a></td>
    </tr>
    {% endif %}
</table>

<p>Thank you,<br>{{ CurrentOrganization.OrganizationName }}</p>',
                    true,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""SchedulerEmailTemplates"" WHERE ""TemplateType"" = 'reminder' AND ""Channel"" = 'email'
                );
            ");

            // 4. Reminder - SMS
            migrationBuilder.Sql(@"
                INSERT INTO ""SchedulerEmailTemplates"" (""Id"", ""TemplateType"", ""Channel"", ""Subject"", ""Content"", ""IsActive"", ""CreationTime"")
                SELECT
                    gen_random_uuid(),
                    'reminder',
                    'sms',
                    NULL,
                    'Reminder: Your appointment {{ Target.AppointmentCode }} ({{ Target.AppointmentType }}) is coming up at {{ Target.DateTime }}.{% if Target.MeetingLink != """" %} Join: {{ Target.MeetingLink }}{% endif %} — {{ CurrentOrganization.OrganizationName }}',
                    false,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""SchedulerEmailTemplates"" WHERE ""TemplateType"" = 'reminder' AND ""Channel"" = 'sms'
                );
            ");

            // 5. Post-Meeting - Email
            migrationBuilder.Sql(@"
                INSERT INTO ""SchedulerEmailTemplates"" (""Id"", ""TemplateType"", ""Channel"", ""Subject"", ""Content"", ""IsActive"", ""CreationTime"")
                SELECT
                    gen_random_uuid(),
                    'post_meeting',
                    'email',
                    '[{{ CurrentOrganization.OrganizationName }}] Post-Appointment Follow-Up — {{ Target.AppointmentCode }}',
                    '<p>Hello {{ Target.ContactName }},</p>

<p>Thank you for your recent appointment. Here is a summary:</p>

<table style=""border-collapse: collapse; width: 100%; margin: 20px 0;"">
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold; width: 150px;"">Appointment</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AppointmentCode }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Type</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.AppointmentType }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Date & Time</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.DateTime }}</td>
    </tr>
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Staff</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.StaffName }}</td>
    </tr>
    {% if Target.Notes != """" %}
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; font-weight: bold;"">Notes</td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">{{ Target.Notes }}</td>
    </tr>
    {% endif %}
</table>

<p>If you have any questions or need a follow-up, please do not hesitate to reach out.</p>

<p>Thank you,<br>{{ CurrentOrganization.OrganizationName }}</p>',
                    true,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""SchedulerEmailTemplates"" WHERE ""TemplateType"" = 'post_meeting' AND ""Channel"" = 'email'
                );
            ");

            // 6. Post-Meeting - SMS
            migrationBuilder.Sql(@"
                INSERT INTO ""SchedulerEmailTemplates"" (""Id"", ""TemplateType"", ""Channel"", ""Subject"", ""Content"", ""IsActive"", ""CreationTime"")
                SELECT
                    gen_random_uuid(),
                    'post_meeting',
                    'sms',
                    NULL,
                    'Thank you for your appointment {{ Target.AppointmentCode }} ({{ Target.AppointmentType }}) on {{ Target.DateTime }}. If you need a follow-up, please reach out. — {{ CurrentOrganization.OrganizationName }}',
                    false,
                    NOW() AT TIME ZONE 'UTC'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""SchedulerEmailTemplates"" WHERE ""TemplateType"" = 'post_meeting' AND ""Channel"" = 'sms'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""SchedulerEmailTemplates""
                WHERE ""TemplateType"" IN ('confirmation', 'reminder', 'post_meeting');
            ");
        }
    }
}
