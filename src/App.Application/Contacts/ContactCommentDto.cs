using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Contacts;

/// <summary>
/// Contact comment data transfer object.
/// </summary>
public record ContactCommentDto : BaseEntityDto
{
    public long ContactId { get; init; }
    public ShortGuid AuthorStaffId { get; init; }
    public string AuthorStaffName { get; init; } = null!;
    public string Body { get; init; } = null!;
    public DateTime CreationTime { get; init; }

    public static ContactCommentDto MapFrom(ContactComment comment)
    {
        return new ContactCommentDto
        {
            Id = comment.Id,
            ContactId = comment.ContactId,
            AuthorStaffId = comment.AuthorStaffId,
            AuthorStaffName = comment.AuthorStaff?.FullName ?? "Unknown",
            Body = comment.Body,
            CreationTime = comment.CreationTime
        };
    }
}

