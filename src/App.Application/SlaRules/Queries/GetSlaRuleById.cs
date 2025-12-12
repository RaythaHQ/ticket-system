using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SlaRules.Queries;

public class GetSlaRuleById
{
    public record Query : IRequest<IQueryResponseDto<SlaRuleDto>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<SlaRuleDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<SlaRuleDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var rule = await _db.SlaRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.Id.Guid, cancellationToken);

            if (rule == null)
                throw new NotFoundException("SlaRule", request.Id);

            return new QueryResponseDto<SlaRuleDto>(SlaRuleDto.MapFrom(rule));
        }
    }
}

