using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Security;
using App.Application.OrganizationSettings.Queries;
using App.Web.Authentication;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = AppApiAuthorizationHandler.POLICY_PREFIX + AppClaimTypes.IsAdmin)]
public class PingController : BaseController
{
    [HttpGet(Name = "Ping")]
    public async Task<ActionResult<HealthCheckResult>> Get()
    {
        var response = await Mediator.Send(new GetOrganizationSettings.Query());
        return new HealthCheckResult
        {
            Success = response.Success,
            Version = CurrentVersion.Version,
            OrganizationName = response.Result.OrganizationName,
        };
    }

    /// <summary>
    /// Test endpoint to trigger a crash for Sentry testing. Remove in production.
    /// </summary>
    [HttpGet("crash", Name = "ping-crash")]
    [AllowAnonymous]
    public IActionResult Crash()
    {
        throw new InvalidOperationException("This is a test exception for Sentry verification!");
    }
}

public class HealthCheckResult
{
    public bool Success { get; set; }
    public string Version { get; set; }
    public string OrganizationName { get; set; }
}
