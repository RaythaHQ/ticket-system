using App.Application.Common.Models;
using App.Domain.Entities;

namespace App.Application.Contacts;

/// <summary>
/// Lightweight contact DTO for list views.
/// </summary>
public record ContactListItemDto : BaseNumericEntityDto
{
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? PrimaryPhone { get; init; }
    public string? OrganizationAccount { get; init; }
    public int TicketCount { get; init; }
    public int CommentCount { get; init; }
    public DateTime CreationTime { get; init; }

    public static ContactListItemDto MapFrom(Contact contact)
    {
        return new ContactListItemDto
        {
            Id = contact.Id,
            Name = contact.Name,
            Email = contact.Email,
            PrimaryPhone = contact.PhoneNumbers.FirstOrDefault(),
            OrganizationAccount = contact.OrganizationAccount,
            TicketCount = contact.Tickets?.Count ?? 0,
            CommentCount = contact.Comments?.Count ?? 0,
            CreationTime = contact.CreationTime
        };
    }
}

