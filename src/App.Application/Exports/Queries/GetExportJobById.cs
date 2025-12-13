using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Exports.Queries;

public class GetExportJobById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<ExportJobDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ExportJobDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ExportJobDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var entity = await _db.ExportJobs
                .Include(e => e.Requester)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("ExportJob", request.Id);

            return new QueryResponseDto<ExportJobDto>(ExportJobDto.GetProjection(entity));
        }
    }
}

