using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Contacts.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Queries;

/// <summary>
/// Quick search for contacts with OR logic across multiple search criteria.
/// Used for dashboard quick lookup functionality.
/// </summary>
public class QuickSearchContacts
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<ContactListItemDto>>>
    {
        /// <summary>
        /// Optional filter by first name (contains search).
        /// </summary>
        public string? FirstName { get; init; }

        /// <summary>
        /// Optional filter by last name (contains search).
        /// </summary>
        public string? LastName { get; init; }

        /// <summary>
        /// Optional filter by phone number (flexible matching).
        /// </summary>
        public string? Phone { get; init; }
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
            var baseQuery = _db.Contacts
                .AsNoTracking()
                .Include(c => c.Tickets)
                .Include(c => c.Comments)
                .AsQueryable();

            var contactResults = new List<Domain.Entities.Contact>();

            // First Name
            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                var firstNameLower = request.FirstName.ToLower();
                var firstNameContacts = await baseQuery
                    .Where(c => c.Name.ToLower().Contains(firstNameLower))
                    .ToListAsync(cancellationToken);
                contactResults.AddRange(firstNameContacts);
            }

            // Last Name
            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                var lastNameLower = request.LastName.ToLower();
                var lastNameContacts = await baseQuery
                    .Where(c => c.Name.ToLower().Contains(lastNameLower))
                    .ToListAsync(cancellationToken);
                contactResults.AddRange(lastNameContacts);
            }

            // Phone
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var searchDigits = PhoneNumberNormalizer.ExtractDigits(request.Phone);
                if (!string.IsNullOrEmpty(searchDigits) && searchDigits.Length >= 4)
                {
                    // Load all contacts to check phone numbers in memory
                    var allContacts = await _db.Contacts
                        .AsNoTracking()
                        .Where(c => c.PhoneNumbersJson != null)
                        .ToListAsync(cancellationToken);

                    var matchingContactIds = allContacts
                        .Where(c =>
                            c.PhoneNumbers.Any(p =>
                                PhoneNumberNormalizer.Matches(p, request.Phone)
                            )
                        )
                        .Select(c => c.Id)
                        .ToList();

                    if (matchingContactIds.Any())
                    {
                        var phoneContacts = await baseQuery
                            .Where(c => matchingContactIds.Contains(c.Id))
                            .ToListAsync(cancellationToken);
                        contactResults.AddRange(phoneContacts);
                    }
                }
            }

            // Remove duplicates and order
            var finalContacts = contactResults
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderByDescending(c => c.CreationTime)
                .Take(100)
                .ToList();

            var dtos = finalContacts.Select(ContactListItemDto.MapFrom);

            return new QueryResponseDto<IEnumerable<ContactListItemDto>>(dtos);
        }
    }
}

