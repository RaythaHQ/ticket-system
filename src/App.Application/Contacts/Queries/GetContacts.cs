using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Queries;

public class GetContacts
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<ContactListItemDto>>>
    {
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.DESCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ContactListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<ContactListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.Contacts
                .AsNoTracking()
                .Include(c => c.Tickets)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(searchQuery)
                    || (c.Email != null && c.Email.ToLower().Contains(searchQuery))
                    || (c.OrganizationAccount != null && c.OrganizationAccount.ToLower().Contains(searchQuery))
                    || c.Id.ToString().Contains(searchQuery)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(c => ContactListItemDto.MapFrom(c))
                .ToArray();

            return new QueryResponseDto<ListResultDto<ContactListItemDto>>(
                new ListResultDto<ContactListItemDto>(items, total)
            );
        }
    }
}

