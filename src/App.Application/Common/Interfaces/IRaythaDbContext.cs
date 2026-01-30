using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Common.Interfaces;

public interface IAppDbContext
{
    public DbSet<User> Users { get; }
    public DbSet<Role> Roles { get; }
    public DbSet<UserGroup> UserGroups { get; }
    public DbSet<VerificationCode> VerificationCodes { get; }
    public DbSet<Domain.Entities.OrganizationSettings> OrganizationSettings { get; }
    public DbSet<EmailTemplate> EmailTemplates { get; }
    public DbSet<EmailTemplateRevision> EmailTemplateRevisions { get; }
    public DbSet<AuthenticationScheme> AuthenticationSchemes { get; }
    public DbSet<JwtLogin> JwtLogins { get; }
    public DbSet<OneTimePassword> OneTimePasswords { get; }
    public DbSet<AuditLog> AuditLogs { get; }
    public DbSet<ApiKey> ApiKeys { get; }
    public DbSet<BackgroundTask> BackgroundTasks { get; }
    public DbSet<FailedLoginAttempt> FailedLoginAttempts { get; }
    public DbSet<MediaItem> MediaItems { get; }

    // Ticketing system entities
    public DbSet<Ticket> Tickets { get; }
    public DbSet<Contact> Contacts { get; }
    public DbSet<Team> Teams { get; }
    public DbSet<TeamMembership> TeamMemberships { get; }
    public DbSet<TicketChangeLogEntry> TicketChangeLogEntries { get; }
    public DbSet<TicketComment> TicketComments { get; }
    public DbSet<TicketAttachment> TicketAttachments { get; }
    public DbSet<TicketFollower> TicketFollowers { get; }
    public DbSet<ContactChangeLogEntry> ContactChangeLogEntries { get; }
    public DbSet<ContactAttachment> ContactAttachments { get; }
    public DbSet<ContactComment> ContactComments { get; }
    public DbSet<SlaRule> SlaRules { get; }
    public DbSet<TicketView> TicketViews { get; }
    public DbSet<UserFavoriteView> UserFavoriteViews { get; }
    public DbSet<NotificationPreference> NotificationPreferences { get; }
    public DbSet<ExportJob> ExportJobs { get; }
    public DbSet<ImportJob> ImportJobs { get; }

    // Wiki entities
    public DbSet<WikiArticle> WikiArticles { get; }

    // Ticket configuration entities
    public DbSet<TicketPriorityConfig> TicketPriorityConfigs { get; }
    public DbSet<TicketStatusConfig> TicketStatusConfigs { get; }

    // Webhook entities
    public DbSet<Webhook> Webhooks { get; }
    public DbSet<WebhookLog> WebhookLogs { get; }

    // Notification entities
    public DbSet<Notification> Notifications { get; }

    public DbContext DbContext { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
