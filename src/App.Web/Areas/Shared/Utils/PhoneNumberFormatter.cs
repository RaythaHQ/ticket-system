using System.Text.RegularExpressions;

namespace App.Web.Areas.Shared.Utils;

/// <summary>
/// Utility class for formatting phone numbers for display.
/// USA phone numbers are formatted as (XXX) XXX-XXXX.
/// International numbers are displayed as-is.
/// </summary>
public static class PhoneNumberFormatter
{
    private static readonly Regex DigitsOnly = new(@"[^\d]", RegexOptions.Compiled);

    /// <summary>
    /// Formats a phone number for display.
    /// If the number is a USA phone number (10 digits or +1 followed by 10 digits),
    /// it's formatted as (XXX) XXX-XXXX. Otherwise, the original value is returned.
    /// </summary>
    /// <param name="phoneNumber">The phone number to format (typically in E.164 format).</param>
    /// <returns>The formatted phone number for display.</returns>
    public static string FormatForDisplay(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        // Extract digits only
        var digits = DigitsOnly.Replace(phoneNumber, "");

        // Check for USA format:
        // - Exactly 10 digits (domestic format)
        // - 11 digits starting with 1 (country code + 10 digits)
        if (digits.Length == 10)
        {
            return FormatUsaNumber(digits);
        }

        if (digits.Length == 11 && digits.StartsWith("1"))
        {
            return FormatUsaNumber(digits.Substring(1));
        }

        // Not a USA number, return as-is
        return phoneNumber;
    }

    /// <summary>
    /// Formats a 10-digit USA phone number as (XXX) XXX-XXXX.
    /// </summary>
    private static string FormatUsaNumber(string tenDigits)
    {
        return $"({tenDigits.Substring(0, 3)}) {tenDigits.Substring(3, 3)}-{tenDigits.Substring(6, 4)}";
    }
}

