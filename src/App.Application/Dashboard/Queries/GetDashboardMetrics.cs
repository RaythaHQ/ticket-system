using Mediator;
using Microsoft.EntityFrameworkCore;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;

namespace App.Application.Dashboard.Queries;

public class GetDashboardMetrics
{
    public record Query : IRequest<IQueryResponseDto<DashboardDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<DashboardDto>>
    {
        private readonly IAppDbContext _db;
        public readonly IAppRawDbInfo _rawSqlDb;

        public Handler(IAppDbContext db, IAppRawDbInfo rawSqlDb)
        {
            _rawSqlDb = rawSqlDb;
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<DashboardDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            int totalUsers = await _db.Users.CountAsync(cancellationToken);
            var dbSize = _rawSqlDb.GetDatabaseSize();

            decimal numericValueOfReserved = Convert.ToDecimal(dbSize.reserved.Split(" ").First());
            string units = dbSize.reserved.Split(" ").Last();
            decimal dbSizeInMb = ComputeToMb(numericValueOfReserved, units);
            return new QueryResponseDto<DashboardDto>(
                new DashboardDto
                {
                    TotalUsers = totalUsers,
                    DbSize = dbSizeInMb,
                }
            );
        }

        private decimal ComputeToMb(decimal rawValue, string units)
        {
            switch (units)
            {
                case "KB":
                    return rawValue / 1000;
                case "GB":
                    return rawValue * 1000;
                default:
                    return rawValue;
            }
        }
    }
}
