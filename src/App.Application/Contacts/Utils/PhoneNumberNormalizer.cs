using System.Text.RegularExpressions;

namespace App.Application.Contacts.Utils;

/// <summary>
/// Utility class for normalizing phone numbers to E.164 format.
/// </summary>
public static class PhoneNumberNormalizer
{
    /// <summary>
    /// Normalizes a phone number to E.164 format.
    /// Returns null if the input is null/empty or cannot be normalized.
    /// </summary>
    /// <param name="phoneNumber">The phone number to normalize.</param>
    /// <param name="defaultCountryCode">The default country code to use if not present (default: 1 for US/Canada).</param>
    /// <returns>The normalized phone number in E.164 format (e.g., +15551234567) or null.</returns>
    public static string? Normalize(string? phoneNumber, string defaultCountryCode = "1")
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove all non-digit characters except leading +
        var cleaned = Regex.Replace(phoneNumber.Trim(), @"[^\d+]", "");

        // If empty after cleaning, return null
        if (string.IsNullOrEmpty(cleaned))
            return null;

        // If starts with +, assume it's already in E.164 format
        if (cleaned.StartsWith("+"))
        {
            // Validate: E.164 should have 8-15 digits after the +
            var digitsOnly = cleaned.Substring(1);
            if (digitsOnly.Length >= 8 && digitsOnly.Length <= 15 && digitsOnly.All(char.IsDigit))
            {
                return cleaned;
            }
            return null;
        }

        // Remove leading zeros
        cleaned = cleaned.TrimStart('0');

        // If starts with country code (e.g., 1 for US), add +
        if (cleaned.Length == 11 && cleaned.StartsWith(defaultCountryCode))
        {
            return "+" + cleaned;
        }

        // If 10 digits (US/Canada format), add default country code
        if (cleaned.Length == 10)
        {
            return "+" + defaultCountryCode + cleaned;
        }

        // If between 8-15 digits, assume it's a valid international number
        if (cleaned.Length >= 8 && cleaned.Length <= 15)
        {
            return "+" + cleaned;
        }

        // Cannot normalize
        return null;
    }

    /// <summary>
    /// Normalizes multiple phone numbers.
    /// </summary>
    public static List<string> NormalizeMany(IEnumerable<string>? phoneNumbers, string defaultCountryCode = "1")
    {
        if (phoneNumbers == null)
            return new List<string>();

        return phoneNumbers
            .Select(p => Normalize(p, defaultCountryCode))
            .Where(p => p != null)
            .Cast<string>()
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Extracts digits from a phone number for flexible search matching.
    /// </summary>
    public static string ExtractDigits(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        return Regex.Replace(phoneNumber, @"[^\d]", "");
    }

    /// <summary>
    /// Checks if a normalized phone number matches a search query.
    /// Supports partial matching and various input formats.
    /// </summary>
    public static bool Matches(string? normalizedPhone, string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhone) || string.IsNullOrWhiteSpace(searchQuery))
            return false;

        var phoneDigits = ExtractDigits(normalizedPhone);
        var searchDigits = ExtractDigits(searchQuery);

        // Match if search digits appear anywhere in phone digits
        return phoneDigits.Contains(searchDigits);
    }
}

