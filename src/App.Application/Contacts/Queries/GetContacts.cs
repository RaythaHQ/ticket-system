using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Application.Contacts.Utils;
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
                .Include(c => c.Comments)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                var searchDigits = PhoneNumberNormalizer.ExtractDigits(request.Search);

                // Check if search term looks like a phone number (has at least 4 digits)
                var isPhoneSearch = !string.IsNullOrEmpty(searchDigits) && searchDigits.Length >= 4;

                if (isPhoneSearch)
                {
                    // For phone number searches, we need to load contacts and check phone numbers in memory
                    // since phone numbers are stored as JSON and need flexible matching
                    var allContacts = await _db.Contacts
                        .AsNoTracking()
                        .Where(c => c.PhoneNumbersJson != null)
                        .ToListAsync(cancellationToken);

                    var matchingContactIds = allContacts
                        .Where(c =>
                            c.PhoneNumbers.Any(p =>
                                PhoneNumberNormalizer.Matches(p, request.Search)
                            )
                        )
                        .Select(c => c.Id)
                        .ToList();

                    // Also check standard fields for the search term
                    var standardMatches = await _db.Contacts
                        .AsNoTracking()
                        .Where(c =>
                            c.Name.ToLower().Contains(searchQuery)
                            || (c.Email != null && c.Email.ToLower().Contains(searchQuery))
                            || (c.OrganizationAccount != null && c.OrganizationAccount.ToLower().Contains(searchQuery))
                            || c.Id.ToString().Contains(searchQuery)
                        )
                        .Select(c => c.Id)
                        .ToListAsync(cancellationToken);

                    // Combine phone matches with standard matches
                    var allMatchingIds = matchingContactIds.Union(standardMatches).ToList();

                    if (allMatchingIds.Any())
                    {
                        query = query.Where(c => allMatchingIds.Contains(c.Id));
                    }
                    else
                    {
                        // No matches, return empty result
                        query = query.Where(c => false);
                    }
                }
                else
                {
                    // Standard text search (not a phone number)
                    query = query.Where(c =>
                        c.Name.ToLower().Contains(searchQuery)
                        || (c.Email != null && c.Email.ToLower().Contains(searchQuery))
                        || (c.OrganizationAccount != null && c.OrganizationAccount.ToLower().Contains(searchQuery))
                        || c.Id.ToString().Contains(searchQuery)
                    );
                }
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

