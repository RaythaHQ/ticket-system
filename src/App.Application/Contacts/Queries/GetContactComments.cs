using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Queries;

public class GetContactComments
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<ContactCommentDto>>>
    {
        public long ContactId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<ContactCommentDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<ContactCommentDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var comments = await _db.ContactComments
                .AsNoTracking()
                .Include(c => c.AuthorStaff)
                .Where(c => c.ContactId == request.ContactId)
                .OrderByDescending(c => c.CreationTime)
                .ToListAsync(cancellationToken);

            var dtos = comments.Select(ContactCommentDto.MapFrom);

            return new QueryResponseDto<IEnumerable<ContactCommentDto>>(dtos);
        }
    }
}

