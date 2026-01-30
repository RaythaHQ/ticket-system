using System.Text.RegularExpressions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Wiki.Commands;

public class UpdateWikiArticle
{
    public record Command : LoggableRequest<CommandResponseDto<Guid>>
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Slug { get; init; }
        public string Content { get; init; } = string.Empty;
        public string? Category { get; init; }
        public string? Excerpt { get; init; }
        public bool IsPublished { get; init; }
        public bool IsPinned { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Content)
                .NotEmpty();

            RuleFor(x => x.Category)
                .MaximumLength(200);

            RuleFor(x => x.Excerpt)
                .MaximumLength(1000);

            RuleFor(x => x.Slug)
                .MaximumLength(500)
                .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .When(x => !string.IsNullOrEmpty(x.Slug))
                .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");
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

            // Generate slug from title if not provided
            var slug = !string.IsNullOrEmpty(request.Slug)
                ? request.Slug.ToLower()
                : GenerateSlug(request.Title);

            // Ensure slug is unique (excluding current article)
            slug = await EnsureUniqueSlugAsync(slug, request.Id, cancellationToken);

            // Update category - may need to update sort order
            if (article.Category != request.Category)
            {
                var maxSortOrder = await _db.WikiArticles
                    .Where(a => a.Category == request.Category)
                    .MaxAsync(a => (int?)a.SortOrder, cancellationToken) ?? 0;
                article.SortOrder = maxSortOrder + 1;
            }

            article.Title = request.Title;
            article.Slug = slug;
            article.Content = request.Content;
            article.Category = request.Category;
            article.Excerpt = request.Excerpt;
            article.IsPublished = request.IsPublished;
            article.IsPinned = request.IsPinned;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<Guid>(article.Id);
        }

        private static string GenerateSlug(string title)
        {
            var slug = title.ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');
            return slug;
        }

        private async Task<string> EnsureUniqueSlugAsync(
            string baseSlug,
            Guid excludeId,
            CancellationToken cancellationToken
        )
        {
            var slug = baseSlug;
            var suffix = 1;

            while (await _db.WikiArticles
                .Where(a => a.Slug == slug && a.Id != excludeId)
                .AnyAsync(cancellationToken))
            {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            return slug;
        }
    }
}
