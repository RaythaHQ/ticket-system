using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Wiki.Commands;

public class DeleteWikiArticle
{
    public record Command : LoggableRequest<CommandResponseDto<Guid>>
    {
        public Guid Id { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<Guid>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<Guid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var article = await _db.WikiArticles.FindAsync(
                new object[] { request.Id },
                cancellationToken
            );

            if (article == null)
            {
                return new CommandResponseDto<Guid>("Id", "Article not found");
            }

            _db.WikiArticles.Remove(article);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<Guid>(article.Id);
        }
    }
}
