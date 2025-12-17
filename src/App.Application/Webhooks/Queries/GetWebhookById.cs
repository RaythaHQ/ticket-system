using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Queries;

public class GetWebhookById
{
    public record Query : IRequest<IQueryResponseDto<WebhookDto?>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WebhookDto?>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<WebhookDto?>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageSystemSettings();

            var webhook = await _db
                .Webhooks.AsNoTracking()
                .Where(w => w.Id == request.Id.Guid)
                .Select(WebhookDto.GetProjection())
                .FirstOrDefaultAsync(cancellationToken);

            return new QueryResponseDto<WebhookDto?>(webhook);
        }
    }
}
