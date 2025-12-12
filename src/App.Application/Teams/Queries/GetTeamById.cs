using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Teams.Queries;

public class GetTeamById
{
    public record Query : IRequest<IQueryResponseDto<TeamDto>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<TeamDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<TeamDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var team = await _db.Teams
                .AsNoTracking()
                .Include(t => t.Memberships)
                .FirstOrDefaultAsync(t => t.Id == request.Id.Guid, cancellationToken);

            if (team == null)
                throw new NotFoundException("Team", request.Id);

            return new QueryResponseDto<TeamDto>(TeamDto.MapFrom(team));
        }
    }
}

