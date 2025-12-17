using App.Application.Webhooks;
using App.Application.Webhooks.Services;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;

namespace App.Infrastructure.Services;

/// <summary>
/// Builds webhook payloads from domain entities.
/// </summary>
public class WebhookPayloadBuilder : IWebhookPayloadBuilder
{
    public WebhookPayloadDto BuildTicketCreatedPayload(Ticket ticket)
    {
        return new WebhookPayloadDto
        {
            Event = WebhookTriggerType.TICKET_CREATED,
            Timestamp = DateTime.UtcNow,
            TicketId = ticket.Id,
            Old = null,
            New = MapTicketToDto(ticket),
        };
    }

    public WebhookPayloadDto BuildTicketUpdatedPayload(
        Ticket ticket,
        string? oldTitle,
        string? oldDescription,
        string? oldPriority
    )
    {
        // Build the "old" snapshot with the previous values
        var oldSnapshot = MapTicketToDto(ticket);
        if (oldTitle != null)
        {
            oldSnapshot = oldSnapshot with { Title = oldTitle };
        }
        if (oldDescription != null)
        {
            oldSnapshot = oldSnapshot with { Description = oldDescription };
        }
        if (oldPriority != null)
        {
            oldSnapshot = oldSnapshot with
            {
                Priority = oldPriority,
                PriorityLabel = TicketPriority.From(oldPriority).Label,
            };
        }

        return new WebhookPayloadDto
        {
            Event = WebhookTriggerType.TICKET_UPDATED,
            Timestamp = DateTime.UtcNow,
            TicketId = ticket.Id,
            Old = oldSnapshot,
            New = MapTicketToDto(ticket),
        };
    }

    public WebhookPayloadDto BuildTicketStatusChangedPayload(Ticket ticket, string oldStatus)
    {
        var oldSnapshot = MapTicketToDto(ticket) with
        {
            Status = oldStatus,
            StatusLabel = TicketStatus.From(oldStatus).Label,
        };

        return new WebhookPayloadDto
        {
            Event = WebhookTriggerType.TICKET_STATUS_CHANGED,
            Timestamp = DateTime.UtcNow,
            TicketId = ticket.Id,
            Old = oldSnapshot,
            New = MapTicketToDto(ticket),
        };
    }

    public WebhookPayloadDto BuildTicketAssigneeChangedPayload(
        Ticket ticket,
        Guid? oldAssigneeId,
        string? oldAssigneeName,
        Guid? oldTeamId,
        string? oldTeamName
    )
    {
        var oldSnapshot = MapTicketToDto(ticket) with
        {
            AssigneeId = oldAssigneeId.HasValue
                ? ((ShortGuid)oldAssigneeId.Value).ToString()
                : null,
            AssigneeName = oldAssigneeName,
            TeamId = oldTeamId.HasValue ? ((ShortGuid)oldTeamId.Value).ToString() : null,
            TeamName = oldTeamName,
        };

        return new WebhookPayloadDto
        {
            Event = WebhookTriggerType.TICKET_ASSIGNEE_CHANGED,
            Timestamp = DateTime.UtcNow,
            TicketId = ticket.Id,
            Old = oldSnapshot,
            New = MapTicketToDto(ticket),
        };
    }

    public WebhookCommentPayloadDto BuildCommentAddedPayload(Ticket ticket, TicketComment comment)
    {
        return new WebhookCommentPayloadDto
        {
            Event = WebhookTriggerType.COMMENT_ADDED,
            Timestamp = DateTime.UtcNow,
            TicketId = ticket.Id,
            Old = MapTicketToDto(ticket),
            New = MapTicketToDto(ticket),
            Comment = new WebhookCommentDto
            {
                Id = ((ShortGuid)comment.Id).ToString(),
                Content = comment.Body,
                IsInternal = false, // TicketComment doesn't have IsInternal flag
                AuthorId = ((ShortGuid)comment.AuthorStaffId).ToString(),
                AuthorName = comment.AuthorStaff?.FullName,
                CreatedAt = comment.CreationTime,
            },
        };
    }

    public WebhookPayloadDto BuildTestPayload(string triggerType)
    {
        var testTicket = new WebhookTicketDto
        {
            Id = 12345,
            Title = "Test Ticket - Webhook Verification",
            Description = "This is a test payload to verify webhook connectivity.",
            Status = TicketStatus.OPEN,
            StatusLabel = TicketStatus.Open.Label,
            Priority = TicketPriority.NORMAL,
            PriorityLabel = TicketPriority.Normal.Label,
            Category = "General",
            Tags = new List<string> { "test", "webhook" },
            AssigneeId = ((ShortGuid)Guid.NewGuid()).ToString(),
            AssigneeName = "Test User",
            TeamId = ((ShortGuid)Guid.NewGuid()).ToString(),
            TeamName = "Test Team",
            ContactId = 1,
            ContactName = "Test Contact",
            ContactEmail = "test@example.com",
            ContactPrimaryPhone = "+1234567890",
            SlaStatus = SlaStatus.ON_TRACK,
            SlaDueAt = DateTime.UtcNow.AddHours(4),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow,
        };

        return new WebhookPayloadDto
        {
            Event = triggerType,
            Timestamp = DateTime.UtcNow,
            TicketId = 12345,
            Old = triggerType == WebhookTriggerType.TICKET_CREATED ? null : testTicket,
            New = testTicket,
        };
    }

    private static WebhookTicketDto MapTicketToDto(Ticket ticket)
    {
        return new WebhookTicketDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            StatusLabel = ticket.StatusValue.Label,
            Priority = ticket.Priority,
            PriorityLabel = ticket.PriorityValue.Label,
            Category = ticket.Category,
            Tags = ticket.Tags,
            AssigneeId = ticket.AssigneeId.HasValue
                ? ((ShortGuid)ticket.AssigneeId.Value).ToString()
                : null,
            AssigneeName = ticket.Assignee?.FullName,
            TeamId = ticket.OwningTeamId.HasValue
                ? ((ShortGuid)ticket.OwningTeamId.Value).ToString()
                : null,
            TeamName = ticket.OwningTeam?.Name,
            ContactId = ticket.ContactId,
            ContactName = ticket.Contact?.FullName,
            ContactEmail = ticket.Contact?.Email,
            ContactPrimaryPhone = ticket.Contact?.PhoneNumbers?.FirstOrDefault(),
            SlaStatus = ticket.SlaStatus,
            SlaDueAt = ticket.SlaDueAt,
            CreatedAt = ticket.CreationTime,
            UpdatedAt = ticket.LastModificationTime,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
        };
    }
}
