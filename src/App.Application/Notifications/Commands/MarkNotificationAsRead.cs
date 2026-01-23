using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Notifications.Commands;

public class MarkNotificationAsRead
{
    public record Command : IRequest<CommandResponseDto<bool>>
    {
        /// <summary>
        /// The notification ID to mark as read.
        /// </summary>
        public ShortGuid Id { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Notification ID is required.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<bool>>
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

        public async ValueTask<CommandResponseDto<bool>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUser.UserId?.Guid;
            if (!userId.HasValue)
            {
                throw new ForbiddenAccessException();
            }

            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.Id.Guid, cancellationToken);

            if (notification == null)
            {
                throw new NotFoundException("Notification", request.Id.ToString());
            }

            // Verify ownership
            if (notification.RecipientUserId != userId.Value)
            {
                throw new ForbiddenAccessException("You do not have permission to access this notification.");
            }

            // Already read - no-op
            if (notification.IsRead)
            {
                return new CommandResponseDto<bool>(true);
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            // Broadcast updated unread count
            await _notificationService.BroadcastUnreadCountUpdateAsync(userId.Value, cancellationToken);

            return new CommandResponseDto<bool>(true);
        }
    }
}

