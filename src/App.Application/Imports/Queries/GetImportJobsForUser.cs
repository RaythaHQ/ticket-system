using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Imports.Queries;

public class GetImportJobsForUser
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<ImportJobDto>>>
    {
        public int PageSize { get; init; } = 20;
        public int PageNumber { get; init; } = 1;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<ImportJobDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<ImportJobDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (!_currentUser.UserId.HasValue)
            {
                return new QueryResponseDto<IEnumerable<ImportJobDto>>(
                    Enumerable.Empty<ImportJobDto>()
                );
            }

            var userId = _currentUser.UserId.Value.Guid;

            var jobs = await _db
                .ImportJobs.Include(e => e.Requester)
                .Where(e => e.RequesterUserId == userId)
                .OrderByDescending(e => e.RequestedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var dtos = jobs.Select(ImportJobDto.GetProjection);
            return new QueryResponseDto<IEnumerable<ImportJobDto>>(dtos);
        }
    }
}
