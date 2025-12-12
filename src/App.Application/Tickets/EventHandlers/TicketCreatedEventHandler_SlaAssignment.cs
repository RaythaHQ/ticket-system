using App.Application.Common.Interfaces;
using App.Domain.Entities;
using App.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Assigns SLA to newly created tickets based on matching rules.
/// </summary>
public class TicketCreatedEventHandler_SlaAssignment : INotificationHandler<TicketCreatedEvent>
{
    private readonly ISlaService _slaService;
    private readonly ILogger<TicketCreatedEventHandler_SlaAssignment> _logger;

    public TicketCreatedEventHandler_SlaAssignment(
        ISlaService slaService,
        ILogger<TicketCreatedEventHandler_SlaAssignment> logger)
    {
        _slaService = slaService;
        _logger = logger;
    }

    public async ValueTask Handle(TicketCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _slaService.EvaluateAndAssignSlaAsync(notification.Ticket, cancellationToken);
            if (rule != null)
            {
                _logger.LogInformation(
                    "Assigned SLA rule '{RuleName}' to ticket {TicketId}. Due at: {DueAt}",
                    rule.Name,
                    notification.Ticket.Id,
                    notification.Ticket.SlaDueAt
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign SLA to ticket {TicketId}", notification.Ticket.Id);
        }
    }
}

