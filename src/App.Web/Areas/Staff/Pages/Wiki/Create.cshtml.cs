using System.ComponentModel.DataAnnotations;
using App.Application.Wiki.Commands;
using App.Application.Wiki.Queries;
using App.Domain.Entities;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Wiki;

[Authorize(Policy = BuiltInSystemPermission.EDIT_WIKI_ARTICLES_PERMISSION)]
public class Create : BaseStaffPageModel
{
    [BindProperty]
    public FormModel Form { get; set; } = new();

    public List<string> ExistingCategories { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        await LoadCategoriesAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(cancellationToken);
            return Page();
        }

        var response = await Mediator.Send(
            new CreateWikiArticle.Command
            {
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
            SetSuccessMessage("Article created successfully.");
            return RedirectToPage(RouteNames.Wiki.Index);
        }

        SetErrorMessage(response.GetErrors());
        await LoadCategoriesAsync(cancellationToken);
        return Page();
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
        public bool IsPublished { get; set; } = false;

        [Display(Name = "Pinned")]
        public bool IsPinned { get; set; } = false;
    }
}
