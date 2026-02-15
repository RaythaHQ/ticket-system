using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.SchedulerAdmin.DTOs;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Queries;

public class GetSchedulerStaffById
{
    public record Query : IRequest<IQueryResponseDto<SchedulerStaffDto>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<SchedulerStaffDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<SchedulerStaffDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.SchedulerStaffMembers
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.EligibleAppointmentTypes)
                    .ThenInclude(e => e.AppointmentType)
                .FirstOrDefaultAsync(s => s.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("SchedulerStaffMember", request.Id);

            return new QueryResponseDto<SchedulerStaffDto>(SchedulerStaffDto.MapFrom(entity));
        }
    }
}
