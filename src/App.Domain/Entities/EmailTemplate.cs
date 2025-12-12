namespace App.Domain.Entities;

public class EmailTemplate : BaseAuditableEntity
{
    public string? Subject { get; set; }
    public string? DeveloperName { get; set; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? Content { get; set; }
    public bool IsBuiltInTemplate { get; set; }
    public virtual ICollection<EmailTemplateRevision> Revisions { get; set; } =
        new List<EmailTemplateRevision>();
}

public class BuiltInEmailTemplate : ValueObject
{
    static BuiltInEmailTemplate() { }

    private BuiltInEmailTemplate() { }

    private BuiltInEmailTemplate(string subject, string developerName, bool safeToCc)
    {
        DefaultSubject = subject;
        DeveloperName = developerName;
        SafeToCc = safeToCc;
    }

    public static BuiltInEmailTemplate From(string developerName)
    {
        var type = Templates.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedTemplateTypeException(developerName);
        }

        return type;
    }

    public static BuiltInEmailTemplate AdminWelcomeEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] An administrator has created your account",
            "email_admin_welcome",
            false
        );
    public static BuiltInEmailTemplate AdminPasswordChangedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been changed",
            "email_admin_passwordchanged",
            false
        );
    public static BuiltInEmailTemplate AdminPasswordResetEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been reset by an administrator",
            "email_admin_passwordreset",
            false
        );

    public static BuiltInEmailTemplate LoginBeginLoginWithMagicLinkEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Website login access link",
            "email_login_beginloginwithmagiclink",
            false
        );
    public static BuiltInEmailTemplate LoginBeginForgotPasswordEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Password recovery",
            "email_login_beginforgotpassword",
            false
        );
    public static BuiltInEmailTemplate LoginCompletedForgotPasswordEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been recovered",
            "email_login_completedforgotpassword",
            false
        );

    public static BuiltInEmailTemplate UserWelcomeEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] An administrator has created your account",
            "email_user_welcome",
            false
        );
    public static BuiltInEmailTemplate UserPasswordChangedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been changed",
            "email_user_passwordchanged",
            false
        );
    public static BuiltInEmailTemplate UserPasswordResetEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Your password has been reset by an administrator",
            "email_user_passwordreset",
            false
        );

    // Ticketing notification templates
    public static BuiltInEmailTemplate TicketAssignedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} assigned to you",
            "email_ticket_assigned",
            true
        );
    public static BuiltInEmailTemplate TicketCommentAddedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] New comment on ticket #{{ Target.TicketId }}",
            "email_ticket_commentadded",
            true
        );
    public static BuiltInEmailTemplate TicketStatusChangedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} status changed",
            "email_ticket_statuschanged",
            true
        );
    public static BuiltInEmailTemplate SlaApproachingEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] ⚠️ SLA approaching for ticket #{{ Target.TicketId }}",
            "email_sla_approaching",
            true
        );
    public static BuiltInEmailTemplate SlaBreachedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] 🚨 SLA breached for ticket #{{ Target.TicketId }}",
            "email_sla_breached",
            true
        );
    public static BuiltInEmailTemplate TicketAssignedToTeamEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} assigned to your team",
            "email_ticket_assignedtoteam",
            true
        );
    public static BuiltInEmailTemplate TicketClosedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} has been closed",
            "email_ticket_closed",
            true
        );
    public static BuiltInEmailTemplate TicketReopenedEmail =>
        new(
            "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} has been reopened",
            "email_ticket_reopened",
            true
        );

    public string DefaultSubject { get; private set; } = string.Empty;
    public string DeveloperName { get; private set; } = string.Empty;
    public bool SafeToCc { get; private set; } = false;

    public string DefaultContent
    {
        get
        {
            var pathToFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Entities",
                "DefaultTemplates",
                $"{DeveloperName}.liquid"
            );
            return File.ReadAllText(pathToFile);
        }
    }

    public static implicit operator string(BuiltInEmailTemplate scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator BuiltInEmailTemplate(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    public static IEnumerable<BuiltInEmailTemplate> Templates
    {
        get
        {
            yield return AdminWelcomeEmail;
            yield return AdminPasswordChangedEmail;
            yield return AdminPasswordResetEmail;

            yield return LoginBeginLoginWithMagicLinkEmail;
            yield return LoginBeginForgotPasswordEmail;
            yield return LoginCompletedForgotPasswordEmail;

            yield return UserWelcomeEmail;
            yield return UserPasswordChangedEmail;
            yield return UserPasswordResetEmail;

            // Ticketing templates
            yield return TicketAssignedEmail;
            yield return TicketAssignedToTeamEmail;
            yield return TicketCommentAddedEmail;
            yield return TicketStatusChangedEmail;
            yield return TicketClosedEmail;
            yield return TicketReopenedEmail;
            yield return SlaApproachingEmail;
            yield return SlaBreachedEmail;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
