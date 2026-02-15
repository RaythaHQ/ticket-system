using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Security;
using App.Domain.Entities;
using App.Infrastructure.Persistence;
using App.Web.Authentication;
using App.Web.Middlewares;
using App.Web.Services;
using CSharpVitamins;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddWebUIServices(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration
    )
    {
        // Bind observability options
        services.Configure<ObservabilityOptions>(
            configuration.GetSection(ObservabilityOptions.SectionName)
        );

        var obsOptions =
            configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        // Configure OpenTelemetry tracing/metrics (enabled if OtlpEndpoint is set)
        if (!string.IsNullOrEmpty(obsOptions.OpenTelemetry.OtlpEndpoint))
        {
            ConfigureOpenTelemetry(services, obsOptions, environment);
        }

        // Sentry configuration is done in the host builder via UseSentry()
        // The DSN and environment are configured there

        // In development, use less restrictive cookie settings to allow non-HTTPS access
        // from non-localhost hosts (e.g., http://machinename:8888). SameSite=None requires
        // Secure, and Secure cookies are rejected from non-secure origins except localhost.
        var cookieSecurePolicy = environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        var cookieSameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None;

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.LoginPath = new PathString("/admin/login-redirect");
                    options.Cookie.IsEssential = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = cookieSecurePolicy;
                    options.Cookie.SameSite = cookieSameSite;
                    options.AccessDeniedPath = new PathString("/admin/403");
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.EventsType = typeof(CustomCookieAuthenticationEvents);
                }
            );

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                RaythaClaimTypes.IsAdmin,
                policy => policy.Requirements.Add(new IsAdminRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_USERS_PERMISSION,
                policy => policy.Requirements.Add(new ManageUsersRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION,
                policy => policy.Requirements.Add(new ManageAdministratorsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION,
                policy => policy.Requirements.Add(new ManageTemplatesRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION,
                policy => policy.Requirements.Add(new ManageAuditLogsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                policy => policy.Requirements.Add(new ManageSystemSettingsRequirement())
            );

            // Ticketing system policies
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_TEAMS_PERMISSION,
                policy => policy.Requirements.Add(new ManageTeamsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_TICKETS_PERMISSION,
                policy => policy.Requirements.Add(new ManageTicketsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.ACCESS_REPORTS_PERMISSION,
                policy => policy.Requirements.Add(new AccessReportsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_SYSTEM_VIEWS_PERMISSION,
                policy => policy.Requirements.Add(new ManageSystemViewsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.IMPORT_EXPORT_TICKETS_PERMISSION,
                policy => policy.Requirements.Add(new ImportExportTicketsRequirement())
            );
            // Wiki permissions
            options.AddPolicy(
                BuiltInSystemPermission.EDIT_WIKI_ARTICLES_PERMISSION,
                policy => policy.Requirements.Add(new EditWikiArticlesRequirement())
            );

            options.AddPolicy(
                AppApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin,
                policy => policy.Requirements.Add(new ApiIsAdminRequirement())
            );
            options.AddPolicy(
                AppApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageSystemSettingsRequirement())
            );
            options.AddPolicy(
                AppApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInSystemPermission.MANAGE_USERS_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageUsersRequirement())
            );
            options.AddPolicy(
                AppApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageTemplatesRequirement())
            );
        });

        services.AddScoped<CustomCookieAuthenticationEvents>();
        services
            .AddControllersWithViews(options => { })
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                o.JsonSerializerOptions.WriteIndented = true;
                o.JsonSerializerOptions.Converters.Add(new ShortGuidConverter());
                o.JsonSerializerOptions.Converters.Add(new AuditableUserDtoConverter());
            });
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentOrganization, CurrentOrganization>();
        services.AddScoped<IRelativeUrlBuilder, RelativeUrlBuilder>();
        services.AddScoped<INotificationSuppressionService, NotificationSuppressionService>();
        services.AddScoped<IRenderEngine, RenderEngine>();
        services.AddSingleton<IFileStorageProviderSettings, FileStorageProviderSettings>();
        services.AddSingleton<ICurrentVersion, CurrentVersion>();

        services.AddScoped<IAuthorizationHandler, AppAdminAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, AppApiAuthorizationHandler>();
        services.AddSingleton<
            IAuthorizationMiddlewareResultHandler,
            ApiKeyAuthorizationMiddleware
        >();

        services.AddScoped<ICsvService, CsvService>();

        // SignalR for real-time notifications and activity stream
        services.AddSignalR();
        services.AddScoped<IInAppNotificationService, InAppNotificationService>();
        services.AddScoped<ISidebarBadgeService, SidebarBadgeService>();
        services.AddScoped<IActivityStreamService, ActivityStreamService>();

        services.AddRouting();
        services
            .AddDataProtection()
            .SetApplicationName("App")
            .PersistKeysToDbContext<AppDbContext>();
        services.AddHttpContextAccessor();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
        });
        services.AddRazorPages();
        return services;
    }

    private static void ConfigureOpenTelemetry(
        IServiceCollection services,
        ObservabilityOptions options,
        IWebHostEnvironment environment
    )
    {
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: options.OpenTelemetry.ServiceName,
                serviceVersion: typeof(ConfigureServices).Assembly.GetName().Version?.ToString()
                    ?? "1.0.0"
            )
            .AddAttributes(
                new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.EnvironmentName,
                }
            );

        services
            .AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(options.OpenTelemetry.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(opts =>
                    {
                        opts.SetDbStatementForText = true;
                    });

                if (!string.IsNullOrEmpty(options.OpenTelemetry.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(otlp, options.OpenTelemetry, "traces");
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(options.OpenTelemetry.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(otlp, options.OpenTelemetry, "metrics");
                    });
                }
            });

        // Configure OTEL logging if enabled
        if (
            options.Logging.EnableOpenTelemetry
            && !string.IsNullOrEmpty(options.OpenTelemetry.OtlpEndpoint)
        )
        {
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(otelLogging =>
                {
                    otelLogging.SetResourceBuilder(resourceBuilder);
                    otelLogging.AddOtlpExporter(otlp =>
                    {
                        ConfigureOtlpExporter(otlp, options.OpenTelemetry, "logs");
                    });
                });
            });
        }
    }

    private static void ConfigureOtlpExporter(
        OtlpExporterOptions otlp,
        OpenTelemetryOptions options,
        string signalType
    )
    {
        // Build the full endpoint with signal path for OpenObserve compatibility
        // OpenObserve expects: /api/{org}/v1/{signal}
        var baseEndpoint = options.OtlpEndpoint!.TrimEnd('/');
        var fullEndpoint = $"{baseEndpoint}/v1/{signalType}";
        otlp.Endpoint = new Uri(fullEndpoint);

        // Use gRPC or HTTP/protobuf protocol based on configuration
        otlp.Protocol = options.UseGrpc ? OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf;

        // Build headers for OTLP exporter
        var headers = new List<string>();

        // Add authorization header if configured
        if (!string.IsNullOrEmpty(options.Authorization))
        {
            headers.Add($"Authorization={options.Authorization}");
        }

        // Add organization header for OpenObserve
        if (!string.IsNullOrEmpty(options.Organization))
        {
            headers.Add($"organization={options.Organization}");
        }

        // Add stream-name header for OpenObserve
        if (!string.IsNullOrEmpty(options.StreamName))
        {
            headers.Add($"stream-name={options.StreamName}");
        }

        if (headers.Count > 0)
        {
            otlp.Headers = string.Join(",", headers);
        }
    }
}
