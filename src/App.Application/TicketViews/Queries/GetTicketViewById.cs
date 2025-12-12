using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketViews.Queries;

public class GetTicketViewById
{
    public record Query : IRequest<IQueryResponseDto<TicketViewDto>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<TicketViewDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<TicketViewDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var view = await _db.TicketViews
                .AsNoTracking()
                .Include(v => v.OwnerStaff)
                .FirstOrDefaultAsync(v => v.Id == request.Id.Guid, cancellationToken);

            if (view == null)
                throw new NotFoundException("TicketView", request.Id);

            return new QueryResponseDto<TicketViewDto>(TicketViewDto.MapFrom(view));
        }
    }
}

