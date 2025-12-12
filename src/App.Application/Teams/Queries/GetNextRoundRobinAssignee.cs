using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;

namespace App.Application.Teams.Queries;

public class GetNextRoundRobinAssignee
{
    public record Query : IRequest<IQueryResponseDto<RoundRobinAssigneeDto?>>
    {
        public ShortGuid TeamId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<RoundRobinAssigneeDto?>>
    {
        private readonly IRoundRobinService _roundRobinService;

        public Handler(IRoundRobinService roundRobinService)
        {
            _roundRobinService = roundRobinService;
        }

        public async ValueTask<IQueryResponseDto<RoundRobinAssigneeDto?>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var assigneeId = await _roundRobinService.GetNextAssigneeAsync(request.TeamId.Guid, cancellationToken);

            RoundRobinAssigneeDto? result = assigneeId.HasValue
                ? new RoundRobinAssigneeDto { StaffAdminId = assigneeId.Value }
                : null;

            return new QueryResponseDto<RoundRobinAssigneeDto?>(result);
        }
    }
}

public record RoundRobinAssigneeDto
{
    public ShortGuid StaffAdminId { get; init; }
}

