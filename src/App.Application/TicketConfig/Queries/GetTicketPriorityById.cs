using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Queries;

public class GetTicketPriorityById
{
    public record Query : IRequest<IQueryResponseDto<TicketPriorityConfigDto?>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<TicketPriorityConfigDto?>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<TicketPriorityConfigDto?>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var priority = await _db.TicketPriorityConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            return new QueryResponseDto<TicketPriorityConfigDto?>(
                priority != null ? TicketPriorityConfigDto.MapFrom(priority) : null
            );
        }
    }
}

