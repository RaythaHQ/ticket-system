using Microsoft.AspNetCore.Routing;

namespace App.Web.Areas.Staff.Pages.Shared.Models;

public class BackLinkOptions
{
    public string Page { get; set; }
    public string Area { get; set; } = "Staff";
    public string Handler { get; set; }
    public string Fragment { get; set; }
    public RouteValueDictionary RouteValues { get; set; } = new();
    public string Href { get; set; }

    public string Text { get; set; } = "Back";
    public string Class { get; set; } = "staff-back-link";
    public string IconSvg { get; set; }
}
