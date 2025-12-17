using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Queries;

public class GetWebhooksForDropdown
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<WebhookSelectOptionDto>>> { }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<IEnumerable<WebhookSelectOptionDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<WebhookSelectOptionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageSystemSettings();

            var webhooks = await _db
                .Webhooks.AsNoTracking()
                .OrderBy(w => w.Name)
                .Select(w => new WebhookSelectOptionDto { Value = w.Id.ToString(), Label = w.Name })
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<IEnumerable<WebhookSelectOptionDto>>(webhooks);
        }
    }
}

public record WebhookSelectOptionDto
{
    public string Value { get; init; } = null!;
    public string Label { get; init; } = null!;
}
