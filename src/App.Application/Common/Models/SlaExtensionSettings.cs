namespace App.Application.Common.Models;

/// <summary>
/// Configuration settings for SLA extension limits.
/// Values sourced from environment variables.
/// </summary>
public class SlaExtensionSettings
{
    /// <summary>
    /// Maximum number of times non-privileged users can extend an SLA.
    /// Default: 1
    /// </summary>
    public int MaxExtensions { get; set; } = 1;

    /// <summary>
    /// Maximum hours non-privileged users can extend an SLA by.
    /// Default: 168 (7 days)
    /// </summary>
    public int MaxExtensionHours { get; set; } = 168;

    /// <summary>
    /// Creates settings from environment variables.
    /// Uses defaults if variables are not set or invalid.
    /// </summary>
    public static SlaExtensionSettings FromEnvironment()
    {
        return new SlaExtensionSettings
        {
            MaxExtensions = int.TryParse(
                Environment.GetEnvironmentVariable("SLA_MAX_EXTENSIONS"),
                out var max
            )
                ? max
                : 1,
            MaxExtensionHours = int.TryParse(
                Environment.GetEnvironmentVariable("SLA_MAX_EXTENSION_HOURS"),
                out var hours
            )
                ? hours
                : 168,
        };
    }
}

