using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Queries;

public class GetWebhookLogById
{
    public record Query : IRequest<IQueryResponseDto<WebhookLogDto?>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WebhookLogDto?>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<WebhookLogDto?>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageSystemSettings();

            var log = await _db
                .WebhookLogs.AsNoTracking()
                .Include(l => l.Webhook)
                .Where(l => l.Id == request.Id.Guid)
                .Select(WebhookLogDto.GetProjection())
                .FirstOrDefaultAsync(cancellationToken);

            return new QueryResponseDto<WebhookLogDto?>(log);
        }
    }
}
