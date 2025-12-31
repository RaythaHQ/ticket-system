using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Contacts.Utils;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

/// <summary>
/// Quick search for tickets with OR logic across multiple search criteria.
/// Used for dashboard quick lookup functionality.
/// </summary>
public class QuickSearchTickets
{
    public record Query : LoggableQuery<IQueryResponseDto<IEnumerable<TicketListItemDto>>>
    {
        /// <summary>
        /// Optional filter by contact ID.
        /// </summary>
        public long? ContactId { get; init; }

        /// <summary>
        /// Optional filter by contact phone number (flexible matching).
        /// </summary>
        public string? ContactPhone { get; init; }

        /// <summary>
        /// Optional filter by ticket title (contains search).
        /// </summary>
        public string? TicketTitle { get; init; }

        /// <summary>
        /// Optional filter by assignee ID.
        /// </summary>
        public ShortGuid? AssigneeId { get; init; }

        /// <summary>
        /// Optional filter by created by user ID.
        /// </summary>
        public ShortGuid? CreatedById { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<TicketListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<TicketListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var baseQuery = _db.Tickets
                .AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.Contact)
                .Include(t => t.CreatedByStaff)
                .AsQueryable();

            var results = new List<Domain.Entities.Ticket>();

            // Contact ID
            if (request.ContactId.HasValue)
            {
                var contactTickets = await baseQuery
                    .Where(t => t.ContactId == request.ContactId.Value)
                    .ToListAsync(cancellationToken);
                results.AddRange(contactTickets);
            }

            // Contact Phone
            if (!string.IsNullOrWhiteSpace(request.ContactPhone))
            {
                var searchDigits = PhoneNumberNormalizer.ExtractDigits(request.ContactPhone);
                if (!string.IsNullOrEmpty(searchDigits) && searchDigits.Length >= 4)
                {
                    // Load contacts to check phone numbers
                    var contactsWithMatchingPhone = await _db.Contacts
                        .AsNoTracking()
                        .Where(c => c.PhoneNumbersJson != null)
                        .ToListAsync(cancellationToken);

                    var matchingContactIds = contactsWithMatchingPhone
                        .Where(c =>
                            c.PhoneNumbers.Any(p =>
                                PhoneNumberNormalizer.Matches(p, request.ContactPhone)
                            )
                        )
                        .Select(c => c.Id)
                        .ToList();

                    if (matchingContactIds.Any())
                    {
                        var phoneTickets = await baseQuery
                            .Where(t =>
                                t.ContactId.HasValue && matchingContactIds.Contains(t.ContactId.Value)
                            )
                            .ToListAsync(cancellationToken);
                        results.AddRange(phoneTickets);
                    }
                }
            }

            // Ticket Title
            if (!string.IsNullOrWhiteSpace(request.TicketTitle))
            {
                var titleLower = request.TicketTitle.ToLower();
                var titleTickets = await baseQuery
                    .Where(t => t.Title.ToLower().Contains(titleLower))
                    .ToListAsync(cancellationToken);
                results.AddRange(titleTickets);
            }

            // Assignee
            if (request.AssigneeId.HasValue)
            {
                var assigneeTickets = await baseQuery
                    .Where(t => t.AssigneeId == request.AssigneeId.Value.Guid)
                    .ToListAsync(cancellationToken);
                results.AddRange(assigneeTickets);
            }

            // Created By
            if (request.CreatedById.HasValue)
            {
                var createdTickets = await baseQuery
                    .Where(t => t.CreatedByStaffId == request.CreatedById.Value.Guid)
                    .ToListAsync(cancellationToken);
                results.AddRange(createdTickets);
            }

            // Remove duplicates and order
            var finalTickets = results
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .OrderByDescending(t => t.CreationTime)
                .Take(100)
                .ToList();

            var dtos = finalTickets.Select(TicketListItemDto.MapFrom);

            return new QueryResponseDto<IEnumerable<TicketListItemDto>>(dtos);
        }
    }
}

