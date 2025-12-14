using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Imports.Queries;

public class GetImportJobById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<ImportJobDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ImportJobDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ImportJobDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db
                .ImportJobs.Include(e => e.Requester)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("ImportJob", request.Id);

            return new QueryResponseDto<ImportJobDto>(ImportJobDto.GetProjection(entity));
        }
    }
}
