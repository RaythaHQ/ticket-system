using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Notifications.Commands;

public class MarkAllNotificationsAsRead
{
    public record Command : IRequest<CommandResponseDto<int>>
    {
        /// <summary>
        /// Filter by read status: "all", "unread", "read".
        /// Default: "unread" (only unread notifications).
        /// </summary>
        public string? FilterStatus { get; init; } = "unread";

        /// <summary>
        /// Filter by notification type (NotificationEventType.DeveloperName).
        /// If null/empty, applies to all types.
        /// </summary>
        public string? FilterType { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FilterStatus)
                .Must(status => string.IsNullOrEmpty(status) ||
                    status.ToLower() == "all" ||
                    status.ToLower() == "unread" ||
                    status.ToLower() == "read")
                .WithMessage("Invalid filter status. Use 'all', 'unread', or 'read'.");

            RuleFor(x => x.FilterType)
                .Must(type =>
                {
                    if (string.IsNullOrEmpty(type)) return true;
                    try
                    {
                        NotificationEventType.From(type);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage("Invalid notification type.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<int>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IInAppNotificationService _notificationService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            IInAppNotificationService notificationService)
        {
            _db = db;
            _currentUser = currentUser;
            _notificationService = notificationService;
        }

        public async ValueTask<CommandResponseDto<int>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUser.UserId?.Guid;
            if (!userId.HasValue)
            {
                throw new ForbiddenAccessException();
            }

            var query = _db.Notifications
                .Where(n => n.RecipientUserId == userId.Value);

            // Apply status filter - we only want to mark unread notifications
            // unless explicitly showing "read" filter (which wouldn't make sense to mark as read)
            var filterStatus = (request.FilterStatus ?? "unread").ToLower();
            switch (filterStatus)
            {
                case "unread":
                    query = query.Where(n => !n.IsRead);
                    break;
                case "read":
                    // Already read - nothing to do
                    return new CommandResponseDto<int>(0);
                case "all":
                default:
                    // Mark all unread as read
                    query = query.Where(n => !n.IsRead);
                    break;
            }

            // Apply type filter
            if (!string.IsNullOrEmpty(request.FilterType))
            {
                query = query.Where(n => n.EventType == request.FilterType.ToLower());
            }

            var now = DateTime.UtcNow;
            var notifications = await query.ToListAsync(cancellationToken);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _db.SaveChangesAsync(cancellationToken);

            // Broadcast updated unread count
            await _notificationService.BroadcastUnreadCountUpdateAsync(userId.Value, cancellationToken);

            return new CommandResponseDto<int>(notifications.Count);
        }
    }
}

