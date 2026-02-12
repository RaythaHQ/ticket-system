using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Queries;

public class GetTaskTemplates
{
    public record Query : IRequest<QueryResponseDto<ListResultDto<TaskTemplateListItemDto>>>
    {
        /// <summary>
        /// When true, only returns active templates (for staff template picker).
        /// </summary>
        public bool ActiveOnly { get; init; }
    }

    public class Handler : IRequestHandler<Query, QueryResponseDto<ListResultDto<TaskTemplateListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<QueryResponseDto<ListResultDto<TaskTemplateListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var query = _db.TaskTemplates.AsNoTracking();

            if (request.ActiveOnly)
                query = query.Where(t => t.IsActive);

            var templates = await query
                .Include(t => t.Items)
                .OrderBy(t => t.Name)
                .Select(t => new TaskTemplateListItemDto
                {
                    Id = new ShortGuid(t.Id),
                    Name = t.Name,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    ItemCount = t.Items.Count,
                    CreatedAt = t.CreationTime,
                })
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<TaskTemplateListItemDto>>(
                new ListResultDto<TaskTemplateListItemDto>(templates, templates.Count));
        }
    }
}

public record TaskTemplateListItemDto
{
    public ShortGuid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
