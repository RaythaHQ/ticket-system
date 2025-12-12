using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
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
        /// Optional filter by status.
        /// </summary>
        public string? Status { get; init; }

        /// <summary>
        /// Optional filter by priority.
        /// </summary>
        public string? Priority { get; init; }

        /// <summary>
        /// Optional filter by assignee ID.
        /// </summary>
        public Guid? AssigneeId { get; init; }

        /// <summary>
        /// Optional filter by team ID.
        /// </summary>
        public Guid? TeamId { get; init; }

        /// <summary>
        /// Optional filter by contact ID.
        /// </summary>
        public long? ContactId { get; init; }

        /// <summary>
        /// When true, show only unassigned tickets.
        /// </summary>
        public bool? Unassigned { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<TicketListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
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
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Status))
                query = query.Where(t => t.Status == request.Status);

            if (!string.IsNullOrEmpty(request.Priority))
                query = query.Where(t => t.Priority == request.Priority);

            if (request.AssigneeId.HasValue)
                query = query.Where(t => t.AssigneeId == request.AssigneeId.Value);

            if (request.TeamId.HasValue)
                query = query.Where(t => t.OwningTeamId == request.TeamId.Value);

            if (request.ContactId.HasValue)
                query = query.Where(t => t.ContactId == request.ContactId.Value);

            if (request.Unassigned == true)
                query = query.Where(t => t.AssigneeId == null);

            // Apply search
            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchQuery)
                    || (t.Description != null && t.Description.ToLower().Contains(searchQuery))
                    || t.Id.ToString().Contains(searchQuery)
                    || (t.Contact != null && t.Contact.Name.ToLower().Contains(searchQuery))
                );
            }

            var total = await query.CountAsync(cancellationToken);
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

