using System.Text.Json.Serialization;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;

namespace App.Application.OrganizationSettings.Commands;

public class InitialSetup
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;

        [JsonIgnore]
        public string SuperAdminEmailAddress { get; init; } = null!;

        [JsonIgnore]
        public string SuperAdminPassword { get; init; } = null!;
        public string OrganizationName { get; init; } = null!;
        public string WebsiteUrl { get; init; } = null!;
        public string TimeZone { get; init; } = null!;
        public string SmtpDefaultFromAddress { get; init; } = null!;
        public string SmtpDefaultFromName { get; init; } = null!;

        [JsonIgnore]
        public string SmtpHost { get; init; } = null!;

        [JsonIgnore]
        public int? SmtpPort { get; init; }

        [JsonIgnore]
        public string SmtpUsername { get; init; } = null!;

        [JsonIgnore]
        public string SmtpPassword { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IEmailerConfiguration emailerConfiguration)
        {
            RuleFor(x => x.SuperAdminPassword).NotEmpty().MinimumLength(8);
            RuleFor(x => x.SuperAdminEmailAddress).EmailAddress();
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.SmtpPort)
                .GreaterThan(0)
                .LessThanOrEqualTo(65535)
                .When(p => p.SmtpPort.HasValue);
            RuleFor(x => x.OrganizationName).NotEmpty();
            RuleFor(x => x.TimeZone)
                .Must(DateTimeExtensions.IsValidTimeZone)
                .WithMessage(p => $"{p.TimeZone} timezone is unrecognized.");
            RuleFor(x => x.WebsiteUrl)
                .Must(StringExtensions.IsValidUriFormat)
                .WithMessage(p => $"{p.WebsiteUrl} must be a valid URI format.");
            RuleFor(x => x.SmtpDefaultFromAddress).EmailAddress();
            RuleFor(x => x.SmtpDefaultFromName).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        Guid orgSettingsGuid = Guid.NewGuid();

        private readonly IAppDbContext _db;
        private readonly IEmailerConfiguration _emailerConfiguration;

        public Handler(IAppDbContext db, IEmailerConfiguration emailerConfiguration)
        {
            _db = db;
            _emailerConfiguration = emailerConfiguration;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            InsertOrganizationSettings(request);
            InsertDefaultRolesAndSuperAdmin(request);
            InsertDefaultEmailTemplates();
            InsertDefaultAuthentications();
            InsertDefaultTicketPriorities();
            InsertDefaultTicketStatuses();
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(orgSettingsGuid);
        }

        protected void InsertOrganizationSettings(Command request)
        {
            var entity = new Domain.Entities.OrganizationSettings
            {
                Id = orgSettingsGuid,
                SmtpHost = request.SmtpHost,
                SmtpPort = request.SmtpPort,
                SmtpUsername = request.SmtpUsername,
                SmtpPassword = request.SmtpPassword,
                SmtpOverrideSystem = _emailerConfiguration.IsMissingSmtpEnvVars(),
                OrganizationName = request.OrganizationName,
                TimeZone = request.TimeZone,
                DateFormat = DateTimeExtensions.DEFAULT_DATE_FORMAT,
                WebsiteUrl = request.WebsiteUrl,
                SmtpDefaultFromAddress = request.SmtpDefaultFromAddress,
                SmtpDefaultFromName = request.SmtpDefaultFromName,
            };
            _db.OrganizationSettings.Add(entity);
        }

        protected void InsertDefaultRolesAndSuperAdmin(Command request)
        {
            var roles = new List<Role>();
            Role superAdminRole = new Role
            {
                Id = Guid.NewGuid(),
                Label = BuiltInRole.SuperAdmin.DefaultLabel,
                DeveloperName = BuiltInRole.SuperAdmin,
                SystemPermissions = BuiltInRole.SuperAdmin.DefaultSystemPermission,
                CreationTime = DateTime.UtcNow,
            };
            roles.Add(superAdminRole);
            Role adminRole = new Role
            {
                Id = Guid.NewGuid(),
                Label = BuiltInRole.Admin.DefaultLabel,
                DeveloperName = BuiltInRole.Admin,
                SystemPermissions = BuiltInRole.Admin.DefaultSystemPermission,
                CreationTime = DateTime.UtcNow,
            };
            roles.Add(adminRole);
            Role editorRole = new Role
            {
                Id = Guid.NewGuid(),
                Label = BuiltInRole.Editor.DefaultLabel,
                DeveloperName = BuiltInRole.Editor,
                SystemPermissions = BuiltInRole.Editor.DefaultSystemPermission,
                CreationTime = DateTime.UtcNow,
            };
            roles.Add(editorRole);

            var salt = PasswordUtility.RandomSalt();
            var superAdmin = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.SuperAdminEmailAddress,
                Roles = roles,
                Salt = salt,
                PasswordHash = PasswordUtility.Hash(request.SuperAdminPassword, salt),
                IsActive = true,
                IsAdmin = true,
            };
            _db.Users.Add(superAdmin);
        }

        protected void InsertDefaultEmailTemplates()
        {
            var list = new List<EmailTemplate>();

            foreach (var templateToBuild in BuiltInEmailTemplate.Templates)
            {
                var template = new EmailTemplate
                {
                    Id = Guid.NewGuid(),
                    Content = templateToBuild.DefaultContent,
                    Subject = templateToBuild.DefaultSubject,
                    DeveloperName = templateToBuild.DeveloperName,
                    IsBuiltInTemplate = true,
                };
                list.Add(template);
            }

            _db.EmailTemplates.AddRange(list);
        }

        protected void InsertDefaultAuthentications()
        {
            var list = new List<AuthenticationScheme>
            {
                new AuthenticationScheme
                {
                    Label = "Email address and password",
                    DeveloperName = AuthenticationSchemeType.EmailAndPassword,
                    IsBuiltInAuth = true,
                    IsEnabledForAdmins = true,
                    IsEnabledForUsers = true,
                    AuthenticationSchemeType = AuthenticationSchemeType.EmailAndPassword,
                    LoginButtonText = "Login with your email and password",
                    BruteForceProtectionMaxFailedAttempts = 10,
                    BruteForceProtectionWindowInSeconds = 60,
                },
                new AuthenticationScheme
                {
                    Label = "Magic link",
                    DeveloperName = AuthenticationSchemeType.MagicLink,
                    IsBuiltInAuth = true,
                    IsEnabledForAdmins = false,
                    IsEnabledForUsers = false,
                    AuthenticationSchemeType = AuthenticationSchemeType.MagicLink,
                    LoginButtonText = "Email me a login link",
                    MagicLinkExpiresInSeconds = 900,
                },
            };
            _db.AuthenticationSchemes.AddRange(list);
        }

        protected void InsertDefaultTicketPriorities()
        {
            var priorities = new List<TicketPriorityConfig>
            {
                new TicketPriorityConfig
                {
                    Id = Guid.NewGuid(),
                    Label = "Urgent",
                    DeveloperName = "urgent",
                    ColorName = "danger",
                    SortOrder = 1,
                    IsDefault = false,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                },
                new TicketPriorityConfig
                {
                    Id = Guid.NewGuid(),
                    Label = "High",
                    DeveloperName = "high",
                    ColorName = "warning",
                    SortOrder = 2,
                    IsDefault = false,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                },
                new TicketPriorityConfig
                {
                    Id = Guid.NewGuid(),
                    Label = "Normal",
                    DeveloperName = "normal",
                    ColorName = "primary",
                    SortOrder = 3,
                    IsDefault = true,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                },
                new TicketPriorityConfig
                {
                    Id = Guid.NewGuid(),
                    Label = "Low",
                    DeveloperName = "low",
                    ColorName = "secondary",
                    SortOrder = 4,
                    IsDefault = false,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                },
            };
            _db.TicketPriorityConfigs.AddRange(priorities);
        }

        protected void InsertDefaultTicketStatuses()
        {
            var statuses = new List<TicketStatusConfig>
            {
                new TicketStatusConfig
                {
                    Id = Guid.NewGuid(),
                    Label = "New",
                    DeveloperName = "new",
                    ColorName = "primary",
                    SortOrder = 1,
                    StatusType = TicketStatusType.OPEN,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                },
                new TicketStatusConfig
                {
                    Id = Guid.NewGuid(),
                    Label = "In Progress",
                    DeveloperName = "in_progress",
                    ColorName = "info",
                    SortOrder = 2,
                    StatusType = TicketStatusType.OPEN,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                },
                new TicketStatusConfig
                {
                    Id = Guid.NewGuid(),
                    Label = "Pending",
                    DeveloperName = "pending",
                    ColorName = "warning",
                    SortOrder = 3,
                    StatusType = TicketStatusType.OPEN,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                },
                new TicketStatusConfig
                {
                    Id = Guid.NewGuid(),
                    Label = "Closed",
                    DeveloperName = "closed",
                    ColorName = "secondary",
                    SortOrder = 4,
                    StatusType = TicketStatusType.CLOSED,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                },
            };
            _db.TicketStatusConfigs.AddRange(statuses);
        }
    }
}
