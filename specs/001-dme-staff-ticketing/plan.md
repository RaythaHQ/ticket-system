# Implementation Plan: DME Staff Ticketing System

**Feature Branch**: `001-dme-staff-ticketing`  
**Spec Reference**: [spec.md](./spec.md)  
**Created**: 2025-12-11  
**Status**: Ready for Implementation

---

## Table of Contents

1. [Architecture Alignment](#1-architecture-alignment)
2. [Phase 1: Domain Model](#2-phase-1-domain-model)
3. [Phase 2: Persistence Layer](#3-phase-2-persistence-layer)
4. [Phase 3: Application Layer - Core CQRS](#4-phase-3-application-layer---core-cqrs)
5. [Phase 4: Staff UI - Tickets](#5-phase-4-staff-ui---tickets)
6. [Phase 5: Staff UI - Contacts](#6-phase-5-staff-ui---contacts)
7. [Phase 6: Admin UI - Configuration](#7-phase-6-admin-ui---configuration)
8. [Phase 7: Email Notifications](#8-phase-7-email-notifications)
9. [Phase 8: Background Jobs & SLA Processing](#9-phase-8-background-jobs--sla-processing)
10. [Phase 9: Metrics & Reporting](#10-phase-9-metrics--reporting)
11. [Phase 10: Testing](#11-phase-10-testing)
12. [Integration Points & Special Considerations](#12-integration-points--special-considerations)

---

## 1. Architecture Alignment

### Constitution Compliance Checklist

| Principle | Alignment Strategy |
|-----------|-------------------|
| Clean Architecture & Dependency Rule | All new code follows `Web → Application → Domain` + `Infrastructure → Application` flow |
| CQRS & Mediator-Driven Use Cases | All ticket/contact operations implemented as Commands/Queries with Validators and Handlers |
| Razor Pages First, Minimal JavaScript | Staff and Admin UIs use Razor Pages with thin PageModels; JS only for search/filter enhancements |
| Explicit Data Access | All DB access via `IAppDbContext`; async methods with `CancellationToken` throughout |
| Security & Observability | Permission checks in handlers; structured logging; audit via change logs |

### Deviation: Numeric IDs for Tickets and Contacts

**Justification**: Business requirement for human-readable ticket and contact IDs (e.g., "TKT-12345", "CON-67890"). Numeric IDs are easier for phone support, reporting, and external system integration in DME workflows.

**Impact**:
- New base entity/DTO classes for numeric ID entities
- Custom configurations using `UseIdentityColumn()` or sequence
- No impact on existing GUID-based entities

---

## 2. Phase 1: Domain Model

### 2.1 Create Numeric ID Base Classes

**File**: `src/App.Domain/Common/BaseNumericEntity.cs`

```csharp
public interface IBaseNumericEntity
{
    long Id { get; set; }
}

public abstract class BaseNumericEntity : IBaseNumericEntity
{
    public long Id { get; set; }

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void RemoveDomainEvent(BaseEvent domainEvent) => _domainEvents.Remove(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**File**: `src/App.Domain/Common/BaseNumericAuditableEntity.cs`

```csharp
public abstract class BaseNumericAuditableEntity : BaseNumericEntity, ICreationAuditable, IModificationAuditable
{
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastModificationTime { get; set; }
    public Guid? CreatorUserId { get; set; }
    public virtual User? CreatorUser { get; set; }
    public Guid? LastModifierUserId { get; set; }
    public virtual User? LastModifierUser { get; set; }
}
```

**File**: `src/App.Domain/Common/BaseNumericFullAuditableEntity.cs`

```csharp
public abstract class BaseNumericFullAuditableEntity : BaseNumericAuditableEntity, ISoftDelete
{
    public Guid? DeleterUserId { get; set; }
    public DateTime? DeletionTime { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

### 2.2 Create Core Domain Entities

**Location**: `src/App.Domain/Entities/`

#### Ticket Entity

**File**: `src/App.Domain/Entities/Ticket.cs`

```csharp
public class Ticket : BaseNumericFullAuditableEntity
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public string? Category { get; set; }
    
    // Relationships
    public Guid? OwningTeamId { get; set; }
    public virtual Team? OwningTeam { get; set; }
    
    public Guid? AssigneeId { get; set; }
    public virtual User? Assignee { get; set; }
    
    public Guid? CreatedByStaffId { get; set; }
    public virtual User? CreatedByStaff { get; set; }
    
    public long? ContactId { get; set; }
    public virtual Contact? Contact { get; set; }
    
    // Tags stored as JSON array
    public string? TagsJson { get; set; }
    
    [NotMapped]
    public List<string> Tags
    {
        get => string.IsNullOrEmpty(TagsJson) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        set => TagsJson = JsonSerializer.Serialize(value);
    }
    
    // Timestamps
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    // SLA
    public Guid? SlaRuleId { get; set; }
    public virtual SlaRule? SlaRule { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public DateTime? SlaBreachedAt { get; set; }
    public SlaStatus? SlaStatus { get; set; }
    
    // Collections
    public virtual ICollection<TicketChangeLogEntry> ChangeLogEntries { get; set; } = new List<TicketChangeLogEntry>();
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
```

#### Contact Entity

**File**: `src/App.Domain/Entities/Contact.cs`

```csharp
public class Contact : BaseNumericFullAuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumbersJson { get; set; } // E.164 normalized, stored as JSON array
    public string? Address { get; set; }
    public string? OrganizationAccount { get; set; }
    public string? DmeIdentifiersJson { get; set; } // JSON object for DME-specific IDs
    
    [NotMapped]
    public List<string> PhoneNumbers
    {
        get => string.IsNullOrEmpty(PhoneNumbersJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(PhoneNumbersJson) ?? new List<string>();
        set => PhoneNumbersJson = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public Dictionary<string, string> DmeIdentifiers
    {
        get => string.IsNullOrEmpty(DmeIdentifiersJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(DmeIdentifiersJson) ?? new Dictionary<string, string>();
        set => DmeIdentifiersJson = JsonSerializer.Serialize(value);
    }
    
    // Collections
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<ContactChangeLogEntry> ChangeLogEntries { get; set; } = new List<ContactChangeLogEntry>();
    public virtual ICollection<ContactComment> Comments { get; set; } = new List<ContactComment>();
}
```

#### Additional Entities to Create

| Entity | Base Class | File |
|--------|-----------|------|
| `Team` | `BaseAuditableEntity` (GUID) | `Team.cs` |
| `TeamMembership` | `BaseAuditableEntity` (GUID) | `TeamMembership.cs` |
| `TicketChangeLogEntry` | `BaseEntity` (GUID) | `TicketChangeLogEntry.cs` |
| `TicketComment` | `BaseAuditableEntity` (GUID) | `TicketComment.cs` |
| `TicketAttachment` | `BaseAuditableEntity` (GUID) | `TicketAttachment.cs` |
| `ContactChangeLogEntry` | `BaseEntity` (GUID) | `ContactChangeLogEntry.cs` |
| `ContactComment` | `BaseAuditableEntity` (GUID) | `ContactComment.cs` |
| `SlaRule` | `BaseAuditableEntity` (GUID) | `SlaRule.cs` |
| `TicketView` | `BaseAuditableEntity` (GUID) | `TicketView.cs` |
| `NotificationPreference` | `BaseAuditableEntity` (GUID) | `NotificationPreference.cs` |

### 2.3 Create Value Objects

**Location**: `src/App.Domain/ValueObjects/`

| Value Object | File | Purpose |
|--------------|------|---------|
| `TicketStatus` | `TicketStatus.cs` | Open, InProgress, Pending, Resolved, Closed |
| `TicketPriority` | `TicketPriority.cs` | Low, Normal, High, Urgent |
| `SlaStatus` | `SlaStatus.cs` | OnTrack, ApproachingBreach, Breached, Completed |
| `NotificationEventType` | `NotificationEventType.cs` | TicketAssigned, CommentAdded, etc. |

**Pattern**: Follow existing `SortOrder` value object pattern with `From()` factory method.

### 2.4 Create Domain Events

**Location**: `src/App.Domain/Events/`

| Event | File | Triggers |
|-------|------|----------|
| `TicketCreatedEvent` | `TicketCreatedEvent.cs` | Ticket creation |
| `TicketAssignedEvent` | `TicketAssignedEvent.cs` | Ticket assignment to user |
| `TicketAssignedToTeamEvent` | `TicketAssignedToTeamEvent.cs` | Ticket assignment to team |
| `TicketStatusChangedEvent` | `TicketStatusChangedEvent.cs` | Status transitions |
| `TicketCommentAddedEvent` | `TicketCommentAddedEvent.cs` | Comment added |
| `TicketClosedEvent` | `TicketClosedEvent.cs` | Ticket closed |
| `TicketReopenedEvent` | `TicketReopenedEvent.cs` | Ticket reopened |
| `SlaApproachingBreachEvent` | `SlaApproachingBreachEvent.cs` | SLA threshold crossed |
| `SlaBreachedEvent` | `SlaBreachedEvent.cs` | SLA breached |
| `ContactCreatedEvent` | `ContactCreatedEvent.cs` | Contact creation |

### 2.5 Update User Entity

**File**: `src/App.Domain/Entities/User.cs`

Add permission flags:

```csharp
// Add to existing User entity
public bool CanManageTickets { get; set; }
public bool ManageTeams { get; set; }
public bool AccessReports { get; set; }

// Navigation properties
public virtual ICollection<TeamMembership> TeamMemberships { get; set; } = new List<TeamMembership>();
public virtual ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
```

---

## 3. Phase 2: Persistence Layer

### 3.1 Update IAppDbContext

**File**: `src/App.Application/Common/Interfaces/IRaythaDbContext.cs`

Add new DbSets:

```csharp
// Add to IAppDbContext interface
DbSet<Ticket> Tickets { get; }
DbSet<Contact> Contacts { get; }
DbSet<Team> Teams { get; }
DbSet<TeamMembership> TeamMemberships { get; }
DbSet<TicketChangeLogEntry> TicketChangeLogEntries { get; }
DbSet<TicketComment> TicketComments { get; }
DbSet<TicketAttachment> TicketAttachments { get; }
DbSet<ContactChangeLogEntry> ContactChangeLogEntries { get; }
DbSet<ContactComment> ContactComments { get; }
DbSet<SlaRule> SlaRules { get; }
DbSet<TicketView> TicketViews { get; }
DbSet<NotificationPreference> NotificationPreferences { get; }
```

### 3.2 Update AppDbContext

**File**: `src/App.Infrastructure/Persistence/AppDbContext.cs`

Add corresponding DbSet implementations.

### 3.3 Create Entity Configurations

**Location**: `src/App.Infrastructure/Persistence/Configurations/`

#### TicketConfiguration (Numeric ID)

**File**: `TicketConfiguration.cs`

```csharp
public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        // Use identity column for numeric ID
        builder.Property(t => t.Id)
            .UseIdentityColumn();
        
        builder.HasKey(t => t.Id);
        
        // Indexes for common queries
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.OwningTeamId);
        builder.HasIndex(t => t.AssigneeId);
        builder.HasIndex(t => t.CreatedByStaffId);
        builder.HasIndex(t => t.ContactId);
        builder.HasIndex(t => t.CreationTime);
        builder.HasIndex(t => t.ClosedAt);
        builder.HasIndex(t => new { t.SlaStatus, t.SlaDueAt });
        builder.HasIndex(t => new { t.OwningTeamId, t.AssigneeId });
        builder.HasIndex(t => t.IsDeleted);
        
        // Soft delete query filter
        builder.HasQueryFilter(t => !t.IsDeleted);
        
        // Relationships
        builder.HasOne(t => t.OwningTeam)
            .WithMany(team => team.Tickets)
            .HasForeignKey(t => t.OwningTeamId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(t => t.Contact)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.ContactId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(t => t.CreatedByStaff)
            .WithMany()
            .HasForeignKey(t => t.CreatedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(t => t.SlaRule)
            .WithMany()
            .HasForeignKey(t => t.SlaRuleId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Audit relationships
        builder.HasOne(t => t.CreatorUser)
            .WithMany()
            .HasForeignKey(t => t.CreatorUserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(t => t.LastModifierUser)
            .WithMany()
            .HasForeignKey(t => t.LastModifierUserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        // Status stored as string
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
        
        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);
        
        builder.Property(t => t.SlaStatus)
            .HasConversion<string>()
            .HasMaxLength(50);
    }
}
```

#### ContactConfiguration (Numeric ID)

**File**: `ContactConfiguration.cs`

```csharp
public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.Property(c => c.Id)
            .UseIdentityColumn();
        
        builder.HasKey(c => c.Id);
        
        // Indexes
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.OrganizationAccount);
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.IsDeleted);
        
        // Soft delete query filter
        builder.HasQueryFilter(c => !c.IsDeleted);
        
        // Full-text search on Name (database-specific, handled in migration)
    }
}
```

#### Additional Configurations to Create

| Configuration | Key Indexes | Special Handling |
|---------------|-------------|------------------|
| `TeamConfiguration` | Name (unique) | Standard GUID |
| `TeamMembershipConfiguration` | (TeamId, StaffAdminId) unique | Composite index |
| `TicketChangeLogEntryConfiguration` | (TicketId, Timestamp) | Immutable pattern |
| `TicketCommentConfiguration` | TicketId | Standard |
| `TicketAttachmentConfiguration` | TicketId | Standard |
| `ContactChangeLogEntryConfiguration` | (ContactId, Timestamp) | Immutable pattern |
| `ContactCommentConfiguration` | ContactId | Standard |
| `SlaRuleConfiguration` | Active, Priority order | JSON conditions column |
| `TicketViewConfiguration` | OwnerStaffId, IsDefault | JSON conditions column |
| `NotificationPreferenceConfiguration` | (StaffAdminId, EventType) unique | Standard |

### 3.4 Update Auditable Entity Interceptor

**File**: `src/App.Infrastructure/Persistence/Interceptors/AuditableEntitySaveChangesInterceptor.cs`

Update to handle `ICreationAuditable` and `IModificationAuditable` interfaces (which `BaseNumericAuditableEntity` implements) rather than concrete base classes.

### 3.5 Create Migration

```bash
dotnet ef migrations add AddTicketingSystem -p src/App.Infrastructure -s src/App.Web
```

---

## 4. Phase 3: Application Layer - Core CQRS

### 4.1 Create Numeric ID DTO Base Classes

**File**: `src/App.Application/Common/Models/BaseNumericEntityDto.cs`

```csharp
public interface IBaseNumericEntityDto
{
    long Id { get; init; }
}

public record BaseNumericEntityDto : IBaseNumericEntityDto
{
    public long Id { get; init; }
}

public record BaseNumericAuditableEntityDto : BaseNumericEntityDto
{
    public DateTime CreationTime { get; init; }
    public ShortGuid? CreatorUserId { get; init; }
    public ShortGuid? LastModifierUserId { get; init; }
    public DateTime? LastModificationTime { get; init; }
}
```

### 4.2 Tickets Feature Structure

**Location**: `src/App.Application/Tickets/`

```
Tickets/
├── TicketDto.cs
├── TicketListItemDto.cs
├── Commands/
│   ├── CreateTicket.cs
│   ├── UpdateTicket.cs
│   ├── AssignTicket.cs
│   ├── ChangeTicketStatus.cs
│   ├── CloseTicket.cs
│   ├── ReopenTicket.cs
│   ├── AddTicketComment.cs
│   └── AddTicketAttachment.cs
├── Queries/
│   ├── GetTicketById.cs
│   ├── GetTickets.cs
│   ├── GetTicketChangeLog.cs
│   └── GetTicketComments.cs
├── EventHandlers/
│   ├── TicketCreatedEventHandler.cs
│   ├── TicketAssignedEventHandler.cs
│   ├── TicketStatusChangedEventHandler.cs
│   ├── TicketCommentAddedEventHandler.cs
│   ├── TicketClosedEventHandler.cs
│   ├── TicketReopenedEventHandler.cs
│   ├── SlaApproachingBreachEventHandler.cs
│   └── SlaBreachedEventHandler.cs
└── RenderModels/
    ├── TicketAssigned_RenderModel.cs
    ├── TicketCommentAdded_RenderModel.cs
    ├── SlaApproaching_RenderModel.cs
    └── SlaBreach_RenderModel.cs
```

#### Example: CreateTicket Command

**File**: `src/App.Application/Tickets/Commands/CreateTicket.cs`

```csharp
public class CreateTicket
{
    // Note: Returns long (numeric ID) instead of ShortGuid
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Priority { get; init; }
        public string? Category { get; init; }
        public Guid? OwningTeamId { get; init; }
        public long? ContactId { get; init; }
        public List<string>? Tags { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(500);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description,
                Status = TicketStatus.Open,
                Priority = string.IsNullOrEmpty(request.Priority) 
                    ? TicketPriority.Normal 
                    : TicketPriority.From(request.Priority),
                Category = request.Category,
                OwningTeamId = request.OwningTeamId,
                ContactId = request.ContactId,
                CreatedByStaffId = _currentUser.UserId?.Guid,
                Tags = request.Tags ?? new List<string>(),
            };

            _db.Tickets.Add(ticket);
            
            // Log creation in change log
            var changeLogEntry = new TicketChangeLogEntry
            {
                TicketId = ticket.Id, // Will be set after SaveChanges
                ActorStaffId = _currentUser.UserId?.Guid,
                FieldChangesJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["Status"] = new { OldValue = (string?)null, NewValue = ticket.Status.DeveloperName }
                }),
                Message = "Ticket created"
            };
            
            ticket.AddDomainEvent(new TicketCreatedEvent(ticket));
            
            await _db.SaveChangesAsync(cancellationToken);
            
            // Add change log entry with ticket ID
            changeLogEntry.TicketId = ticket.Id;
            _db.TicketChangeLogEntries.Add(changeLogEntry);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}
```

#### TicketDto with Numeric ID

**File**: `src/App.Application/Tickets/TicketDto.cs`

```csharp
public record TicketDto : BaseNumericAuditableEntityDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string? Category { get; init; }
    public ShortGuid? OwningTeamId { get; init; }
    public string? OwningTeamName { get; init; }
    public ShortGuid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public long? ContactId { get; init; }
    public string? ContactName { get; init; }
    public List<string> Tags { get; init; } = new();
    public DateTime? ResolvedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public ShortGuid? SlaRuleId { get; init; }
    public string? SlaRuleName { get; init; }
    public DateTime? SlaDueAt { get; init; }
    public string? SlaStatus { get; init; }

    public static Expression<Func<Ticket, TicketDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static TicketDto GetProjection(Ticket entity)
    {
        if (entity == null) return null!;

        return new TicketDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Status = entity.Status.DeveloperName,
            Priority = entity.Priority.DeveloperName,
            Category = entity.Category,
            OwningTeamId = entity.OwningTeamId,
            OwningTeamName = entity.OwningTeam?.Name,
            AssigneeId = entity.AssigneeId,
            AssigneeName = entity.Assignee?.FullName,
            ContactId = entity.ContactId,
            ContactName = entity.Contact?.Name,
            Tags = entity.Tags,
            ResolvedAt = entity.ResolvedAt,
            ClosedAt = entity.ClosedAt,
            SlaRuleId = entity.SlaRuleId,
            SlaRuleName = entity.SlaRule?.Name,
            SlaDueAt = entity.SlaDueAt,
            SlaStatus = entity.SlaStatus?.DeveloperName,
            CreationTime = entity.CreationTime,
            CreatorUserId = entity.CreatorUserId,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
        };
    }
}
```

### 4.3 Contacts Feature Structure

**Location**: `src/App.Application/Contacts/`

```
Contacts/
├── ContactDto.cs
├── ContactListItemDto.cs
├── Commands/
│   ├── CreateContact.cs
│   ├── UpdateContact.cs
│   ├── DeleteContact.cs
│   └── AddContactComment.cs
├── Queries/
│   ├── GetContactById.cs
│   ├── GetContacts.cs
│   ├── SearchContacts.cs
│   ├── GetContactChangeLog.cs
│   └── GetContactTickets.cs
└── Utils/
    └── PhoneNumberNormalizer.cs
```

#### Phone Number Normalization Utility

**File**: `src/App.Application/Contacts/Utils/PhoneNumberNormalizer.cs`

```csharp
public static class PhoneNumberNormalizer
{
    /// <summary>
    /// Normalizes phone number to E.164 format.
    /// Input: "(555) 123-4567" or "555.123.4567" or "+1-555-123-4567"
    /// Output: "+15551234567"
    /// </summary>
    public static string Normalize(string input, string defaultCountryCode = "1")
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        
        // Strip all non-digit characters except leading +
        var digits = new string(input.Where(c => char.IsDigit(c) || c == '+').ToArray());
        
        if (digits.StartsWith("+"))
            return digits;
        
        // Assume US/Canada if no country code
        if (digits.Length == 10)
            return $"+{defaultCountryCode}{digits}";
        
        if (digits.Length == 11 && digits.StartsWith(defaultCountryCode))
            return $"+{digits}";
        
        return $"+{digits}";
    }
    
    /// <summary>
    /// Normalizes input for search matching.
    /// </summary>
    public static string NormalizeForSearch(string input)
    {
        return Normalize(input);
    }
}
```

### 4.4 Teams Feature Structure

**Location**: `src/App.Application/Teams/`

```
Teams/
├── TeamDto.cs
├── TeamMembershipDto.cs
├── Commands/
│   ├── CreateTeam.cs
│   ├── UpdateTeam.cs
│   ├── DeleteTeam.cs
│   ├── AddTeamMember.cs
│   ├── RemoveTeamMember.cs
│   ├── SetMemberAssignable.cs
│   └── ToggleRoundRobin.cs
├── Queries/
│   ├── GetTeamById.cs
│   ├── GetTeams.cs
│   ├── GetTeamMembers.cs
│   └── GetNextRoundRobinAssignee.cs
└── Services/
    └── RoundRobinService.cs
```

### 4.5 SLA Rules Feature Structure

**Location**: `src/App.Application/SlaRules/`

```
SlaRules/
├── SlaRuleDto.cs
├── Commands/
│   ├── CreateSlaRule.cs
│   ├── UpdateSlaRule.cs
│   ├── DeactivateSlaRule.cs
│   └── ReorderSlaRules.cs
├── Queries/
│   ├── GetSlaRuleById.cs
│   ├── GetSlaRules.cs
│   └── EvaluateSlaForTicket.cs
└── Services/
    └── SlaEvaluationService.cs
```

### 4.6 Views Feature Structure

**Location**: `src/App.Application/TicketViews/`

```
TicketViews/
├── TicketViewDto.cs
├── Commands/
│   ├── CreateTicketView.cs
│   ├── UpdateTicketView.cs
│   └── DeleteTicketView.cs
├── Queries/
│   ├── GetTicketViewById.cs
│   ├── GetTicketViews.cs
│   └── GetDefaultViews.cs
└── Services/
    └── ViewFilterBuilder.cs
```

### 4.7 Permission Check Service

**File**: `src/App.Application/Common/Interfaces/ITicketPermissionService.cs`

```csharp
public interface ITicketPermissionService
{
    bool CanManageTickets();
    bool CanManageTeams();
    bool CanAccessReports();
    void RequireCanManageTickets();
    void RequireCanManageTeams();
    void RequireCanAccessReports();
}
```

**File**: `src/App.Application/Common/Services/TicketPermissionService.cs`

```csharp
public class TicketPermissionService : ITicketPermissionService
{
    private readonly ICurrentUser _currentUser;
    private readonly IAppDbContext _db;

    public TicketPermissionService(ICurrentUser currentUser, IAppDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public bool CanManageTickets()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return false;
        
        var user = _db.Users.FirstOrDefault(u => u.Id == _currentUser.UserId.Value.Guid);
        return user?.CanManageTickets ?? false;
    }

    public void RequireCanManageTickets()
    {
        if (!CanManageTickets())
            throw new ForbiddenAccessException("You do not have permission to manage tickets.");
    }
    
    // Similar implementations for CanManageTeams, CanAccessReports
}
```

---

## 5. Phase 4: Staff UI - Tickets

### 5.1 Create Staff Area

**Location**: `src/App.Web/Areas/Staff/`

```
Staff/
├── Pages/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Tickets/
│   │   ├── Index.cshtml
│   │   ├── Index.cshtml.cs
│   │   ├── Create.cshtml
│   │   ├── Create.cshtml.cs
│   │   ├── Details.cshtml
│   │   ├── Details.cshtml.cs
│   │   ├── Edit.cshtml
│   │   ├── Edit.cshtml.cs
│   │   ├── _TicketList.cshtml (partial)
│   │   ├── _TicketChangeLog.cshtml (partial)
│   │   ├── _TicketComments.cshtml (partial)
│   │   └── _TicketSlaInfo.cshtml (partial)
│   └── Shared/
│       ├── _Layout.cshtml
│       ├── _StaffNav.cshtml
│       └── BaseStaffPageModel.cs
```

### 5.2 BaseStaffPageModel

**File**: `src/App.Web/Areas/Staff/Pages/Shared/BaseStaffPageModel.cs`

```csharp
[Authorize]
public abstract class BaseStaffPageModel : BasePageModel
{
    private ITicketPermissionService? _permissionService;
    
    protected ITicketPermissionService PermissionService =>
        _permissionService ??= HttpContext.RequestServices.GetRequiredService<ITicketPermissionService>();
    
    public bool CanManageTickets => PermissionService.CanManageTickets();
    public bool CanManageTeams => PermissionService.CanManageTeams();
    public bool CanAccessReports => PermissionService.CanAccessReports();
}
```

### 5.3 Ticket List Page with Views and Search

**File**: `src/App.Web/Areas/Staff/Pages/Tickets/Index.cshtml.cs`

```csharp
public class Index : BaseStaffPageModel, IHasListView<TicketListItemDto>
{
    [BindProperty(SupportsGet = true)]
    public Guid? ViewId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    
    public ListViewModel<TicketListItemDto> ListView { get; set; } = new();
    public IEnumerable<TicketViewDto> AvailableViews { get; set; } = new List<TicketViewDto>();
    public TicketViewDto? CurrentView { get; set; }

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(new BreadcrumbNode("Tickets", "/staff/tickets"));
        
        // Load available views
        var viewsResponse = await Mediator.Send(new GetTicketViews.Query
        {
            IncludeSystemViews = true,
            OwnerStaffId = CurrentUser.UserId?.Guid
        });
        AvailableViews = viewsResponse.Result.Items;
        
        // Get current view or default
        if (ViewId.HasValue)
        {
            var viewResponse = await Mediator.Send(new GetTicketViewById.Query { Id = ViewId.Value });
            CurrentView = viewResponse.Result;
        }
        else
        {
            CurrentView = AvailableViews.FirstOrDefault(v => v.IsDefault);
        }
        
        // Build query with view filters + search
        var ticketsQuery = new GetTickets.Query
        {
            ViewId = CurrentView?.Id,
            Search = Search,
            SearchColumns = CurrentView?.VisibleColumns, // Search only visible columns
            PageNumber = ListView.PageNumber,
            PageSize = ListView.PageSize,
            OrderBy = ListView.OrderByPropertyName,
            SortOrder = ListView.OrderByDirection
        };
        
        var response = await Mediator.Send(ticketsQuery);
        ListView.SetItems(response.Result.Items, response.Result.TotalCount);
        
        return Page();
    }
}
```

---

## 6. Phase 5: Staff UI - Contacts

### 6.1 Contact Pages Structure

**Location**: `src/App.Web/Areas/Staff/Pages/Contacts/`

```
Contacts/
├── Index.cshtml
├── Index.cshtml.cs
├── Create.cshtml
├── Create.cshtml.cs
├── Details.cshtml
├── Details.cshtml.cs
├── Edit.cshtml
├── Edit.cshtml.cs
├── _ContactTickets.cshtml (partial)
├── _ContactChangeLog.cshtml (partial)
└── _ContactComments.cshtml (partial)
```

### 6.2 Contact Search with Phone Normalization

**File**: `src/App.Web/Areas/Staff/Pages/Contacts/Index.cshtml.cs`

```csharp
public class Index : BaseStaffPageModel, IHasListView<ContactListItemDto>
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    
    public ListViewModel<ContactListItemDto> ListView { get; set; } = new();

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(new BreadcrumbNode("Contacts", "/staff/contacts"));
        
        var query = new SearchContacts.Query
        {
            Search = Search, // Handler normalizes phone numbers automatically
            PageNumber = ListView.PageNumber,
            PageSize = ListView.PageSize,
        };
        
        var response = await Mediator.Send(query);
        ListView.SetItems(response.Result.Items, response.Result.TotalCount);
        
        return Page();
    }
}
```

---

## 7. Phase 6: Admin UI - Configuration

### 7.1 Admin Pages Structure

**Location**: `src/App.Web/Areas/Admin/Pages/` (extend existing)

```
Admin/Pages/
├── Teams/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── Create.cshtml
│   ├── Create.cshtml.cs
│   ├── Edit.cshtml
│   ├── Edit.cshtml.cs
│   ├── Delete.cshtml
│   ├── Delete.cshtml.cs
│   └── Members/
│       ├── Index.cshtml
│       ├── Index.cshtml.cs
│       ├── Add.cshtml
│       ├── Add.cshtml.cs
│       └── Remove.cshtml
├── SlaRules/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── Create.cshtml
│   ├── Create.cshtml.cs
│   ├── Edit.cshtml
│   └── Edit.cshtml.cs
├── TicketViews/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── Create.cshtml
│   ├── Create.cshtml.cs
│   └── Edit.cshtml
├── Reports/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── TeamReport.cshtml
│   ├── TeamReport.cshtml.cs
│   ├── SlaReport.cshtml
│   └── SlaReport.cshtml.cs
└── TicketSettings/
    ├── Index.cshtml
    └── Index.cshtml.cs
```

### 7.2 Permission-Protected Admin Pages

**File**: `src/App.Web/Areas/Admin/Pages/Teams/Index.cshtml.cs`

```csharp
[Authorize]
public class Index : BaseAdminPageModel, IHasListView<TeamDto>
{
    public ListViewModel<TeamDto> ListView { get; set; } = new();

    public async Task<IActionResult> OnGet()
    {
        // Check permission
        var permissionService = HttpContext.RequestServices.GetRequiredService<ITicketPermissionService>();
        if (!permissionService.CanManageTeams())
        {
            return RedirectToPage("/Error403");
        }
        
        SetBreadcrumbs(
            new BreadcrumbNode("Admin", "/admin"),
            new BreadcrumbNode("Teams", "/admin/teams")
        );
        
        var response = await Mediator.Send(new GetTeams.Query
        {
            PageNumber = ListView.PageNumber,
            PageSize = ListView.PageSize,
        });
        
        ListView.SetItems(response.Result.Items, response.Result.TotalCount);
        
        return Page();
    }
}
```

---

## 8. Phase 7: Email Notifications

### 8.1 New Email Templates

**Location**: `src/App.Domain/Entities/DefaultTemplates/`

| Template File | Purpose |
|---------------|---------|
| `email_ticket_assigned.liquid` | Ticket assigned to staff |
| `email_ticket_assignedtoteam.liquid` | Ticket assigned to team |
| `email_ticket_commentadded.liquid` | Comment added to ticket |
| `email_ticket_statuschanged.liquid` | Ticket status changed |
| `email_ticket_closed.liquid` | Ticket closed |
| `email_ticket_reopened.liquid` | Ticket reopened |
| `email_sla_approaching.liquid` | SLA approaching breach |
| `email_sla_breached.liquid` | SLA breached |

### 8.2 Update BuiltInEmailTemplate Value Object

**File**: `src/App.Domain/Entities/EmailTemplate.cs`

Add new static properties to `BuiltInEmailTemplate`:

```csharp
// Add to BuiltInEmailTemplate class

public static BuiltInEmailTemplate TicketAssignedEmail =>
    new(
        "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} assigned to you",
        "email_ticket_assigned",
        false
    );

public static BuiltInEmailTemplate TicketAssignedToTeamEmail =>
    new(
        "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} assigned to your team",
        "email_ticket_assignedtoteam",
        false
    );

public static BuiltInEmailTemplate TicketCommentAddedEmail =>
    new(
        "[{{ CurrentOrganization.OrganizationName }}] New comment on Ticket #{{ Target.TicketId }}",
        "email_ticket_commentadded",
        false
    );

public static BuiltInEmailTemplate TicketStatusChangedEmail =>
    new(
        "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} status changed",
        "email_ticket_statuschanged",
        false
    );

public static BuiltInEmailTemplate TicketClosedEmail =>
    new(
        "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} has been closed",
        "email_ticket_closed",
        false
    );

public static BuiltInEmailTemplate TicketReopenedEmail =>
    new(
        "[{{ CurrentOrganization.OrganizationName }}] Ticket #{{ Target.TicketId }} has been reopened",
        "email_ticket_reopened",
        false
    );

public static BuiltInEmailTemplate SlaApproachingEmail =>
    new(
        "[{{ CurrentOrganization.OrganizationName }}] SLA Warning: Ticket #{{ Target.TicketId }} approaching breach",
        "email_sla_approaching",
        false
    );

public static BuiltInEmailTemplate SlaBreachedEmail =>
    new(
        "[{{ CurrentOrganization.OrganizationName }}] SLA Breached: Ticket #{{ Target.TicketId }}",
        "email_sla_breached",
        true // Safe to CC supervisors
    );

// Update Templates property to yield all new templates
public static IEnumerable<BuiltInEmailTemplate> Templates
{
    get
    {
        // ... existing templates ...
        
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
```

### 8.3 Create RenderModels for Email Templates

**Location**: `src/App.Application/Tickets/RenderModels/`

**File**: `TicketAssigned_RenderModel.cs`

```csharp
public record TicketAssigned_RenderModel
{
    public long TicketId { get; init; }
    public string TicketTitle { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;
    public string AssigneeName { get; init; } = string.Empty;
    public string AssigneeEmail { get; init; } = string.Empty;
    public string? AssignedByName { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? SlaInfo { get; init; }
}
```

**File**: `SlaApproaching_RenderModel.cs`

```csharp
public record SlaApproaching_RenderModel
{
    public long TicketId { get; init; }
    public string TicketTitle { get; init; } = string.Empty;
    public string TicketUrl { get; init; } = string.Empty;
    public string SlaRuleName { get; init; } = string.Empty;
    public DateTime SlaDueAt { get; init; }
    public TimeSpan TimeRemaining { get; init; }
    public string AssigneeName { get; init; } = string.Empty;
    public string AssigneeEmail { get; init; } = string.Empty;
}
```

### 8.4 Create Event Handlers for Notifications

**File**: `src/App.Application/Tickets/EventHandlers/TicketAssignedEventHandler.cs`

```csharp
public class TicketAssignedEventHandler : INotificationHandler<TicketAssignedEvent>
{
    private readonly IEmailer _emailer;
    private readonly IAppDbContext _db;
    private readonly IRenderEngine _renderEngine;
    private readonly IRelativeUrlBuilder _urlBuilder;
    private readonly ICurrentOrganization _currentOrganization;

    public TicketAssignedEventHandler(
        IEmailer emailer,
        IAppDbContext db,
        IRenderEngine renderEngine,
        IRelativeUrlBuilder urlBuilder,
        ICurrentOrganization currentOrganization)
    {
        _emailer = emailer;
        _db = db;
        _renderEngine = renderEngine;
        _urlBuilder = urlBuilder;
        _currentOrganization = currentOrganization;
    }

    public async ValueTask Handle(
        TicketAssignedEvent notification,
        CancellationToken cancellationToken)
    {
        if (notification.Assignee == null)
            return;
        
        // Check user's notification preferences
        var preference = await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => 
                p.StaffAdminId == notification.Assignee.Id && 
                p.EventType == NotificationEventType.TicketAssigned,
                cancellationToken);
        
        if (preference?.EmailEnabled != true)
            return;
        
        var template = _db.EmailTemplates.First(p =>
            p.DeveloperName == BuiltInEmailTemplate.TicketAssignedEmail);
        
        var renderModel = new TicketAssigned_RenderModel
        {
            TicketId = notification.Ticket.Id,
            TicketTitle = notification.Ticket.Title,
            TicketUrl = _urlBuilder.StaffTicketDetailsUrl(notification.Ticket.Id),
            AssigneeName = notification.Assignee.FullName,
            AssigneeEmail = notification.Assignee.EmailAddress,
            AssignedByName = notification.AssignedBy?.FullName,
            Priority = notification.Ticket.Priority.DeveloperName,
            Status = notification.Ticket.Status.DeveloperName,
            ContactName = notification.Ticket.Contact?.Name,
        };
        
        var wrappedModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(_currentOrganization),
            Target = renderModel,
        };
        
        var subject = _renderEngine.RenderAsHtml(template.Subject, wrappedModel);
        var content = _renderEngine.RenderAsHtml(template.Content, wrappedModel);
        
        _emailer.SendEmail(new EmailMessage
        {
            To = new List<string> { notification.Assignee.EmailAddress },
            Subject = subject,
            Content = content,
        });
    }
}
```

### 8.5 Example Liquid Template

**File**: `src/App.Domain/Entities/DefaultTemplates/email_ticket_assigned.liquid`

```liquid
<p>Hello {{ Target.AssigneeName }},</p>

<p>A ticket has been assigned to you{% if Target.AssignedByName %} by {{ Target.AssignedByName }}{% endif %}.</p>

<table style="border-collapse: collapse; margin: 20px 0;">
  <tr>
    <td style="padding: 8px; font-weight: bold;">Ticket #:</td>
    <td style="padding: 8px;">{{ Target.TicketId }}</td>
  </tr>
  <tr>
    <td style="padding: 8px; font-weight: bold;">Title:</td>
    <td style="padding: 8px;">{{ Target.TicketTitle }}</td>
  </tr>
  <tr>
    <td style="padding: 8px; font-weight: bold;">Priority:</td>
    <td style="padding: 8px;">{{ Target.Priority }}</td>
  </tr>
  <tr>
    <td style="padding: 8px; font-weight: bold;">Status:</td>
    <td style="padding: 8px;">{{ Target.Status }}</td>
  </tr>
  {% if Target.ContactName %}
  <tr>
    <td style="padding: 8px; font-weight: bold;">Contact:</td>
    <td style="padding: 8px;">{{ Target.ContactName }}</td>
  </tr>
  {% endif %}
  {% if Target.SlaInfo %}
  <tr>
    <td style="padding: 8px; font-weight: bold;">SLA:</td>
    <td style="padding: 8px;">{{ Target.SlaInfo }}</td>
  </tr>
  {% endif %}
</table>

<p>
  <a href="{{ Target.TicketUrl }}" style="background-color: #0066cc; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;">
    View Ticket
  </a>
</p>

<p>Thank you,<br/>
{{ CurrentOrganization.OrganizationName }}</p>
```

### 8.6 Update IRelativeUrlBuilder

**File**: `src/App.Application/Common/Interfaces/IRelativeUrlBuilder.cs`

Add new URL builder methods:

```csharp
// Add to IRelativeUrlBuilder interface
string StaffTicketDetailsUrl(long ticketId);
string StaffContactDetailsUrl(long contactId);
string AdminTeamEditUrl(Guid teamId);
```

---

## 9. Phase 8: Background Jobs & SLA Processing

### 9.1 SLA Evaluation Background Job

**File**: `src/App.Application/BackgroundTasks/Jobs/SlaEvaluationJob.cs`

```csharp
public class SlaEvaluationJob : IBackgroundJob
{
    private readonly IAppDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<SlaEvaluationJob> _logger;

    public SlaEvaluationJob(
        IAppDbContext db,
        IMediator mediator,
        ILogger<SlaEvaluationJob> logger)
    {
        _db = db;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SLA evaluation job");
        
        var now = DateTime.UtcNow;
        
        // Get tickets with active SLAs that aren't completed
        var tickets = await _db.Tickets
            .Where(t => t.SlaDueAt != null 
                && t.SlaStatus != SlaStatus.Completed 
                && t.SlaStatus != SlaStatus.Breached
                && !t.IsDeleted)
            .Include(t => t.Assignee)
            .Include(t => t.SlaRule)
            .ToListAsync(cancellationToken);
        
        foreach (var ticket in tickets)
        {
            var timeRemaining = ticket.SlaDueAt!.Value - now;
            
            if (timeRemaining <= TimeSpan.Zero)
            {
                // Breached
                if (ticket.SlaStatus != SlaStatus.Breached)
                {
                    ticket.SlaStatus = SlaStatus.Breached;
                    ticket.SlaBreachedAt = now;
                    ticket.AddDomainEvent(new SlaBreachedEvent(ticket));
                    
                    _logger.LogWarning("Ticket {TicketId} SLA breached", ticket.Id);
                }
            }
            else if (timeRemaining <= TimeSpan.FromHours(1) || 
                     timeRemaining <= (ticket.SlaRule?.TargetResolutionTime * 0.25))
            {
                // Approaching breach
                if (ticket.SlaStatus != SlaStatus.ApproachingBreach)
                {
                    ticket.SlaStatus = SlaStatus.ApproachingBreach;
                    ticket.AddDomainEvent(new SlaApproachingBreachEvent(ticket, timeRemaining));
                    
                    _logger.LogWarning("Ticket {TicketId} SLA approaching breach, {TimeRemaining} remaining", 
                        ticket.Id, timeRemaining);
                }
            }
        }
        
        await _db.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("SLA evaluation job completed. Processed {Count} tickets", tickets.Count);
    }
}
```

### 9.2 Register Background Job

**File**: `src/App.Infrastructure/BackgroundTasks/QueuedHostedService.cs`

Add SLA job to the scheduled tasks that run periodically (e.g., every 5 minutes).

### 9.3 Round Robin Assignment Service

**File**: `src/App.Application/Teams/Services/RoundRobinService.cs`

```csharp
public class RoundRobinService : IRoundRobinService
{
    private readonly IAppDbContext _db;
    private static readonly object _lock = new();

    public RoundRobinService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetNextAssigneeForTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await _db.Teams
            .Include(t => t.Memberships)
                .ThenInclude(m => m.StaffAdmin)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        
        if (team == null || !team.RoundRobinEnabled)
            return null;
        
        var eligibleMembers = team.Memberships
            .Where(m => m.IsAssignable && m.StaffAdmin.IsActive)
            .OrderBy(m => m.LastAssignedAt ?? DateTime.MinValue)
            .ToList();
        
        if (!eligibleMembers.Any())
            return null;
        
        var nextMember = eligibleMembers.First();
        nextMember.LastAssignedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync(cancellationToken);
        
        return nextMember.StaffAdmin;
    }
}
```

---

## 10. Phase 9: Metrics & Reporting

### 10.1 Dashboard Queries

**File**: `src/App.Application/Tickets/Queries/GetUserDashboardMetrics.cs`

```csharp
public class GetUserDashboardMetrics
{
    public record Query : IRequest<IQueryResponseDto<UserDashboardMetricsDto>>
    {
        public Guid? UserId { get; init; } // If null, uses current user
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<UserDashboardMetricsDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<UserDashboardMetricsDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var userId = request.UserId ?? _currentUser.UserId?.Guid;
            if (!userId.HasValue)
                throw new ForbiddenAccessException();
            
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);
            var thirtyDaysAgo = now.AddDays(-30);
            
            var openTickets = await _db.Tickets
                .CountAsync(t => t.AssigneeId == userId && 
                    t.Status != TicketStatus.Closed, cancellationToken);
            
            var resolved7Days = await _db.Tickets
                .CountAsync(t => t.AssigneeId == userId && 
                    t.ResolvedAt >= sevenDaysAgo, cancellationToken);
            
            var resolved30Days = await _db.Tickets
                .CountAsync(t => t.AssigneeId == userId && 
                    t.ResolvedAt >= thirtyDaysAgo, cancellationToken);
            
            // Calculate median close time
            var closedTickets = await _db.Tickets
                .Where(t => t.AssigneeId == userId && t.ClosedAt != null)
                .Select(t => new { t.CreationTime, t.ClosedAt })
                .ToListAsync(cancellationToken);
            
            var closeTimes = closedTickets
                .Select(t => (t.ClosedAt!.Value - t.CreationTime).TotalHours)
                .OrderBy(h => h)
                .ToList();
            
            double medianCloseTimeHours = 0;
            if (closeTimes.Any())
            {
                int mid = closeTimes.Count / 2;
                medianCloseTimeHours = closeTimes.Count % 2 == 0
                    ? (closeTimes[mid - 1] + closeTimes[mid]) / 2
                    : closeTimes[mid];
            }
            
            // Calculate reopen rate
            var reopenedCount = await _db.TicketChangeLogEntries
                .CountAsync(e => e.Ticket.AssigneeId == userId && 
                    e.Message == "Ticket reopened", cancellationToken);
            
            var totalClosed = closedTickets.Count;
            var reopenRate = totalClosed > 0 
                ? (double)reopenedCount / totalClosed * 100 
                : 0;
            
            return new QueryResponseDto<UserDashboardMetricsDto>(new UserDashboardMetricsDto
            {
                OpenAssignedTickets = openTickets,
                Resolved7Days = resolved7Days,
                Resolved30Days = resolved30Days,
                MedianCloseTimeHours = medianCloseTimeHours,
                ReopenRatePercent = reopenRate,
            });
        }
    }
}
```

### 10.2 Team Reports (Access Reports Required)

**File**: `src/App.Application/Tickets/Queries/GetTeamReport.cs`

```csharp
public class GetTeamReport
{
    public record Query : IRequest<IQueryResponseDto<TeamReportDto>>
    {
        public Guid TeamId { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator(ITicketPermissionService permissionService)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                if (!permissionService.CanAccessReports())
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, 
                        "You do not have permission to access reports.");
                }
            });
        }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<TeamReportDto>>
    {
        // Implementation with team-level metrics
    }
}
```

---

## 11. Phase 10: Testing

### 11.1 Unit Tests

**Location**: `tests/App.Domain.UnitTests/`

| Test File | Coverage |
|-----------|----------|
| `ValueObjects/TicketStatusTests.cs` | Status value object |
| `ValueObjects/TicketPriorityTests.cs` | Priority value object |
| `ValueObjects/SlaStatusTests.cs` | SLA status value object |
| `Entities/TicketTests.cs` | Ticket entity behavior |
| `Entities/ContactTests.cs` | Contact entity behavior |

**Location**: `tests/App.Application.UnitTests/`

| Test File | Coverage |
|-----------|----------|
| `Tickets/CreateTicketValidatorTests.cs` | Validation rules |
| `Tickets/CreateTicketHandlerTests.cs` | Handler logic |
| `Contacts/PhoneNumberNormalizerTests.cs` | Phone normalization |
| `Teams/RoundRobinServiceTests.cs` | Round robin logic |
| `SlaRules/SlaEvaluationServiceTests.cs` | SLA evaluation |

### 11.2 Email Template Tests

**File**: `tests/App.Application.UnitTests/Tickets/EmailTemplateRenderTests.cs`

```csharp
[TestFixture]
public class EmailTemplateRenderTests
{
    [Test]
    public void TicketAssigned_Template_RendersCorrectly()
    {
        // Arrange
        var renderEngine = new DotLiquidRenderEngine();
        var template = BuiltInEmailTemplate.TicketAssignedEmail.DefaultContent;
        
        var model = new Wrapper_RenderModel
        {
            CurrentOrganization = new CurrentOrganization_RenderModel
            {
                OrganizationName = "Test DME Company"
            },
            Target = new TicketAssigned_RenderModel
            {
                TicketId = 12345,
                TicketTitle = "Equipment delivery delayed",
                AssigneeName = "John Smith",
                Priority = "High",
                Status = "Open",
                TicketUrl = "https://example.com/staff/tickets/12345"
            }
        };
        
        // Act
        var result = renderEngine.RenderAsHtml(template, model);
        
        // Assert
        Assert.That(result, Does.Contain("Hello John Smith"));
        Assert.That(result, Does.Contain("12345"));
        Assert.That(result, Does.Contain("Equipment delivery delayed"));
        Assert.That(result, Does.Contain("High"));
    }
    
    [Test]
    public void SlaApproaching_Template_RendersCorrectly()
    {
        // Similar test for SLA approaching template
    }
    
    [Test]
    public void SlaBreach_Template_RendersCorrectly()
    {
        // Similar test for SLA breach template
    }
}
```

### 11.3 Integration Tests

**Location**: `tests/App.Infrastructure.IntegrationTests/` (create if needed)

| Test File | Coverage |
|-----------|----------|
| `Persistence/TicketRepositoryTests.cs` | Ticket CRUD with numeric IDs |
| `Persistence/ContactRepositoryTests.cs` | Contact CRUD with numeric IDs |
| `BackgroundTasks/SlaEvaluationJobTests.cs` | SLA job execution |

---

## 12. Integration Points & Special Considerations

### 12.1 Numeric ID Conflicts with GUID-Based Helpers

**Issue**: Existing `BaseEntityDto` uses `ShortGuid` for `Id`, but Tickets and Contacts need `long`.

**Resolution**:

1. Create `BaseNumericEntityDto` and `BaseNumericAuditableEntityDto` (Phase 4.1)
2. Ticket and Contact DTOs inherit from numeric base classes
3. Commands that return numeric IDs use `CommandResponseDto<long>` instead of `CommandResponseDto<ShortGuid>`
4. PageModels handle `long` IDs in route parameters

**Affected Files**:
- `src/App.Application/Common/Models/BaseNumericEntityDto.cs` (new)
- `src/App.Application/Tickets/TicketDto.cs` (uses numeric base)
- `src/App.Application/Contacts/ContactDto.cs` (uses numeric base)
- All ticket/contact command handlers (return `long`)
- All ticket/contact Razor Pages (route parameters as `long`)

### 12.2 Email Template Integration

**Integration Points**:

1. **Template Registration**: Add new templates to `BuiltInEmailTemplate.Templates` property
2. **Liquid Files**: Create `.liquid` files in `DefaultTemplates/` folder
3. **RenderModels**: Create in `Tickets/RenderModels/` following existing pattern
4. **Event Handlers**: Implement `INotificationHandler<TEvent>` for each notification event
5. **Notification Preferences**: Query `NotificationPreferences` table before sending
6. **URL Builder**: Extend `IRelativeUrlBuilder` with ticket/contact URLs

**Database Seeding**: Email templates are auto-seeded from `BuiltInEmailTemplate.Templates` on startup.

### 12.3 Auditable Entity Interceptor

**Issue**: Interceptor assumes GUID-based `IAuditable` entities.

**Resolution**: The interceptor uses interfaces (`ICreationAuditable`, `IModificationAuditable`), not base classes. `BaseNumericAuditableEntity` implements these interfaces, so no changes needed to the interceptor.

### 12.4 Domain Event Dispatching

**Current Pattern**: `MediatorExtensions.DispatchDomainEventsBeforeSaveChanges` and `DispatchDomainEventsAfterSaveChanges` dispatch events from entities.

**Resolution**: `BaseNumericEntity` includes the same `_domainEvents` list and methods as `BaseEntity`, so domain events work identically.

### 12.5 Soft Delete Query Filters

**Implementation**: Entity configurations include `HasQueryFilter(t => !t.IsDeleted)` for Tickets and Contacts.

**Special Handling**: Admin queries that need deleted items use `.IgnoreQueryFilters()`.

---

## Implementation Order Summary

| Phase | Priority | Dependencies | Estimated Effort |
|-------|----------|--------------|------------------|
| 1. Domain Model | P1 | None | 2-3 days |
| 2. Persistence Layer | P1 | Phase 1 | 2 days |
| 3. Application Layer | P1 | Phase 2 | 4-5 days |
| 4. Staff UI - Tickets | P1 | Phase 3 | 3-4 days |
| 5. Staff UI - Contacts | P1 | Phase 3 | 2-3 days |
| 6. Admin UI | P2 | Phase 3 | 3-4 days |
| 7. Email Notifications | P2 | Phase 3 | 2-3 days |
| 8. Background Jobs | P2 | Phase 3, 7 | 2 days |
| 9. Metrics & Reporting | P3 | Phase 3 | 2-3 days |
| 10. Testing | Ongoing | All phases | 3-4 days |

**Total Estimated Effort**: 25-33 days

---

## Files to Create/Modify Summary

### New Files (by layer)

**Domain (14 files)**:
- `Common/BaseNumericEntity.cs`
- `Common/BaseNumericAuditableEntity.cs`
- `Common/BaseNumericFullAuditableEntity.cs`
- `Entities/Ticket.cs`
- `Entities/Contact.cs`
- `Entities/Team.cs`
- `Entities/TeamMembership.cs`
- `Entities/TicketChangeLogEntry.cs`
- `Entities/TicketComment.cs`
- `Entities/TicketAttachment.cs`
- `Entities/ContactChangeLogEntry.cs`
- `Entities/ContactComment.cs`
- `Entities/SlaRule.cs`
- `Entities/TicketView.cs`
- `Entities/NotificationPreference.cs`
- `ValueObjects/TicketStatus.cs`
- `ValueObjects/TicketPriority.cs`
- `ValueObjects/SlaStatus.cs`
- `ValueObjects/NotificationEventType.cs`
- `Events/TicketCreatedEvent.cs` (+ 9 more events)
- `Entities/DefaultTemplates/email_ticket_*.liquid` (8 files)

**Application (~50 files)**:
- `Common/Models/BaseNumericEntityDto.cs`
- `Common/Interfaces/ITicketPermissionService.cs`
- `Tickets/` (DTOs, Commands, Queries, EventHandlers, RenderModels)
- `Contacts/` (DTOs, Commands, Queries, Utils)
- `Teams/` (DTOs, Commands, Queries, Services)
- `SlaRules/` (DTOs, Commands, Queries, Services)
- `TicketViews/` (DTOs, Commands, Queries, Services)

**Infrastructure (~15 files)**:
- `Persistence/Configurations/TicketConfiguration.cs`
- `Persistence/Configurations/ContactConfiguration.cs`
- (+ 10 more configurations)
- `BackgroundTasks/Jobs/SlaEvaluationJob.cs`

**Web (~60 files)**:
- `Areas/Staff/Pages/` (complete Staff area)
- `Areas/Admin/Pages/Teams/`
- `Areas/Admin/Pages/SlaRules/`
- `Areas/Admin/Pages/Reports/`
- `Areas/Admin/Pages/TicketViews/`

### Modified Files

- `src/App.Domain/Entities/User.cs` (add permission flags)
- `src/App.Domain/Entities/EmailTemplate.cs` (add new template definitions)
- `src/App.Application/Common/Interfaces/IRaythaDbContext.cs` (add DbSets)
- `src/App.Infrastructure/Persistence/AppDbContext.cs` (add DbSets)
- `src/App.Application/Common/Interfaces/IRelativeUrlBuilder.cs` (add URL methods)
- `src/App.Web/Services/RelativeUrlBuilder.cs` (implement new URLs)
- `src/App.Infrastructure/ConfigureServices.cs` (register new services)
- `src/App.Application/ConfigureServices.cs` (register new services)

