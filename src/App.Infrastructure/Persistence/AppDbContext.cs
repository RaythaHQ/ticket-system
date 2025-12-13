using System.Reflection;
using Mediator;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using App.Application.Common.Interfaces;
using App.Domain.Entities;
using App.Infrastructure.Common;
using App.Infrastructure.Persistence.Interceptors;

namespace App.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext, IDataProtectionKeyContext
{
    private readonly IMediator _mediator;
    private readonly AuditableEntitySaveChangesInterceptor _auditableEntitySaveChangesInterceptor;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IMediator mediator,
        AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor
    )
        : base(options)
    {
        _mediator = mediator;
        _auditableEntitySaveChangesInterceptor = auditableEntitySaveChangesInterceptor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailTemplateRevision> EmailTemplateRevisions => Set<EmailTemplateRevision>();
    public DbSet<AuthenticationScheme> AuthenticationSchemes => Set<AuthenticationScheme>();
    public DbSet<JwtLogin> JwtLogins => Set<JwtLogin>();
    public DbSet<OneTimePassword> OneTimePasswords => Set<OneTimePassword>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<BackgroundTask> BackgroundTasks => Set<BackgroundTask>();
    public DbSet<FailedLoginAttempt> FailedLoginAttempts => Set<FailedLoginAttempt>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    // Ticketing system entities
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();
    public DbSet<TicketChangeLogEntry> TicketChangeLogEntries => Set<TicketChangeLogEntry>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<ContactChangeLogEntry> ContactChangeLogEntries => Set<ContactChangeLogEntry>();
    public DbSet<ContactComment> ContactComments => Set<ContactComment>();
    public DbSet<SlaRule> SlaRules => Set<SlaRule>();
    public DbSet<TicketView> TicketViews => Set<TicketView>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    // Ticket configuration entities
    public DbSet<TicketPriorityConfig> TicketPriorityConfigs => Set<TicketPriorityConfig>();
    public DbSet<TicketStatusConfig> TicketStatusConfigs => Set<TicketStatusConfig>();

    public DbContext DbContext => this;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntitySaveChangesInterceptor);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _mediator.DispatchDomainEventsBeforeSaveChanges(this);
        var numItems = await base.SaveChangesAsync(cancellationToken);
        await _mediator.DispatchDomainEventsAfterSaveChanges(this);

        return numItems;
    }
}
