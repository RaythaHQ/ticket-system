using System.Net;
using System.Net.Sockets;
using App.Application.Webhooks.Services;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Services;

/// <summary>
/// Validates webhook URLs to prevent SSRF attacks by blocking internal/private addresses.
/// </summary>
public class UrlValidationService : IUrlValidationService
{
    private readonly ILogger<UrlValidationService> _logger;

    public UrlValidationService(ILogger<UrlValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<UrlValidationResult> ValidateAsync(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlValidationResult.Invalid("URL is required.");
        }

        // Try to parse the URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return UrlValidationResult.Invalid("Invalid URL format.");
        }

        // Must be HTTP or HTTPS
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return UrlValidationResult.Invalid("URL must use HTTP or HTTPS protocol.");
        }

        // Block localhost and local hostnames
        var host = uri.Host.ToLowerInvariant();
        if (IsBlockedHostname(host))
        {
            return UrlValidationResult.Invalid(
                "Webhook URLs cannot target localhost or internal hostnames."
            );
        }

        // Resolve DNS and check if it's a private IP
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            foreach (var address in addresses)
            {
                if (IsPrivateOrReservedAddress(address))
                {
                    _logger.LogWarning(
                        "Blocked webhook URL {Url} - resolves to private IP {IpAddress}",
                        url,
                        address
                    );
                    return UrlValidationResult.Invalid(
                        "Webhook URLs cannot target private or internal IP addresses."
                    );
                }
            }
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Failed to resolve DNS for webhook URL {Url}", url);
            return UrlValidationResult.Invalid(
                "Could not resolve the hostname. Please check the URL."
            );
        }

        return UrlValidationResult.Valid();
    }

    private static bool IsBlockedHostname(string host)
    {
        // Block localhost variations
        if (host == "localhost" || host == "127.0.0.1" || host == "::1" || host == "[::1]")
        {
            return true;
        }

        // Block .local domains (mDNS)
        if (host.EndsWith(".local") || host.EndsWith(".localhost"))
        {
            return true;
        }

        // Block internal domain patterns
        if (host.EndsWith(".internal") || host.EndsWith(".corp") || host.EndsWith(".lan"))
        {
            return true;
        }

        // Block metadata endpoints (AWS, GCP, Azure)
        if (host == "169.254.169.254" || host == "metadata.google.internal")
        {
            return true;
        }

        return false;
    }

    private static bool IsPrivateOrReservedAddress(IPAddress address)
    {
        // Handle IPv4-mapped IPv6 addresses
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        // Check for loopback (127.0.0.0/8, ::1)
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        // IPv4 checks
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            // 10.0.0.0/8 - Private
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12 - Private
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16 - Private
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 169.254.0.0/16 - Link-local
            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }

            // 127.0.0.0/8 - Loopback (already checked above but for completeness)
            if (bytes[0] == 127)
            {
                return true;
            }

            // 0.0.0.0/8 - Current network
            if (bytes[0] == 0)
            {
                return true;
            }
        }

        // IPv6 checks
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // fc00::/7 - Unique local addresses
            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC)
            {
                return true;
            }

            // fe80::/10 - Link-local
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
            {
                return true;
            }
        }

        return false;
    }
}
