using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Queries;

public class GetContactById
{
    public record Query : LoggableQuery<IQueryResponseDto<ContactDto>>
    {
        public long Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ContactDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ContactDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var contact = await _db.Contacts
                .AsNoTracking()
                .Include(c => c.Tickets)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (contact == null)
                throw new NotFoundException("Contact", request.Id);

            return new QueryResponseDto<ContactDto>(ContactDto.MapFrom(contact));
        }
    }
}

