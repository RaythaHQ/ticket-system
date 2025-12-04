using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using App.Application.Common.Interfaces;
using App.Application.Common.Utils;

namespace App.Web.Areas.Api.Controllers.V1;

[Area("Api")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
[ApiController]
[Route("api/v1/[controller]")]
public class BaseController : Controller
{
    private ISender _mediator;
    private ICurrentOrganization _currentOrganization;
    private ICurrentUser _currentUser;
    private ICurrentVersion _currentVersion;
    private IFileStorageProviderSettings _fileStorageSettings;
    private IFileStorageProvider _fileStorageProvider;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected ICurrentOrganization CurrentOrganization =>
        _currentOrganization ??=
            HttpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
    protected ICurrentUser CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
    protected IFileStorageProvider FileStorageProvider =>
        _fileStorageProvider ??=
            HttpContext.RequestServices.GetRequiredService<IFileStorageProvider>();
    protected IFileStorageProviderSettings FileStorageProviderSettings =>
        _fileStorageSettings ??=
            HttpContext.RequestServices.GetRequiredService<IFileStorageProviderSettings>();
    protected ICurrentVersion CurrentVersion =>
        _currentVersion ??= HttpContext.RequestServices.GetRequiredService<ICurrentVersion>();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (CurrentOrganization.RedirectWebsite.IsValidUriFormat())
        {
            context.Result = new RedirectResult(CurrentOrganization.RedirectWebsite);
            return;
        }
    }
}
