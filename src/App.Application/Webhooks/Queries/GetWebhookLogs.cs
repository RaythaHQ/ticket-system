using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Queries;

public class GetWebhookLogs
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WebhookLogDto>>>
    {
        public ShortGuid? WebhookId { get; init; }
        public string? TriggerType { get; init; }
        public bool? Success { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebhookLogDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WebhookLogDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageSystemSettings();

            var query = _db.WebhookLogs.AsNoTracking().Include(l => l.Webhook).AsQueryable();

            if (request.WebhookId.HasValue)
            {
                query = query.Where(l => l.WebhookId == request.WebhookId.Value.Guid);
            }

            if (!string.IsNullOrWhiteSpace(request.TriggerType))
            {
                query = query.Where(l => l.TriggerType == request.TriggerType.ToLower());
            }

            if (request.Success.HasValue)
            {
                query = query.Where(l => l.Success == request.Success.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= request.ToDate.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .ApplyPaginationInput(request)
                .Select(WebhookLogDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<WebhookLogDto>>(
                new ListResultDto<WebhookLogDto>(items, total)
            );
        }
    }
}
