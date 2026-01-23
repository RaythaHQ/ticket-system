using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Notifications.Queries;

public class GetNotifications
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<NotificationListItemDto>>>
    {
        public override int PageSize { get; init; } = 25;
        public override string OrderBy { get; init; } = $"CreatedAt {SortOrder.DESCENDING}";

        /// <summary>
        /// Filter by read status: "all", "unread", "read".
        /// Default: "unread".
        /// </summary>
        public string FilterStatus { get; init; } = "unread";

        /// <summary>
        /// Filter by notification type (NotificationEventType.DeveloperName).
        /// If null/empty, returns all types.
        /// </summary>
        public string? FilterType { get; init; }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<NotificationListItemDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ICurrentOrganization _currentOrganization;

        public Handler(IAppDbContext db, ICurrentUser currentUser, ICurrentOrganization currentOrganization)
        {
            _db = db;
            _currentUser = currentUser;
            _currentOrganization = currentOrganization;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<NotificationListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUser.UserId?.Guid;
            if (!userId.HasValue)
            {
                return new QueryResponseDto<ListResultDto<NotificationListItemDto>>(
                    new ListResultDto<NotificationListItemDto>(
                        Enumerable.Empty<NotificationListItemDto>(),
                        0
                    )
                );
            }

            var query = _db.Notifications.AsNoTracking()
                .Where(n => n.RecipientUserId == userId.Value);

            // Apply status filter
            switch (request.FilterStatus.ToLower())
            {
                case "unread":
                    query = query.Where(n => !n.IsRead);
                    break;
                case "read":
                    query = query.Where(n => n.IsRead);
                    break;
                case "all":
                default:
                    // No filter
                    break;
            }

            // Apply type filter
            if (!string.IsNullOrEmpty(request.FilterType))
            {
                query = query.Where(n => n.EventType == request.FilterType.ToLower());
            }

            var total = await query.CountAsync(cancellationToken);

            // Apply sorting and pagination
            var items = await query
                .ApplyPaginationInput(request)
                .Select(n => new NotificationListItemDto
                {
                    Id = n.Id,
                    EventType = n.EventType,
                    EventTypeLabel = GetEventTypeLabel(n.EventType),
                    Title = n.Title,
                    Message = n.Message,
                    Url = n.Url,
                    TicketId = n.TicketId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    CreatedAtRelative = string.Empty // Will be set below
                })
                .ToListAsync(cancellationToken);

            // Set relative time for each item
            foreach (var item in items)
            {
                // Update relative time
                var dto = item with
                {
                    CreatedAtRelative = GetRelativeTime(item.CreatedAt)
                };
                items[items.IndexOf(item)] = dto;
            }

            return new QueryResponseDto<ListResultDto<NotificationListItemDto>>(
                new ListResultDto<NotificationListItemDto>(items, total)
            );
        }

        private static string GetEventTypeLabel(string eventType)
        {
            try
            {
                return NotificationEventType.From(eventType).Label;
            }
            catch
            {
                return eventType;
            }
        }

        private static string GetRelativeTime(DateTime utcDateTime)
        {
            var now = DateTime.UtcNow;
            var diff = now - utcDateTime;

            if (diff.TotalSeconds < 60)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} minute{((int)diff.TotalMinutes == 1 ? "" : "s")} ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hour{((int)diff.TotalHours == 1 ? "" : "s")} ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} day{((int)diff.TotalDays == 1 ? "" : "s")} ago";
            if (diff.TotalDays < 30)
            {
                var weeks = (int)(diff.TotalDays / 7);
                return $"{weeks} week{(weeks == 1 ? "" : "s")} ago";
            }
            if (diff.TotalDays < 365)
            {
                var months = (int)(diff.TotalDays / 30);
                return $"{months} month{(months == 1 ? "" : "s")} ago";
            }

            var years = (int)(diff.TotalDays / 365);
            return $"{years} year{(years == 1 ? "" : "s")} ago";
        }
    }
}

