using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.SchedulerAdmin.DTOs;
using App.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Queries;

public class GetSchedulerEmailTemplates
{
    public record Query : IRequest<IQueryResponseDto<ListResultDto<SchedulerEmailTemplateDto>>>
    {
        /// <summary>
        /// Optional filter by channel (e.g. "email" or "sms"). Null returns all.
        /// </summary>
        public string? Channel { get; init; }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<SchedulerEmailTemplateDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<SchedulerEmailTemplateDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var query = _db.SchedulerEmailTemplates
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Channel))
            {
                query = query.Where(t => t.Channel == request.Channel);
            }

            var templates = await query
                .OrderBy(t => t.TemplateType)
                .ThenBy(t => t.Channel)
                .ToListAsync(cancellationToken);

            var items = templates
                .Select(SchedulerEmailTemplateDto.MapFrom)
                .ToList();

            return new QueryResponseDto<ListResultDto<SchedulerEmailTemplateDto>>(
                new ListResultDto<SchedulerEmailTemplateDto>(items, items.Count));
        }
    }
}
