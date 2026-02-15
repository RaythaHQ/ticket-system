using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.SchedulerAdmin.DTOs;
using App.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Queries;

public class GetSchedulerConfiguration
{
    public record Query : IRequest<IQueryResponseDto<SchedulerConfigurationDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<SchedulerConfigurationDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<SchedulerConfigurationDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var config = await _db.SchedulerConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (config == null)
            {
                // Create default configuration with standard business hours Mon-Fri 9-5
                config = new SchedulerConfiguration
                {
                    Id = Guid.NewGuid(),
                    AvailableHours = new Dictionary<string, DaySchedule>
                    {
                        { "monday", new DaySchedule { Start = "09:00", End = "17:00" } },
                        { "tuesday", new DaySchedule { Start = "09:00", End = "17:00" } },
                        { "wednesday", new DaySchedule { Start = "09:00", End = "17:00" } },
                        { "thursday", new DaySchedule { Start = "09:00", End = "17:00" } },
                        { "friday", new DaySchedule { Start = "09:00", End = "17:00" } },
                    },
                    DefaultDurationMinutes = 30,
                    DefaultBufferTimeMinutes = 15,
                    DefaultBookingHorizonDays = 30,
                    MinCancellationNoticeHours = 24,
                    ReminderLeadTimeMinutes = 60,
                };

                _db.SchedulerConfigurations.Add(config);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new QueryResponseDto<SchedulerConfigurationDto>(
                SchedulerConfigurationDto.MapFrom(config));
        }
    }
}
