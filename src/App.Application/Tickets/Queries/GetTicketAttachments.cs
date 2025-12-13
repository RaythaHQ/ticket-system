using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetTicketAttachments
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyList<TicketAttachmentDto>>>
    {
        public long TicketId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyList<TicketAttachmentDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IReadOnlyList<TicketAttachmentDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var attachments = await _db.TicketAttachments
                .AsNoTracking()
                .Include(a => a.MediaItem)
                .Include(a => a.UploadedByUser)
                .Where(a => a.TicketId == request.TicketId)
                .OrderByDescending(a => a.CreationTime)
                .Select(a => new TicketAttachmentDto
                {
                    Id = a.Id,
                    TicketId = a.TicketId,
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

            return new QueryResponseDto<IReadOnlyList<TicketAttachmentDto>>(attachments);
        }
    }
}

