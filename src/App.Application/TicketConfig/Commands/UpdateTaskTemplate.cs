using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class UpdateTaskTemplate
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public List<TemplateItemInput> Items { get; init; } = new();
    }

    public record TemplateItemInput
    {
        public string Title { get; init; } = null!;
        public int SortOrder { get; init; }
        public int? DependsOnIndex { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Items).NotEmpty().WithMessage("At least one task item is required.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.Title).NotEmpty().MaximumLength(500);
            });

            RuleFor(x => x.Items).Must(items =>
            {
                if (items == null) return true;
                for (int i = 0; i < items.Count; i++)
                {
                    var depIdx = items[i].DependsOnIndex;
                    if (depIdx.HasValue && depIdx.Value >= 0)
                    {
                        if (depIdx.Value >= items.Count || depIdx.Value == i)
                            return false;
                    }
                }
                return true;
            }).WithMessage("Invalid dependency references in template items.");
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

            template.Name = request.Name;
            template.Description = request.Description;

            // Replace all items: remove existing, add new
            foreach (var existingItem in template.Items.ToList())
            {
                _db.TaskTemplateItems.Remove(existingItem);
            }

            var createdItems = new List<TaskTemplateItem>();
            var orderedInputs = request.Items.OrderBy(i => i.SortOrder).ToList();
            foreach (var input in orderedInputs)
            {
                var item = new TaskTemplateItem
                {
                    Id = Guid.NewGuid(),
                    TaskTemplateId = templateId,
                    Title = input.Title,
                    SortOrder = input.SortOrder,
                };
                createdItems.Add(item);
                _db.TaskTemplateItems.Add(item);
            }

            // Map dependency indexes to IDs
            for (int i = 0; i < orderedInputs.Count; i++)
            {
                var depIdx = orderedInputs[i].DependsOnIndex;
                if (depIdx.HasValue && depIdx.Value >= 0 && depIdx.Value < createdItems.Count)
                {
                    createdItems[i].DependsOnItemId = createdItems[depIdx.Value].Id;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(template.Id);
        }
    }
}
