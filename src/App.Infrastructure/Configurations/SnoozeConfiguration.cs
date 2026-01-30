using Microsoft.Extensions.Configuration;
using App.Application.Common.Interfaces;

namespace App.Infrastructure.Configurations;

/// <summary>
/// Configuration for ticket snooze feature, reads from environment variables.
/// </summary>
public class SnoozeConfiguration : ISnoozeConfiguration
{
    private const int DefaultMaxDurationDays = 90;
    private readonly IConfiguration _configuration;

    public SnoozeConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int MaxDurationDays =>
        int.TryParse(_configuration["SNOOZE_MAX_DURATION_DAYS"], out var days)
            ? days
            : DefaultMaxDurationDays;
}
