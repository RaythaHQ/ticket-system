#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Interfaces;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared.Infrastructure.Navigation;
using App.Web.Authentication;

namespace App.Web.Areas.Admin.Pages.Shared.Components.Sidebar;

/// <summary>
/// ViewComponent for rendering the admin sidebar navigation.
/// Uses NavMap to build the navigation structure, filters items by permissions,
/// and injects dynamic content type menu items.
/// </summary>
public class SidebarViewComponent : ViewComponent
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentOrganization _currentOrganization;

    public SidebarViewComponent(
        IAuthorizationService authorizationService,
        ICurrentUser currentUser,
        ICurrentOrganization currentOrganization
    )
    {
        _authorizationService = authorizationService;
        _currentUser = currentUser;
        _currentOrganization = currentOrganization;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var activeMenu = ViewContext.ViewData["ActiveMenu"]?.ToString();
        var menuItems = await BuildMenuAsync(activeMenu);

        return View(new SidebarViewModel { MenuItems = menuItems, ActiveMenu = activeMenu });
    }

    /// <summary>
    /// Builds the complete menu including static items and dynamic content types.
    /// </summary>
    private async Task<IEnumerable<NavMenuItem>> BuildMenuAsync(string? activeMenu)
    {
        var allItems = new List<NavMenuItem>();

        // Get static menu items from NavMap
        var staticItems = NavMap.GetMenuItems().ToList();

        // Add profile menu with user info
        var profileMenu = NavMap.GetProfileMenu(
            _currentUser.FullName ?? "User",
            _currentOrganization.EmailAndPasswordIsEnabledForAdmins
        );
        staticItems.Add(profileMenu);

        // Filter by permissions
        foreach (var item in staticItems.OrderBy(i => i.Order))
        {
            var processedItem = await ProcessMenuItemAsync(item);
            if (processedItem != null)
            {
                allItems.Add(processedItem);
            }
        }

        return allItems;
    }

    /// <summary>
    /// Processes a single menu item: checks permissions and processes children recursively.
    /// Returns null if the item should be filtered out.
    /// </summary>
    private async Task<NavMenuItem?> ProcessMenuItemAsync(NavMenuItem item)
    {
        // Check permission if required
        if (!string.IsNullOrEmpty(item.Permission))
        {
            var authResult = await _authorizationService.AuthorizeAsync(
                HttpContext.User,
                item.Permission
            );

            if (!authResult.Succeeded)
            {
                return null; // Filter out
            }
        }

        // Process children recursively
        List<NavMenuItem>? processedChildren = null;
        if (item.Children != null && item.Children.Any())
        {
            processedChildren = new List<NavMenuItem>();
            foreach (var child in item.Children.OrderBy(c => c.Order))
            {
                var processedChild = await ProcessMenuItemAsync(child);
                if (processedChild != null)
                {
                    processedChildren.Add(processedChild);
                }
            }

            // If parent has children but all were filtered out, filter out the parent too
            if (!processedChildren.Any())
            {
                return null;
            }
        }

        // Return the item with processed children
        return new NavMenuItem
        {
            Id = item.Id,
            Label = item.Label,
            RouteName = item.RouteName,
            Icon = item.Icon,
            Permission = item.Permission,
            Order = item.Order,
            Children = processedChildren,
            IsDivider = item.IsDivider,
            CssClass = item.CssClass,
            OpenInNewTab = item.OpenInNewTab,
        };
    }
}

/// <summary>
/// ViewModel for the Sidebar ViewComponent.
/// </summary>
public class SidebarViewModel
{
    public IEnumerable<NavMenuItem> MenuItems { get; set; } = Enumerable.Empty<NavMenuItem>();
    public string? ActiveMenu { get; set; }
}
