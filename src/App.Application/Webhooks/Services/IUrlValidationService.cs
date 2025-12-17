namespace App.Application.Webhooks.Services;

/// <summary>
/// Service for validating webhook URLs.
/// </summary>
public interface IUrlValidationService
{
    /// <summary>
    /// Validates a URL for use as a webhook endpoint.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>A validation result indicating whether the URL is valid.</returns>
    Task<UrlValidationResult> ValidateAsync(
        string url,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Result of URL validation.
/// </summary>
public record UrlValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static UrlValidationResult Valid() => new() { IsValid = true };

    public static UrlValidationResult Invalid(string message) =>
        new() { IsValid = false, ErrorMessage = message };
}
