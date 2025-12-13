using App.Application.Common.Models;
using App.Domain.Entities;

namespace App.Application.Contacts;

/// <summary>
/// Full contact data transfer object with all details.
/// </summary>
public record ContactDto : BaseNumericAuditableEntityDto
{
    public string FirstName { get; init; } = null!;
    public string? LastName { get; init; }

    /// <summary>
    /// Combined first and last name for display purposes.
    /// </summary>
    public string Name => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{FirstName} {LastName}";

    public string? Email { get; init; }
    public List<string> PhoneNumbers { get; init; } = new();
    public string? Address { get; init; }
    public string? OrganizationAccount { get; init; }
    public Dictionary<string, string> DmeIdentifiers { get; init; } = new();

    public int TicketCount { get; init; }

    public static ContactDto MapFrom(Contact contact)
    {
        return new ContactDto
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email,
            PhoneNumbers = contact.PhoneNumbers,
            Address = contact.Address,
            OrganizationAccount = contact.OrganizationAccount,
            DmeIdentifiers = contact.DmeIdentifiers,
            TicketCount = contact.Tickets?.Count ?? 0,
            CreationTime = contact.CreationTime,
            CreatorUserId = contact.CreatorUserId,
            LastModifierUserId = contact.LastModifierUserId,
            LastModificationTime = contact.LastModificationTime
        };
    }
}

