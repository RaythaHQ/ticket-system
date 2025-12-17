using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Webhooks.Services;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Commands;

public class UpdateWebhook
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid Id { get; init; }
        public string Name { get; init; } = null!;
        public string Url { get; init; } = null!;
        public string TriggerType { get; init; } = null!;
        public string? Description { get; init; }
        public bool IsActive { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db, IUrlValidationService urlValidator)
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

            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

            RuleFor(x => x.Url)
                .NotEmpty()
                .MaximumLength(2000)
                .MustAsync(
                    async (url, cancellationToken) =>
                    {
                        var result = await urlValidator.ValidateAsync(url, cancellationToken);
                        return result.IsValid;
                    }
                )
                .WithMessage(
                    "Invalid webhook URL. URLs must be publicly accessible and cannot target internal addresses."
                );

            RuleFor(x => x.TriggerType)
                .NotEmpty()
                .Must(triggerType =>
                {
                    try
                    {
                        WebhookTriggerType.From(triggerType);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage(
                    $"Invalid trigger type. Valid types are: {string.Join(", ", WebhookTriggerType.SupportedTypes.Select(t => t.DeveloperName))}"
                );

            RuleFor(x => x.Description).MaximumLength(1000);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageSystemSettings();

            var webhook = await _db.Webhooks.FirstOrDefaultAsync(
                w => w.Id == request.Id.Guid,
                cancellationToken
            );

            if (webhook == null)
            {
                throw new Common.Exceptions.NotFoundException("Webhook", request.Id);
            }

            webhook.Name = request.Name;
            webhook.Url = request.Url;
            webhook.TriggerType = request.TriggerType.ToLower();
            webhook.Description = request.Description;
            webhook.IsActive = request.IsActive;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(webhook.Id);
        }
    }
}
