using App.Application.Wiki;
using App.Application.Wiki.Queries;
using App.Domain.Entities;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Wiki;

public class Index : BaseStaffPageModel
{
    public IEnumerable<WikiArticleListItemDto> Articles { get; set; } = Enumerable.Empty<WikiArticleListItemDto>();
    public List<WikiCategoryDto> Categories { get; set; } = new();
    public string? SelectedCategory { get; set; }
    public string? SearchQuery { get; set; }
    public bool CanEdit { get; set; }
    public int TotalArticleCount { get; set; }

    public async Task<IActionResult> OnGet(
        string? category,
        string? search,
        CancellationToken cancellationToken
    )
    {
        SelectedCategory = category;
        SearchQuery = search;
        CanEdit = TicketPermissionService.CanEditWikiArticles();

        // Get categories
        var categoriesResponse = await Mediator.Send(
            new GetWikiCategories.Query { PublishedOnly = !CanEdit },
            cancellationToken
        );
        Categories = categoriesResponse.Result;

        // Get total article count (unfiltered)
        var totalResponse = await Mediator.Send(
            new GetWikiArticles.Query
            {
                PublishedOnly = !CanEdit,
                PageSize = 1, // We only need the count
            },
            cancellationToken
        );
        TotalArticleCount = totalResponse.Result.TotalCount;

        // Get filtered articles
        var articlesResponse = await Mediator.Send(
            new GetWikiArticles.Query
            {
                Category = category,
                Search = search,
                PublishedOnly = !CanEdit, // Editors can see unpublished articles
                PageSize = 100,
            },
            cancellationToken
        );
        Articles = articlesResponse.Result.Items;

        return Page();
    }
}
