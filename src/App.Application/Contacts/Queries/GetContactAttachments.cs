using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Queries;

public class GetContactAttachments
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyList<ContactAttachmentDto>>>
    {
        public long ContactId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyList<ContactAttachmentDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IReadOnlyList<ContactAttachmentDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var attachments = await _db.ContactAttachments
                .AsNoTracking()
                .Include(a => a.MediaItem)
                .Include(a => a.UploadedByUser)
                .Where(a => a.ContactId == request.ContactId)
                .OrderByDescending(a => a.CreationTime)
                .Select(a => new ContactAttachmentDto
                {
                    Id = a.Id,
                    ContactId = a.ContactId,
                    MediaItemId = a.MediaItemId,
                    DisplayName = a.DisplayName,
                    Description = a.Description,
                    FileName = a.MediaItem.FileName,
                    ContentType = a.MediaItem.ContentType,
                    SizeBytes = a.MediaItem.Length,
                    ObjectKey = a.MediaItem.ObjectKey,
                    UploadedByUserId = a.UploadedByUserId,
                    UploadedByUserName = a.UploadedByUser.FirstName + " " + a.UploadedByUser.LastName,
                    CreatedAt = a.CreationTime
                })
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<IReadOnlyList<ContactAttachmentDto>>(attachments);
        }
    }
}

