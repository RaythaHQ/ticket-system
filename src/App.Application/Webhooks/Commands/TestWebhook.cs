using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Webhooks.Services;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Commands;

public class TestWebhook
{
    public record Command : IRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        return await db.Webhooks.AnyAsync(w => w.Id == id.Guid, cancellationToken);
                    }
                )
                .WithMessage("Webhook not found.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IWebhookPayloadBuilder _payloadBuilder;
        private readonly ITicketPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            IBackgroundTaskQueue taskQueue,
            IWebhookPayloadBuilder payloadBuilder,
            ITicketPermissionService permissionService
        )
        {
            _db = db;
            _taskQueue = taskQueue;
            _payloadBuilder = payloadBuilder;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageSystemSettings();

            var webhook = await _db
                .Webhooks.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == request.Id.Guid, cancellationToken);

            if (webhook == null)
            {
                throw new Common.Exceptions.NotFoundException("Webhook", request.Id);
            }

            // Build a test payload
            var payload = _payloadBuilder.BuildTestPayload(webhook.TriggerType);
            var payloadJson = JsonSerializer.Serialize(payload);

            var args = new WebhookDeliveryArgs
            {
                WebhookId = webhook.Id,
                TicketId = null, // Test payload, no real ticket
                TriggerType = webhook.TriggerType,
                PayloadJson = payloadJson,
                Url = webhook.Url,
                IsTest = true,
            };

            var jobId = await _taskQueue.EnqueueAsync<WebhookDeliveryJob>(args, cancellationToken);

            return new CommandResponseDto<ShortGuid>(jobId);
        }
    }
}
