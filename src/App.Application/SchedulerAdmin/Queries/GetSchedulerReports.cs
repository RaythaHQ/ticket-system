using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.SchedulerAdmin.DTOs;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Queries;

public class GetSchedulerReports
{
    public record Query : IRequest<IQueryResponseDto<SchedulerReportDto>>
    {
        /// <summary>
        /// Optional start date filter (UTC).
        /// </summary>
        public DateTime? DateFrom { get; init; }

        /// <summary>
        /// Optional end date filter (UTC).
        /// </summary>
        public DateTime? DateTo { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<SchedulerReportDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentOrganization _currentOrganization;

        public Handler(IAppDbContext db, ICurrentOrganization currentOrganization)
        {
            _db = db;
            _currentOrganization = currentOrganization;
        }

        public async ValueTask<IQueryResponseDto<SchedulerReportDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.Appointments.AsNoTracking().AsQueryable();

            // Apply date filters (all comparisons in UTC)
            if (request.DateFrom.HasValue)
            {
                var dateFromUtc = DateTime.SpecifyKind(request.DateFrom.Value, DateTimeKind.Utc);
                query = query.Where(a => a.ScheduledStartTime >= dateFromUtc);
            }

            if (request.DateTo.HasValue)
            {
                var dateToUtc = DateTime.SpecifyKind(request.DateTo.Value, DateTimeKind.Utc);
                query = query.Where(a => a.ScheduledStartTime < dateToUtc);
            }

            var appointments = await query.ToListAsync(cancellationToken);
            var totalCount = appointments.Count;

            // Appointments by status
            var appointmentsByStatus = appointments
                .GroupBy(a => a.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            // Daily volume (convert to org timezone for display dates)
            var tzConverter = _currentOrganization.TimeZoneConverter;
            var appointmentVolumeByDate = appointments
                .GroupBy(a => tzConverter.UtcToTimeZone(a.ScheduledStartTime).Date)
                .OrderBy(g => g.Key)
                .Select(g => new SchedulerReportDto.DailyVolumeItem
                {
                    Date = g.Key,
                    Count = g.Count(),
                })
                .ToList();

            // Staff utilization
            var staffUtilization = await _db
                .SchedulerStaffMembers.AsNoTracking()
                .Where(s => s.IsActive)
                .Include(s => s.User)
                .Select(s => new
                {
                    StaffName = s.User.FirstName + " " + s.User.LastName,
                    StaffMemberId = s.Id,
                })
                .ToListAsync(cancellationToken);

            var staffAppointmentCounts = appointments
                .GroupBy(a => a.AssignedStaffMemberId)
                .ToDictionary(g => g.Key, g => g.Count());

            var staffUtilizationItems = staffUtilization
                .Select(s => new SchedulerReportDto.StaffUtilizationItem
                {
                    StaffName = s.StaffName,
                    AppointmentCount = staffAppointmentCounts.GetValueOrDefault(
                        s.StaffMemberId,
                        0
                    ),
                    UtilizationRate =
                        totalCount > 0
                            ? Math.Round(
                                (decimal)staffAppointmentCounts.GetValueOrDefault(
                                    s.StaffMemberId,
                                    0
                                )
                                    / totalCount
                                    * 100,
                                1
                            )
                            : 0,
                })
                .OrderByDescending(s => s.AppointmentCount)
                .ToList();

            // Rate metrics
            var noShowCount = appointments.Count(a => a.Status == AppointmentStatus.NO_SHOW);
            var cancelledCount = appointments.Count(a =>
                a.Status == AppointmentStatus.CANCELLED
            );
            var completedAppointments = appointments.Where(a =>
                a.Status == AppointmentStatus.COMPLETED
            );

            var noShowRate =
                totalCount > 0
                    ? Math.Round((decimal)noShowCount / totalCount * 100, 1)
                    : 0;

            var cancellationRate =
                totalCount > 0
                    ? Math.Round((decimal)cancelledCount / totalCount * 100, 1)
                    : 0;

            var averageDuration = completedAppointments.Any()
                ? Math.Round(
                    (decimal)completedAppointments.Average(a => a.DurationMinutes),
                    1
                )
                : 0;

            var report = new SchedulerReportDto
            {
                AppointmentsByStatus = appointmentsByStatus,
                AppointmentVolumeByDate = appointmentVolumeByDate,
                StaffUtilization = staffUtilizationItems,
                NoShowRate = noShowRate,
                CancellationRate = cancellationRate,
                AverageAppointmentDurationMinutes = averageDuration,
            };

            return new QueryResponseDto<SchedulerReportDto>(report);
        }
    }
}
