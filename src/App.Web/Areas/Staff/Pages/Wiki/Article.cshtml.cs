using App.Application.Wiki;
using App.Application.Wiki.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Wiki;

public class Article : BaseStaffPageModel
{
    public WikiArticleDto WikiArticle { get; set; } = null!;
    public bool CanEdit { get; set; }

    public async Task<IActionResult> OnGet(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug))
            return RedirectToPage(RouteNames.Wiki.Index);

        CanEdit = TicketPermissionService.CanEditWikiArticles();

        var response = await Mediator.Send(
            new GetWikiArticleBySlug.Query
            {
                Slug = slug,
                PublishedOnly = !CanEdit, // Editors can view drafts
                IncrementViewCount = true,
            },
            cancellationToken
        );

        if (response.Result == null)
        {
            SetErrorMessage("Article not found.");
            return RedirectToPage(RouteNames.Wiki.Index);
        }

        WikiArticle = response.Result;
        return Page();
    }
}
