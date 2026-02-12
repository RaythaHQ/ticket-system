using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Queries;

public class GetTaskTemplateById
{
    public record Query : IRequest<QueryResponseDto<TaskTemplateDetailDto>>
    {
        public required ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, QueryResponseDto<TaskTemplateDetailDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<QueryResponseDto<TaskTemplateDetailDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var templateId = request.Id.Guid;
            var template = await _db.TaskTemplates.AsNoTracking()
                .Include(t => t.Items.OrderBy(i => i.SortOrder))
                .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

            if (template == null)
                return new QueryResponseDto<TaskTemplateDetailDto>("Id", "Task template not found.");

            var dto = new TaskTemplateDetailDto
            {
                Id = new ShortGuid(template.Id),
                Name = template.Name,
                Description = template.Description,
                IsActive = template.IsActive,
                Items = template.Items.Select((item, index) => new TaskTemplateItemDto
                {
                    Id = new ShortGuid(item.Id),
                    Title = item.Title,
                    SortOrder = item.SortOrder,
                    DependsOnItemId = item.DependsOnItemId.HasValue
                        ? new ShortGuid(item.DependsOnItemId.Value)
                        : null,
                    DependsOnItemTitle = item.DependsOnItemId.HasValue
                        ? template.Items.FirstOrDefault(i => i.Id == item.DependsOnItemId.Value)?.Title
                        : null,
                }).ToList(),
            };

            return new QueryResponseDto<TaskTemplateDetailDto>(dto);
        }
    }
}

public record TaskTemplateDetailDto
{
    public ShortGuid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public List<TaskTemplateItemDto> Items { get; init; } = new();
}

public record TaskTemplateItemDto
{
    public ShortGuid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public ShortGuid? DependsOnItemId { get; init; }
    public string? DependsOnItemTitle { get; init; }
}
