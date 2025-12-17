using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Queries;

public class GetWebhooks
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WebhookDto>>>
    {
        public string? Search { get; init; }
        public string? TriggerType { get; init; }
        public bool? IsActive { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebhookDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WebhookDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageSystemSettings();

            var query = _db.Webhooks.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(w =>
                    w.Name.ToLower().Contains(search)
                    || (w.Description != null && w.Description.ToLower().Contains(search))
                    || w.Url.ToLower().Contains(search)
                );
            }

            if (!string.IsNullOrWhiteSpace(request.TriggerType))
            {
                query = query.Where(w => w.TriggerType == request.TriggerType.ToLower());
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(w => w.IsActive == request.IsActive.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(w => w.Name)
                .ApplyPaginationInput(request)
                .Select(WebhookDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<WebhookDto>>(
                new ListResultDto<WebhookDto>(items, total)
            );
        }
    }
}
