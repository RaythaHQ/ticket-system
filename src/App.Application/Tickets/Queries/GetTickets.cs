using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Application.TicketViews;
using App.Application.TicketViews.Services;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetTickets
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<TicketListItemDto>>>
    {
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.DESCENDING}";

        /// <summary>
        /// Optional filter by status (exact status developer name).
        /// </summary>
        public string? Status { get; init; }

        /// <summary>
        /// Optional filter by status type ("open" or "closed").
        /// When set, filters tickets by all statuses of this type.
        /// </summary>
        public string? StatusType { get; init; }

        /// <summary>
        /// Optional filter by priority.
        /// </summary>
        public string? Priority { get; init; }

        /// <summary>
        /// Optional filter by assignee ID.
        /// </summary>
        public ShortGuid? AssigneeId { get; init; }

        /// <summary>
        /// Optional filter by team ID.
        /// </summary>
        public ShortGuid? TeamId { get; init; }

        /// <summary>
        /// Optional filter by contact ID.
        /// </summary>
        public long? ContactId { get; init; }

        /// <summary>
        /// Optional filter by created by staff ID.
        /// </summary>
        public ShortGuid? CreatedByStaffId { get; init; }

        /// <summary>
        /// When true, show only unassigned tickets.
        /// </summary>
        public bool? Unassigned { get; init; }

        /// <summary>
        /// When true, show only tickets assigned to teams the current user belongs to.
        /// </summary>
        public bool? TeamTickets { get; init; }

        /// <summary>
        /// Optional view ID to apply saved view filters.
        /// </summary>
        public ShortGuid? ViewId { get; init; }

        /// <summary>
        /// Visible columns for column-limited search. If not provided with view, searches all fields.
        /// </summary>
        public List<string>? VisibleColumns { get; init; }

        /// <summary>
        /// View conditions to apply (alternative to ViewId for built-in views).
        /// </summary>
        public ViewConditions? ViewConditions { get; init; }

        /// <summary>
        /// When "view", use the view's default sort order instead of query string OrderBy.
        /// </summary>
        public string? SortBy { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<TicketListItemDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<TicketListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.Tickets
                .AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.Contact)
                .Include(t => t.Comments)
                .AsQueryable();

            var filterBuilder = new ViewFilterBuilder();
            List<string> visibleColumns = request.VisibleColumns ?? new List<string>();
            List<Domain.Entities.ViewSortLevel>? viewSortLevels = null;
            bool useViewSort = request.SortBy?.ToLower() == "view";

            // Apply view filters if ViewId is provided
            if (request.ViewId.HasValue)
            {
                var view = await _db.TicketViews
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == request.ViewId.Value.Guid, cancellationToken);

                if (view != null)
                {
                    visibleColumns = view.VisibleColumns;
                    viewSortLevels = view.SortLevels;
                    
                    if (!string.IsNullOrEmpty(view.ConditionsJson))
                    {
                        try
                        {
                            var conditions = System.Text.Json.JsonSerializer.Deserialize<ViewConditions>(view.ConditionsJson);
                            if (conditions != null)
                            {
                                query = filterBuilder.ApplyFilters(query, conditions);
                            }
                        }
                        catch { }
                    }
                }
            }
            else if (request.ViewConditions != null)
            {
                // Apply inline view conditions
                query = filterBuilder.ApplyFilters(query, request.ViewConditions);
            }

            // Apply additional direct filters (these override view filters)
            if (!string.IsNullOrEmpty(request.Status))
                query = query.Where(t => t.Status == request.Status);

            // Apply status type filter (filters by all statuses of that type)
            if (!string.IsNullOrEmpty(request.StatusType))
            {
                var statusTypeValue = request.StatusType.ToLower();
                var statusesOfType = await _db.TicketStatusConfigs
                    .AsNoTracking()
                    .Where(s => s.StatusType == statusTypeValue)
                    .Select(s => s.DeveloperName)
                    .ToListAsync(cancellationToken);
                
                query = query.Where(t => statusesOfType.Contains(t.Status));
            }

            if (!string.IsNullOrEmpty(request.Priority))
                query = query.Where(t => t.Priority == request.Priority);

            if (request.AssigneeId.HasValue)
                query = query.Where(t => t.AssigneeId == request.AssigneeId.Value.Guid);

            if (request.TeamId.HasValue)
                query = query.Where(t => t.OwningTeamId == request.TeamId.Value.Guid);

            if (request.ContactId.HasValue)
                query = query.Where(t => t.ContactId == request.ContactId.Value);

            if (request.CreatedByStaffId.HasValue)
                query = query.Where(t => t.CreatedByStaffId == request.CreatedByStaffId.Value.Guid);

            if (request.Unassigned == true)
            {
                // Show tickets with no individual assigned
                // If TeamId is also set, the TeamId filter already restricts to that team
                // This just ensures no individual assignee
                query = query.Where(t => t.AssigneeId == null);
            }

            // Filter by user's teams if TeamTickets is requested
            // Shows all tickets where the ticket's OwningTeamId matches any team the user is a member of
            if (request.TeamTickets == true && _currentUser.UserId.HasValue)
            {
                var userTeamIds = await _db.TeamMemberships
                    .AsNoTracking()
                    .Where(m => m.StaffAdminId == _currentUser.UserId.Value.Guid)
                    .Select(m => m.TeamId)
                    .ToListAsync(cancellationToken);

                if (userTeamIds.Any())
                {
                    // Filter tickets where OwningTeamId is in the user's team list
                    query = query.Where(t => t.OwningTeamId.HasValue && userTeamIds.Contains(t.OwningTeamId.Value));
                }
                else
                {
                    // User is not in any team, return no results
                    query = query.Where(t => false);
                }
            }

            // Apply search - respect visible columns if available
            if (!string.IsNullOrEmpty(request.Search))
            {
                if (visibleColumns.Any())
                {
                    query = filterBuilder.ApplyColumnSearch(query, request.Search, visibleColumns);
                }
                else
                {
                    // Search all searchable fields
                    var searchQuery = request.Search.ToLower();
                    query = query.Where(t =>
                        t.Title.ToLower().Contains(searchQuery)
                        || (t.Description != null && t.Description.ToLower().Contains(searchQuery))
                        || t.Id.ToString().Contains(searchQuery)
                        || (t.Contact != null && (t.Contact.FirstName.ToLower().Contains(searchQuery)
                            || (t.Contact.LastName != null && t.Contact.LastName.ToLower().Contains(searchQuery))))
                    );
                }
            }

            var total = await query.CountAsync(cancellationToken);

            // Apply sorting: use view's multi-level sort if sortBy=view, otherwise use OrderBy
            if (useViewSort && viewSortLevels?.Any() == true)
            {
                query = filterBuilder.ApplySorting(query, viewSortLevels);
                var items = query
                    .ApplyPaginationInputWithoutSorting(request)
                    .Select(t => TicketListItemDto.MapFrom(t))
                    .ToArray();

                return new QueryResponseDto<ListResultDto<TicketListItemDto>>(
                    new ListResultDto<TicketListItemDto>(items, total)
                );
            }
            else
            {
                var items = query
                    .ApplyPaginationInput(request)
                    .Select(t => TicketListItemDto.MapFrom(t))
                    .ToArray();

                return new QueryResponseDto<ListResultDto<TicketListItemDto>>(
                    new ListResultDto<TicketListItemDto>(items, total)
                );
            }
        }
    }
}
