using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.SchedulerAdmin.DTOs;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Queries;

public class GetAppointmentTypeById
{
    public record Query : IRequest<IQueryResponseDto<AppointmentTypeDto>>
    {
        public required ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<AppointmentTypeDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<AppointmentTypeDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var entity = await _db.AppointmentTypes
                .AsNoTracking()
                .Include(t => t.EligibleStaff)
                    .ThenInclude(e => e.SchedulerStaffMember)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(t => t.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                return new QueryResponseDto<AppointmentTypeDto>("Id", "Appointment type not found.");

            return new QueryResponseDto<AppointmentTypeDto>(
                AppointmentTypeDto.MapFrom(entity));
        }
    }
}
