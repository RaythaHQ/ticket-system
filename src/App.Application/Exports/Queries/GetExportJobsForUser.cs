using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Exports.Queries;

public class GetExportJobsForUser
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<ExportJobDto>>>
    {
        public ShortGuid? UserId { get; init; }
        public override string OrderBy { get; init; } = $"RequestedAt {SortOrder.DESCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ExportJobDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<ExportJobDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var userId = request.UserId?.Guid ?? _currentUser.UserId?.Guid;
            
            if (!userId.HasValue)
                return new QueryResponseDto<ListResultDto<ExportJobDto>>(new ListResultDto<ExportJobDto>(
                    new List<ExportJobDto>(), 0));

            var query = _db.ExportJobs
                .Include(e => e.Requester)
                .AsNoTracking()
                .Where(e => e.RequesterUserId == userId.Value && !e.IsCleanedUp);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .ApplyPaginationInput(request)
                .Select(ExportJobDto.GetProjection())
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<ExportJobDto>>(
                new ListResultDto<ExportJobDto>(items, total));
        }
    }
}

