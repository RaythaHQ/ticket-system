using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using App.Web.Authentication;

namespace App.Web.Middlewares;

public class ApiKeyAuthorizationMiddleware : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler DefaultHandler =
        new AuthorizationMiddlewareResultHandler();

    public async Task HandleAsync(
        RequestDelegate requestDelegate,
        HttpContext httpContext,
        AuthorizationPolicy authorizationPolicy,
        PolicyAuthorizationResult policyAuthorizationResult
    )
    {
        if (
            !policyAuthorizationResult.Succeeded
            && authorizationPolicy.Requirements.Any(requirement =>
                requirement is IHasApiKeyRequirement
            )
        )
        {
            throw new UnauthorizedAccessException();
        }

        // Fallback to the default implementation.
        await DefaultHandler.HandleAsync(
            requestDelegate,
            httpContext,
            authorizationPolicy,
            policyAuthorizationResult
        );
    }
}
