using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

internal sealed class ApiKeySecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["ApiKey"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Scheme = "X-API-KEY", // "bearer" refers to the header name here
                In = ParameterLocation.Header,
                BearerFormat = "GUID",
                Name = "X-API-KEY",
            },
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = securitySchemes;

        // Apply it as a requirement for all operations
        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
        {
            operation.Value.Security ??= [];
            operation.Value.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("ApiKey", document)] = [],
                }
            );

            // Add X-API-SuppressNotifications header parameter to all operations
            operation.Value.Parameters ??= [];
            var suppressNotificationsParam = new OpenApiParameter
            {
                Name = "X-API-SuppressNotifications",
                In = ParameterLocation.Header,
                Required = false,
                Description =
                    "Set to 'true', '1', or 'yes' (case-insensitive) to suppress email and in-app notifications for this operation. "
                    + "Useful for API automation to prevent notification spam. "
                    + "When set, no notifications will be sent for ticket creation, updates, assignments, status changes, comments, or SLA events.",
            };
            suppressNotificationsParam.Schema = new OpenApiSchema();
            operation.Value.Parameters.Add(suppressNotificationsParam);
        }
    }
}
