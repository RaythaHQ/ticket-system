using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Queries;

public class GetAppointmentById
{
    public record Query : LoggableQuery<IQueryResponseDto<AppointmentDto>>
    {
        public long Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<AppointmentDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(IAppDbContext db, ISchedulerPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<AppointmentDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var appointment = await _db
                .Appointments.AsNoTracking()
                .Include(a => a.Contact)
                .Include(a => a.AssignedStaffMember)
                    .ThenInclude(s => s.User)
                .Include(a => a.AppointmentType)
                .Include(a => a.CreatedByStaff)
                .Include(a => a.History)
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (appointment == null)
                throw new NotFoundException("Appointment", request.Id);

            return new QueryResponseDto<AppointmentDto>(
                AppointmentDto.MapFrom(appointment)
            );
        }
    }
}
