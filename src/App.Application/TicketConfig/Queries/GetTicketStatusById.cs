using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Queries;

public class GetTicketStatusById
{
    public record Query : IRequest<IQueryResponseDto<TicketStatusConfigDto?>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<TicketStatusConfigDto?>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<TicketStatusConfigDto?>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var status = await _db.TicketStatusConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.Id.Guid, cancellationToken);

            return new QueryResponseDto<TicketStatusConfigDto?>(
                status != null ? TicketStatusConfigDto.MapFrom(status) : null
            );
        }
    }
}

