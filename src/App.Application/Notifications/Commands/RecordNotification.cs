using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using FluentValidation;
using Mediator;

namespace App.Application.Notifications.Commands;

public class RecordNotification
{
    public record Command : IRequest<CommandResponseDto<Guid>>
    {
        /// <summary>
        /// The user ID to create the notification for.
        /// </summary>
        public Guid RecipientUserId { get; init; }

        /// <summary>
        /// The notification event type (from NotificationEventType).
        /// </summary>
        public string EventType { get; init; } = string.Empty;

        /// <summary>
        /// Short title for display.
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Full notification message.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Optional URL to navigate to.
        /// </summary>
        public string? Url { get; init; }

        /// <summary>
        /// Optional related ticket ID.
        /// </summary>
        public long? TicketId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RecipientUserId)
                .NotEmpty()
                .WithMessage("Recipient user ID is required.");

            RuleFor(x => x.EventType)
                .NotEmpty()
                .Must(eventType =>
                {
                    try
                    {
                        NotificationEventType.From(eventType);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage("Invalid notification event type.");

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Message)
                .NotEmpty()
                .MaximumLength(1000)
                .WithMessage("Message cannot exceed 1000 characters.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<Guid>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<Guid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = request.RecipientUserId,
                EventType = request.EventType.ToLower(),
                Title = request.Title,
                Message = request.Message,
                Url = request.Url,
                TicketId = request.TicketId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<Guid>(notification.Id);
        }
    }
}

