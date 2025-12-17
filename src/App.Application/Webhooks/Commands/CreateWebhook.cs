using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Webhooks.Services;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;

namespace App.Application.Webhooks.Commands;

public class CreateWebhook
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Name { get; init; } = null!;
        public string Url { get; init; } = null!;
        public string TriggerType { get; init; } = null!;
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IUrlValidationService urlValidator)
        {
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
            // Webhooks require ManageSystemSettings permission
            _permissionService.RequireCanManageSystemSettings();

            var webhook = new Webhook
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Url = request.Url,
                TriggerType = request.TriggerType.ToLower(),
                Description = request.Description,
                IsActive = request.IsActive,
            };

            _db.Webhooks.Add(webhook);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(webhook.Id);
        }
    }
}
