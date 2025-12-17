using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Commands;

public class RetryWebhookDelivery
{
    public record Command : IRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid LogId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.LogId)
                .NotEmpty()
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        return await db.WebhookLogs.AnyAsync(
                            l => l.Id == id.Guid,
                            cancellationToken
                        );
                    }
                )
                .WithMessage("Webhook log not found.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ITicketPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            IBackgroundTaskQueue taskQueue,
            ITicketPermissionService permissionService
        )
        {
            _db = db;
            _taskQueue = taskQueue;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageSystemSettings();

            var log = await _db
                .WebhookLogs.Include(l => l.Webhook)
                .FirstOrDefaultAsync(l => l.Id == request.LogId.Guid, cancellationToken);

            if (log == null)
            {
                throw new Common.Exceptions.NotFoundException("WebhookLog", request.LogId);
            }

            if (log.Webhook == null)
            {
                throw new Common.Exceptions.NotFoundException("Webhook", log.WebhookId);
            }

            // Create a new delivery job with the same payload
            var args = new WebhookDeliveryArgs
            {
                WebhookId = log.WebhookId,
                TicketId = log.TicketId,
                TriggerType = log.TriggerType,
                PayloadJson = log.PayloadJson,
                Url = log.Webhook.Url,
                IsTest = false,
            };

            var jobId = await _taskQueue.EnqueueAsync<WebhookDeliveryJob>(args, cancellationToken);

            return new CommandResponseDto<ShortGuid>(jobId);
        }
    }
}
