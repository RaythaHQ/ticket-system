using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;

namespace App.Application.TicketConfig.Commands;

public class CreateTaskTemplate
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public List<TemplateItemInput> Items { get; init; } = new();
    }

    public record TemplateItemInput
    {
        public string Title { get; init; } = null!;
        public int SortOrder { get; init; }

        /// <summary>
        /// 0-based index into the Items list for the dependency. -1 or null means no dependency.
        /// </summary>
        public int? DependsOnIndex { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Items).NotEmpty().WithMessage("At least one task item is required.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.Title).NotEmpty().MaximumLength(500);
            });

            // Validate no circular dependencies in the DependsOnIndex references
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
            var template = new TaskTemplate
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                IsActive = true,
            };

            _db.TaskTemplates.Add(template);

            // Create items and map DependsOnIndex to actual item IDs
            var createdItems = new List<TaskTemplateItem>();
            foreach (var input in request.Items.OrderBy(i => i.SortOrder))
            {
                var item = new TaskTemplateItem
                {
                    Id = Guid.NewGuid(),
                    TaskTemplateId = template.Id,
                    Title = input.Title,
                    SortOrder = input.SortOrder,
                };
                createdItems.Add(item);
                _db.TaskTemplateItems.Add(item);
            }

            // Map dependency indexes to actual IDs
            var orderedInputs = request.Items.OrderBy(i => i.SortOrder).ToList();
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
