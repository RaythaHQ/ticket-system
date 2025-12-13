using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.NotificationPreferences.Queries;

public class GetNotificationPreferences
{
    public record Query : IRequest<QueryResponseDto<List<NotificationPreferenceDto>>>
    {
        public ShortGuid StaffAdminId { get; init; }
    }

    public class Handler : IRequestHandler<Query, QueryResponseDto<List<NotificationPreferenceDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<QueryResponseDto<List<NotificationPreferenceDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Get existing preferences
            var existingPrefs = await _db.NotificationPreferences
                .AsNoTracking()
                .Where(p => p.StaffAdminId == request.StaffAdminId.Guid)
                .ToListAsync(cancellationToken);

            // Build list with all event types, using defaults if no preference exists
            var allEventTypes = NotificationEventType.SupportedTypes;
            var result = new List<NotificationPreferenceDto>();

            foreach (var eventType in allEventTypes)
            {
                var existing = existingPrefs.FirstOrDefault(p => p.EventType == eventType.DeveloperName);

                result.Add(new NotificationPreferenceDto
                {
                    Id = existing?.Id ?? Guid.Empty,
                    StaffAdminId = request.StaffAdminId,
                    EventType = eventType.DeveloperName,
                    EventTypeLabel = eventType.Label,
                    EmailEnabled = existing?.EmailEnabled ?? true, // Default to enabled
                    WebhookEnabled = existing?.WebhookEnabled ?? false,
                    CreationTime = existing?.CreationTime ?? DateTime.UtcNow
                });
            }

            return new QueryResponseDto<List<NotificationPreferenceDto>>(result);
        }
    }
}

