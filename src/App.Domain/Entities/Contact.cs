using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace App.Domain.Entities;

/// <summary>
/// External person or entity associated with tickets (patients, providers, insurance reps, etc.).
/// Uses numeric (long) ID for human-readable contact numbers.
/// </summary>
public class Contact : BaseNumericFullAuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumbersJson { get; set; } // E.164 normalized, stored as JSON array
    public string? Address { get; set; }
    public string? OrganizationAccount { get; set; }
    public string? DmeIdentifiersJson { get; set; } // JSON object for DME-specific IDs

    [NotMapped]
    public List<string> PhoneNumbers
    {
        get => string.IsNullOrEmpty(PhoneNumbersJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(PhoneNumbersJson) ?? new List<string>();
        set => PhoneNumbersJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public Dictionary<string, string> DmeIdentifiers
    {
        get => string.IsNullOrEmpty(DmeIdentifiersJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(DmeIdentifiersJson) ?? new Dictionary<string, string>();
        set => DmeIdentifiersJson = JsonSerializer.Serialize(value);
    }

    // Collections
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<ContactChangeLogEntry> ChangeLogEntries { get; set; } = new List<ContactChangeLogEntry>();
    public virtual ICollection<ContactComment> Comments { get; set; } = new List<ContactComment>();
}

