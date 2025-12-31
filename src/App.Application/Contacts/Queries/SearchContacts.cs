using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Contacts.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Queries;

public class SearchContacts
{
    public record Query : LoggableQuery<IQueryResponseDto<IEnumerable<ContactListItemDto>>>
    {
        /// <summary>
        /// Search term - can be name, email, or phone number in any format.
        /// </summary>
        public string SearchTerm { get; init; } = null!;

        /// <summary>
        /// Maximum results to return.
        /// </summary>
        public int MaxResults { get; init; } = 20;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<ContactListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<ContactListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return new QueryResponseDto<IEnumerable<ContactListItemDto>>(Enumerable.Empty<ContactListItemDto>());
            }

            var searchTerm = request.SearchTerm.Trim().ToLower();
            var searchDigits = PhoneNumberNormalizer.ExtractDigits(request.SearchTerm);

            // Get all contacts for phone number matching (we need to do this in memory for flexible phone matching)
            var contacts = await _db.Contacts
                .AsNoTracking()
                .Include(c => c.Tickets)
                .ToListAsync(cancellationToken);

            var results = contacts.Where(c =>
                // Match by first name
                c.FirstName.ToLower().Contains(searchTerm)
                // Match by last name
                || (c.LastName != null && c.LastName.ToLower().Contains(searchTerm))
                // Match by full name
                || c.FullName.ToLower().Contains(searchTerm)
                // Match by email
                || (c.Email != null && c.Email.ToLower().Contains(searchTerm))
                // Match by organization
                || (c.OrganizationAccount != null && c.OrganizationAccount.ToLower().Contains(searchTerm))
                // Match by ID
                || c.Id.ToString() == searchTerm
                // Match by phone number (flexible matching)
                || (!string.IsNullOrEmpty(searchDigits) && searchDigits.Length >= 4 &&
                    c.PhoneNumbers.Any(p => PhoneNumberNormalizer.Matches(p, request.SearchTerm)))
            )
            .Take(request.MaxResults)
            .Select(ContactListItemDto.MapFrom)
            .ToList();

            return new QueryResponseDto<IEnumerable<ContactListItemDto>>(results);
        }
    }
}

