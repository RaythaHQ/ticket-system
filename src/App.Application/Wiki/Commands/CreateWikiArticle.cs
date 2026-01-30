using System.Text.RegularExpressions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Wiki.Commands;

public class CreateWikiArticle
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Title { get; init; } = string.Empty;
        public string? Slug { get; init; }
        public string Content { get; init; } = string.Empty;
        public string? Category { get; init; }
        public string? Excerpt { get; init; }
        public bool IsPublished { get; init; } = false;
        public bool IsPinned { get; init; } = false;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
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

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Generate slug from title if not provided
            var slug = !string.IsNullOrEmpty(request.Slug)
                ? request.Slug.ToLower()
                : GenerateSlug(request.Title);

            // Ensure slug is unique
            slug = await EnsureUniqueSlugAsync(slug, null, cancellationToken);

            // Auto-calculate sort order within category
            var maxSortOrder = await _db.WikiArticles
                .Where(a => a.Category == request.Category)
                .MaxAsync(a => (int?)a.SortOrder, cancellationToken) ?? 0;

            var article = new WikiArticle
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Slug = slug,
                Content = request.Content,
                Category = request.Category,
                Excerpt = request.Excerpt,
                IsPublished = request.IsPublished,
                IsPinned = request.IsPinned,
                SortOrder = maxSortOrder + 1,
                ViewCount = 0,
            };

            _db.WikiArticles.Add(article);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(article.Id);
        }

        private static string GenerateSlug(string title)
        {
            // Convert to lowercase
            var slug = title.ToLower();
            // Remove special characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"\s+", "-");
            // Remove consecutive hyphens
            slug = Regex.Replace(slug, @"-+", "-");
            // Trim hyphens from ends
            slug = slug.Trim('-');

            return slug;
        }

        private async Task<string> EnsureUniqueSlugAsync(
            string baseSlug,
            Guid? excludeId,
            CancellationToken cancellationToken
        )
        {
            var slug = baseSlug;
            var suffix = 1;

            while (await SlugExistsAsync(slug, excludeId, cancellationToken))
            {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            return slug;
        }

        private async Task<bool> SlugExistsAsync(
            string slug,
            Guid? excludeId,
            CancellationToken cancellationToken
        )
        {
            var query = _db.WikiArticles.Where(a => a.Slug == slug);

            if (excludeId.HasValue)
                query = query.Where(a => a.Id != excludeId.Value);

            return await query.AnyAsync(cancellationToken);
        }
    }
}
