using Mediator;
using Microsoft.EntityFrameworkCore;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;

namespace App.Application.Admins.Queries;

public class GetAdminById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<AdminDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<AdminDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<AdminDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .Users.Include(p => p.Roles)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            return new QueryResponseDto<AdminDto>(AdminDto.GetProjection(entity));
        }
    }
}
