using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class CreateTicket
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public string Title { get; init; } = null!;
        public string? Description { get; init; }
        public string Priority { get; init; } = TicketPriority.NORMAL;
        public string? Category { get; init; }
        public List<string>? Tags { get; init; }
        public Guid? OwningTeamId { get; init; }
        public Guid? AssigneeId { get; init; }
        public long? ContactId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Priority)
                .Must(p => TicketPriority.SupportedTypes.Any(t => t.DeveloperName == p))
                .WithMessage("Invalid priority value.");

            RuleFor(x => x.OwningTeamId)
                .MustAsync(async (teamId, cancellationToken) =>
                {
                    if (!teamId.HasValue) return true;
                    return await db.Teams.AsNoTracking().AnyAsync(t => t.Id == teamId.Value, cancellationToken);
                })
                .WithMessage("Team not found.");

            RuleFor(x => x.AssigneeId)
                .MustAsync(async (assigneeId, cancellationToken) =>
                {
                    if (!assigneeId.HasValue) return true;
                    return await db.Users.AsNoTracking().AnyAsync(u => u.Id == assigneeId.Value, cancellationToken);
                })
                .WithMessage("Assignee not found.");

            RuleFor(x => x.ContactId)
                .MustAsync(async (contactId, cancellationToken) =>
                {
                    if (!contactId.HasValue) return true;
                    return await db.Contacts.AsNoTracking().AnyAsync(c => c.Id == contactId.Value, cancellationToken);
                })
                .WithMessage("Contact not found.");
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
            CancellationToken cancellationToken
        )
        {
            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description,
                Status = TicketStatus.OPEN,
                Priority = request.Priority,
                Category = request.Category,
                Tags = request.Tags ?? new List<string>(),
                OwningTeamId = request.OwningTeamId,
                AssigneeId = request.AssigneeId,
                ContactId = request.ContactId,
                CreatedByStaffId = _currentUser.UserId?.Guid
            };

            ticket.AddDomainEvent(new TicketCreatedEvent(ticket));

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync(cancellationToken);

            // Add initial change log entry after save so we have the TicketId
            var changeLog = new TicketChangeLogEntry
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserId?.Guid,
                Message = "Ticket created"
            };

            _db.TicketChangeLogEntries.Add(changeLog);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}

