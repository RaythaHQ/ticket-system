using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class UpdateSchedulerEmailTemplate
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid TemplateId { get; init; }
        public string? Subject { get; init; }
        public string Content { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.TemplateId).NotEmpty();

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Template content is required.");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    var template = await db.SchedulerEmailTemplates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == command.TemplateId.Guid, cancellationToken);

                    if (template == null) return false;

                    // Subject is required for email channel
                    if (template.Channel == SchedulerEmailTemplate.CHANNEL_EMAIL
                        && string.IsNullOrWhiteSpace(command.Subject))
                    {
                        return false;
                    }

                    return true;
                })
                .WithMessage("Template not found, or subject is required for email channel templates.");
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
            var entity = await _db.SchedulerEmailTemplates
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId.Guid, cancellationToken);

            if (entity == null)
                return new CommandResponseDto<ShortGuid>("TemplateId", "Scheduler email template not found.");

            entity.Subject = request.Subject;
            entity.Content = request.Content;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
