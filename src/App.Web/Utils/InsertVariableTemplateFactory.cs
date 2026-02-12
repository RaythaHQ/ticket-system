using System.Collections.Generic;
using System.Linq;
using App.Application.Admins;
using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Application.Login;
using App.Application.Tickets.RenderModels;
using App.Application.TicketTasks.RenderModels;
using App.Application.Users;
using App.Domain.Entities;
using App.Domain.Exceptions;

namespace App.Web.Utils;

public class InsertVariableTemplateFactory
{
    static InsertVariableTemplateFactory() { }

    private InsertVariableTemplateFactory() { }

    private InsertVariableTemplateFactory(
        string developerName,
        string variableCategoryName,
        IInsertTemplateVariable templateInfo
    )
    {
        DeveloperName = developerName;
        TemplateInfo = templateInfo;
        VariableCategoryName = variableCategoryName;
    }

    public static InsertVariableTemplateFactory From(string developerName)
    {
        var type = Templates.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedTemplateTypeException(developerName);
        }

        return type;
    }

    public static InsertVariableTemplateFactory Request =>
        new("Request", "Request", new Wrapper_RenderModel());
    public static InsertVariableTemplateFactory AdminWelcomeEmail =>
        new(
            BuiltInEmailTemplate.AdminWelcomeEmail,
            "Target",
            new SendAdminWelcomeEmail_RenderModel()
        );
    public static InsertVariableTemplateFactory AdminPasswordChangedEmail =>
        new(
            BuiltInEmailTemplate.AdminPasswordChangedEmail,
            "Target",
            new SendAdminPasswordChanged_RenderModel()
        );
    public static InsertVariableTemplateFactory AdminPasswordResetEmail =>
        new(
            BuiltInEmailTemplate.AdminPasswordResetEmail,
            "Target",
            new SendAdminPasswordReset_RenderModel()
        );
    public static InsertVariableTemplateFactory LoginBeginLoginWithMagicLinkEmail =>
        new(
            BuiltInEmailTemplate.LoginBeginLoginWithMagicLinkEmail,
            "Target",
            new SendBeginLoginWithMagicLink_RenderModel()
        );
    public static InsertVariableTemplateFactory LoginBeginForgotPasswordEmail =>
        new(
            BuiltInEmailTemplate.LoginBeginForgotPasswordEmail,
            "Target",
            new SendBeginForgotPassword_RenderModel()
        );
    public static InsertVariableTemplateFactory LoginCompletedForgotPasswordEmail =>
        new(
            BuiltInEmailTemplate.LoginCompletedForgotPasswordEmail,
            "Target",
            new SendCompletedForgotPassword_RenderModel()
        );
    public static InsertVariableTemplateFactory UserWelcomeEmail =>
        new(
            BuiltInEmailTemplate.UserWelcomeEmail,
            "Target",
            new SendUserWelcomeEmail_RenderModel()
        );
    public static InsertVariableTemplateFactory UserPasswordChangedEmail =>
        new(
            BuiltInEmailTemplate.UserPasswordChangedEmail,
            "Target",
            new SendUserPasswordChanged_RenderModel()
        );
    public static InsertVariableTemplateFactory UserPasswordResetEmail =>
        new(
            BuiltInEmailTemplate.UserPasswordResetEmail,
            "Target",
            new SendUserPasswordReset_RenderModel()
        );
    public static InsertVariableTemplateFactory CurrentOrganization =>
        new("CurrentOrganization", "CurrentOrganization", new CurrentOrganization_RenderModel());
    public static InsertVariableTemplateFactory CurrentUser =>
        new("CurrentUser", "CurrentUser", new CurrentUser_RenderModel());

    // Ticket notification templates
    public static InsertVariableTemplateFactory TicketAssignedEmail =>
        new(BuiltInEmailTemplate.TicketAssignedEmail, "Target", new TicketAssigned_RenderModel());
    public static InsertVariableTemplateFactory TicketAssignedToTeamEmail =>
        new(
            BuiltInEmailTemplate.TicketAssignedToTeamEmail,
            "Target",
            new TicketAssignedToTeam_RenderModel()
        );
    public static InsertVariableTemplateFactory TicketCommentAddedEmail =>
        new(
            BuiltInEmailTemplate.TicketCommentAddedEmail,
            "Target",
            new TicketCommentAdded_RenderModel()
        );
    public static InsertVariableTemplateFactory TicketStatusChangedEmail =>
        new(
            BuiltInEmailTemplate.TicketStatusChangedEmail,
            "Target",
            new TicketStatusChanged_RenderModel()
        );
    public static InsertVariableTemplateFactory TicketClosedEmail =>
        new(BuiltInEmailTemplate.TicketClosedEmail, "Target", new TicketClosed_RenderModel());
    public static InsertVariableTemplateFactory TicketReopenedEmail =>
        new(BuiltInEmailTemplate.TicketReopenedEmail, "Target", new TicketReopened_RenderModel());
    public static InsertVariableTemplateFactory TicketUnsnoozedEmail =>
        new(BuiltInEmailTemplate.TicketUnsnoozedEmail, "Target", new TicketUnsnoozed_RenderModel());
    public static InsertVariableTemplateFactory SlaApproachingEmail =>
        new(BuiltInEmailTemplate.SlaApproachingEmail, "Target", new SlaApproaching_RenderModel());
    public static InsertVariableTemplateFactory SlaBreachedEmail =>
        new(BuiltInEmailTemplate.SlaBreachedEmail, "Target", new SlaBreach_RenderModel());

    // Task notification templates
    public static InsertVariableTemplateFactory TaskAssignedUserEmail =>
        new(BuiltInEmailTemplate.TaskAssignedUserEmail, "Target", new TaskAssignedUser_RenderModel());
    public static InsertVariableTemplateFactory TaskAssignedTeamEmail =>
        new(BuiltInEmailTemplate.TaskAssignedTeamEmail, "Target", new TaskAssignedTeam_RenderModel());
    public static InsertVariableTemplateFactory TaskCompletedEmail =>
        new(BuiltInEmailTemplate.TaskCompletedEmail, "Target", new TaskCompleted_RenderModel());

    public string DeveloperName { get; private set; } = string.Empty;
    public string VariableCategoryName { get; private set; } = string.Empty;
    public IInsertTemplateVariable TemplateInfo { get; private set; } = null;

    public static IEnumerable<InsertVariableTemplateFactory> Templates
    {
        get
        {
            yield return Request;

            yield return AdminWelcomeEmail;
            yield return AdminPasswordChangedEmail;
            yield return AdminPasswordResetEmail;
            yield return LoginBeginLoginWithMagicLinkEmail;
            yield return LoginBeginForgotPasswordEmail;
            yield return LoginCompletedForgotPasswordEmail;
            yield return UserWelcomeEmail;
            yield return UserPasswordChangedEmail;
            yield return UserPasswordResetEmail;

            // Ticket notification templates
            yield return TicketAssignedEmail;
            yield return TicketAssignedToTeamEmail;
            yield return TicketCommentAddedEmail;
            yield return TicketStatusChangedEmail;
            yield return TicketClosedEmail;
            yield return TicketReopenedEmail;
            yield return TicketUnsnoozedEmail;
            yield return SlaApproachingEmail;
            yield return SlaBreachedEmail;

            // Task notification templates
            yield return TaskAssignedUserEmail;
            yield return TaskAssignedTeamEmail;
            yield return TaskCompletedEmail;

            yield return CurrentOrganization;
            yield return CurrentUser;
        }
    }
}
