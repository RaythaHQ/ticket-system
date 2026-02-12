using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class DeleteTaskTemplate
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid Id { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var templateId = request.Id.Guid;
            var template = await _db.TaskTemplates
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

            if (template == null)
                return new CommandResponseDto<ShortGuid>("Id", "Task template not found.");

            // Soft delete template and all items
            template.IsDeleted = true;
            template.DeletionTime = DateTime.UtcNow;
            foreach (var item in template.Items)
            {
                item.IsDeleted = true;
                item.DeletionTime = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(template.Id);
        }
    }
}
