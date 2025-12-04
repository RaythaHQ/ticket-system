using Mediator;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;

namespace App.Application.OrganizationSettings.Queries;

public class GetOrganizationSettings
{
    public record Query : IRequest<IQueryResponseDto<OrganizationSettingsDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<OrganizationSettingsDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<OrganizationSettingsDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var settings = _db.OrganizationSettings.FirstOrDefault();

            return new QueryResponseDto<OrganizationSettingsDto>(
                OrganizationSettingsDto.GetProjection(settings)
            );
        }
    }
}
