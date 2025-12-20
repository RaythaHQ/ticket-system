using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.NotificationPreferences.Commands;

public class UpdateNotificationPreferences
{
    public record Command : LoggableRequest<CommandResponseDto<bool>>
    {
        public ShortGuid StaffAdminId { get; init; }
        public List<PreferenceUpdate> Preferences { get; init; } = new();
        public bool PlaySoundOnNotification { get; init; } = true;
    }

    public record PreferenceUpdate
    {
        public string EventType { get; init; } = string.Empty;
        public bool EmailEnabled { get; init; }
        public bool InAppEnabled { get; init; }
        public bool WebhookEnabled { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.StaffAdminId).NotEmpty();
            RuleFor(x => x.Preferences).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<bool>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<bool>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Users can only update their own preferences
            if (_currentUser.UserIdAsGuid != request.StaffAdminId.Guid)
            {
                throw new UnauthorizedAccessException(
                    "Cannot update preferences for another user."
                );
            }

            var existingPrefs = await _db
                .NotificationPreferences.Where(p => p.StaffAdminId == request.StaffAdminId.Guid)
                .ToListAsync(cancellationToken);

            foreach (var update in request.Preferences)
            {
                var existing = existingPrefs.FirstOrDefault(p => p.EventType == update.EventType);

                if (existing != null)
                {
                    existing.EmailEnabled = update.EmailEnabled;
                    existing.InAppEnabled = update.InAppEnabled;
                    existing.WebhookEnabled = update.WebhookEnabled;
                }
                else
                {
                    _db.NotificationPreferences.Add(
                        new NotificationPreference
                        {
                            Id = Guid.NewGuid(),
                            StaffAdminId = request.StaffAdminId.Guid,
                            EventType = update.EventType,
                            EmailEnabled = update.EmailEnabled,
                            InAppEnabled = update.InAppEnabled,
                            WebhookEnabled = update.WebhookEnabled,
                        }
                    );
                }
            }

            // Update user's sound preference
            var user = await _db.Users.FindAsync(
                new object[] { request.StaffAdminId.Guid },
                cancellationToken
            );
            if (user != null)
            {
                user.PlaySoundOnNotification = request.PlaySoundOnNotification;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<bool>(true);
        }
    }
}
