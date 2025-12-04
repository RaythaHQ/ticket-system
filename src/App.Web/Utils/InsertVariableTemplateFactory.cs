using System.Collections.Generic;
using System.Linq;
using App.Application.Admins;
using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Application.Login;
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

            yield return CurrentOrganization;
            yield return CurrentUser;
        }
    }
}
