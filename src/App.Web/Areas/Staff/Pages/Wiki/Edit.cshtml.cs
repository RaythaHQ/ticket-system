using System.ComponentModel.DataAnnotations;
using App.Application.Wiki;
using App.Application.Wiki.Commands;
using App.Application.Wiki.Queries;
using App.Domain.Entities;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Wiki;

[Authorize(Policy = BuiltInSystemPermission.EDIT_WIKI_ARTICLES_PERMISSION)]
public class Edit : BaseStaffPageModel
{
    [BindProperty]
    public FormModel Form { get; set; } = new();

    public WikiArticleDto Article { get; set; } = null!;
    public List<string> ExistingCategories { get; set; } = new();

    public async Task<IActionResult> OnGet(ShortGuid id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new GetWikiArticleById.Query { Id = id.Guid },
            cancellationToken
        );

        if (response.Result == null)
        {
            SetErrorMessage("Article not found.");
            return RedirectToPage(RouteNames.Wiki.Index);
        }

        Article = response.Result;
        Form = new FormModel
        {
            Id = Article.Id,
            Title = Article.Title,
            Content = Article.Content,
            Category = Article.Category,
            Excerpt = Article.Excerpt,
            IsPublished = Article.IsPublished,
            IsPinned = Article.IsPinned,
        };

        await LoadCategoriesAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            // Reload article for display
            var articleResponse = await Mediator.Send(
                new GetWikiArticleById.Query { Id = Form.Id },
                cancellationToken
            );
            if (articleResponse.Result != null)
                Article = articleResponse.Result;

            await LoadCategoriesAsync(cancellationToken);
            return Page();
        }

        var response = await Mediator.Send(
            new UpdateWikiArticle.Command
            {
                Id = Form.Id,
                Title = Form.Title,
                Content = Form.Content,
                Category = string.IsNullOrWhiteSpace(Form.Category) ? null : Form.Category.Trim(),
                Excerpt = string.IsNullOrWhiteSpace(Form.Excerpt) ? null : Form.Excerpt.Trim(),
                IsPublished = Form.IsPublished,
                IsPinned = Form.IsPinned,
            },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage("Article updated successfully.");
            return RedirectToPage(RouteNames.Wiki.Index);
        }

        SetErrorMessage(response.GetErrors());
        
        // Reload article for display
        var reloadResponse = await Mediator.Send(
            new GetWikiArticleById.Query { Id = Form.Id },
            cancellationToken
        );
        if (reloadResponse.Result != null)
            Article = reloadResponse.Result;

        await LoadCategoriesAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDelete(Guid id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new DeleteWikiArticle.Command { Id = id },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage("Article deleted successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Wiki.Index);
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new GetWikiCategories.Query { PublishedOnly = false },
            cancellationToken
        );
        ExistingCategories = response.Result.Select(c => c.Name).ToList();
    }

    public class FormModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(500)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Excerpt")]
        public string? Excerpt { get; set; }

        [Display(Name = "Published")]
        public bool IsPublished { get; set; }

        [Display(Name = "Pinned")]
        public bool IsPinned { get; set; }
    }
}
