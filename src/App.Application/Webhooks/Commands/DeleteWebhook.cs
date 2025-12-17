using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Webhooks.Commands;

public class DeleteWebhook
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
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

            _db.Webhooks.Remove(webhook);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
