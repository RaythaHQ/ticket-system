using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Queries;

public class GetContactChangeLog
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<ContactChangeLogEntryDto>>>
    {
        public long ContactId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<ContactChangeLogEntryDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<ContactChangeLogEntryDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entries = await _db.ContactChangeLogEntries
                .AsNoTracking()
                .Include(e => e.ActorStaff)
                .Where(e => e.ContactId == request.ContactId)
                .OrderByDescending(e => e.CreationTime)
                .ToListAsync(cancellationToken);

            var dtos = entries.Select(ContactChangeLogEntryDto.MapFrom);

            return new QueryResponseDto<IEnumerable<ContactChangeLogEntryDto>>(dtos);
        }
    }
}

