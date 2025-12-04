using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Web.Areas.Shared.Models;

namespace App.Web.Areas.Public.Pages.Shared.Models;

[Area("Public")]
[Authorize]
public abstract class BasePublicPageModel : BasePageModel { }

