using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.EmailTemplates.Queries;

public class GetEmailTemplateById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<EmailTemplateDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<EmailTemplateDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<EmailTemplateDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .EmailTemplates.AsNoTracking()
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("EmailTemplate", request.Id);

            return new QueryResponseDto<EmailTemplateDto>(EmailTemplateDto.GetProjection(entity));
        }
    }
}
