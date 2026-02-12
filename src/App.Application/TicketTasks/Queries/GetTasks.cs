using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketTasks.Queries;

public class GetTasks
{
    public enum TaskView
    {
        MyTasks,
        TeamTasks,
        Unassigned,
        CreatedByMe,
        Overdue,
        AllTasks,
    }

    public record Query : LoggableQuery<IQueryResponseDto<ListResultDto<TaskListItemDto>>>
    {
        public TaskView View { get; init; } = TaskView.MyTasks;
        public string? Search { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
        public string? Sort { get; init; }
        public string? SortDir { get; init; }

        /// <summary>
        /// When true, exclude closed tasks. When false, show all.
        /// Defaults to true (show open tasks only).
        /// </summary>
        public bool ExcludeClosed { get; init; } = true;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<TaskListItemDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<TaskListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var query = _db.TicketTasks
                .AsNoTracking()
                .Include(t => t.Ticket).ThenInclude(t => t.Assignee)
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.DependsOnTask)
                .Include(t => t.CreatedByStaff)
                .Include(t => t.ClosedByStaff)
                .AsQueryable();

            var userId = _currentUser.UserIdAsGuid;

            // Apply view filter
            switch (request.View)
            {
                case TaskView.MyTasks:
                    query = query.Where(t => t.AssigneeId == userId);
                    break;

                case TaskView.TeamTasks:
                    var teamIds = await _db.TeamMemberships
                        .AsNoTracking()
                        .Where(m => m.StaffAdminId == userId)
                        .Select(m => m.TeamId)
                        .ToListAsync(cancellationToken);
                    query = query.Where(t => t.OwningTeamId != null && teamIds.Contains(t.OwningTeamId.Value));
                    break;

                case TaskView.Unassigned:
                    query = query.Where(t => t.AssigneeId == null && t.OwningTeamId == null);
                    break;

                case TaskView.CreatedByMe:
                    query = query.Where(t => t.CreatedByStaffId == userId);
                    break;

                case TaskView.Overdue:
                    query = query.Where(t => t.Status == TicketTaskStatus.OPEN
                        && t.DueAt != null && t.DueAt < DateTime.UtcNow);
                    break;

                case TaskView.AllTasks:
                    // No view filter
                    break;
            }

            // Apply status filter (exclude closed unless overridden)
            if (request.ExcludeClosed && request.View != TaskView.Overdue)
            {
                query = query.Where(t => t.Status != TicketTaskStatus.CLOSED);
            }

            // Hide blocked tasks (tasks whose dependency is not yet closed)
            // Only show in AllTasks view or when explicitly searching
            if (request.View != TaskView.AllTasks && string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(t =>
                    t.DependsOnTaskId == null
                    || t.DependsOnTask!.Status == TicketTaskStatus.CLOSED);
            }

            // Search on task title, ticket title, and ticket ID
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower().Trim();
                // Try to parse as ticket ID number
                long? searchTicketId = null;
                if (long.TryParse(request.Search.TrimStart('#'), out var parsedId))
                {
                    searchTicketId = parsedId;
                }

                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchLower)
                    || t.Ticket.Title.ToLower().Contains(searchLower)
                    || (searchTicketId.HasValue && t.TicketId == searchTicketId.Value)
                );
            }

            // Sorting
            var sortDir = request.SortDir?.ToLower() == "desc" ? "desc" : "asc";
            query = (request.Sort?.ToLower(), sortDir) switch
            {
                ("title", "asc") => query.OrderBy(t => t.Title),
                ("title", "desc") => query.OrderByDescending(t => t.Title),
                ("dueat", "asc") => query.OrderBy(t => t.DueAt),
                ("dueat", "desc") => query.OrderByDescending(t => t.DueAt),
                ("newest", _) => query.OrderByDescending(t => t.CreationTime),
                ("oldest", _) => query.OrderBy(t => t.CreationTime),
                ("ticket", "asc") => query.OrderBy(t => t.TicketId),
                ("ticket", "desc") => query.OrderByDescending(t => t.TicketId),
                ("assignee", "asc") => query.OrderBy(t => t.Assignee != null ? t.Assignee.FirstName : "").ThenBy(t => t.CreationTime),
                ("assignee", "desc") => query.OrderByDescending(t => t.Assignee != null ? t.Assignee.FirstName : "").ThenBy(t => t.CreationTime),
                ("status", "asc") => query.OrderBy(t => t.Status),
                ("status", "desc") => query.OrderByDescending(t => t.Status),
                _ => request.View switch
                {
                    TaskView.Overdue => query.OrderBy(t => t.DueAt),
                    TaskView.AllTasks => query.OrderByDescending(t => t.CreationTime),
                    _ => query.OrderBy(t => t.DueAt ?? DateTime.MaxValue).ThenByDescending(t => t.CreationTime),
                }
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var tasks = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = tasks.Select(TaskListItemDto.MapFromWithTicket);

            return new QueryResponseDto<ListResultDto<TaskListItemDto>>(
                new ListResultDto<TaskListItemDto>(dtos, totalCount));
        }
    }
}
