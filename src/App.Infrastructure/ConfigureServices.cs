using System;
using System.Data;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Application.SlaRules.Services;
using App.Application.Teams.Services;
using App.Application.TicketViews;
using App.Application.Webhooks.Commands;
using App.Application.Webhooks.Services;
using App.Infrastructure.BackgroundTasks;
using App.Infrastructure.Configurations;
using App.Infrastructure.FileStorage;
using App.Infrastructure.Logging;
using App.Infrastructure.Persistence;
using App.Infrastructure.Persistence.Interceptors;
using App.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        // PostgreSQL only
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                dbConnectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
                    npgsqlOptions.MigrationsAssembly("App.Infrastructure");
                }
            );
        });
        services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(dbConnectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Health checks for PostgreSQL
        services
            .AddHealthChecks()
            .AddNpgSql(dbConnectionString, name: "postgres", timeout: TimeSpan.FromSeconds(5));

        services.AddSingleton<
            ICurrentOrganizationConfiguration,
            CurrentOrganizationConfiguration
        >();
        services.AddSingleton<ISecurityConfiguration, SecurityConfiguration>();
        services.AddScoped<IEmailerConfiguration, EmailerConfiguration>();
        services.AddSingleton<ISnoozeConfiguration, SnoozeConfiguration>();

        services.AddScoped<IEmailer, Emailer>();
        services.AddTransient<IBackgroundTaskDb, BackgroundTaskDb>();
        services.AddTransient<IAppRawDbInfo, AppRawDbInfo>();
        services.AddTransient<IAppRawDbCommands, AppRawDbCommands>();

        //file storage provider
        var fileStorageProvider = configuration[FileStorageUtility.CONFIG_NAME]
            .IfNullOrEmpty(FileStorageUtility.LOCAL)
            .ToLower();
        if (fileStorageProvider == FileStorageUtility.LOCAL)
        {
            services.AddScoped<IFileStorageProvider, LocalFileStorageProvider>();
        }
        else if (fileStorageProvider.ToLower() == FileStorageUtility.AZUREBLOB)
        {
            services.AddScoped<IFileStorageProvider, AzureBlobFileStorageProvider>();
        }
        else if (fileStorageProvider.ToLower() == FileStorageUtility.S3)
        {
            services.AddScoped<IFileStorageProvider, S3FileStorageProvider>();
        }
        else
        {
            throw new NotImplementedException(
                $"Unsupported file storage provider: {fileStorageProvider}"
            );
        }

        for (int i = 0; i < Convert.ToInt32(configuration["NUM_BACKGROUND_WORKERS"] ?? "4"); i++)
        {
            services.AddSingleton<IHostedService, QueuedHostedService>();
        }
        services.AddScoped<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // SLA evaluation background job
        services.AddSingleton<IHostedService, SlaEvaluationJob>();

        // Snooze evaluation background job
        services.AddSingleton<IHostedService, SnoozeEvaluationJob>();

        // Ticket export background task
        services.AddScoped<
            App.Application.Exports.Commands.TicketExportJob,
            TicketExportBackgroundTask
        >();

        // Import background tasks
        services.AddScoped<
            App.Application.Imports.Commands.ContactImportJob,
            ContactImportBackgroundTask
        >();
        services.AddScoped<
            App.Application.Imports.Commands.TicketImportJob,
            TicketImportBackgroundTask
        >();

        // Export cleanup background job
        services.AddSingleton<IHostedService, ExportCleanupJob>();

        // Webhook background tasks and services
        services.AddScoped<WebhookDeliveryJob, WebhookDeliveryBackgroundTask>();
        services.AddSingleton<IHostedService, WebhookLogCleanupJob>();
        services.AddScoped<IUrlValidationService, UrlValidationService>();
        services.AddScoped<IWebhookPayloadBuilder, WebhookPayloadBuilder>();
        services.AddHttpClient("WebhookClient");

        // Ticketing services
        services.AddScoped<IRoundRobinService, RoundRobinService>();
        services.AddScoped<ISlaService, SlaService>();
        services.AddScoped<ITicketConfigService, TicketConfigService>();
        services.AddScoped<INumericIdGenerator, NumericIdGenerator>();
        services.AddScoped<IFavoriteViewsService, FavoriteViewsService>();

        // Cached services for performance - IMemoryCache is singleton, so caching works across requests
        services.AddScoped<ICachedOrganizationService, CachedOrganizationService>();

        // Audit log writers
        RegisterAuditLogWriters(services, configuration);

        return services;
    }

    private static void RegisterAuditLogWriters(IServiceCollection services, IConfiguration configuration)
    {
        var obsOptions = configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        // PostgreSQL writer is always registered (source for in-app UI, always WritesOnly)
        services.AddScoped<IAuditLogWriter, PostgresAuditLogWriter>();

        // Loki writer (optional, uses main Serilog Loki sink)
        // Requires Logging.EnableLoki to be true for logs to actually reach Loki
        var lokiSink = obsOptions.AuditLog.AdditionalSinks.Loki;
        if (lokiSink?.Enabled == true)
        {
            services.AddSingleton<LokiAuditLogWriter>();
            services.AddSingleton<IAuditLogWriter>(sp => sp.GetRequiredService<LokiAuditLogWriter>());
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<LokiAuditLogWriter>());
        }

        // OpenTelemetry writer (optional, configurable mode)
        var otelSink = obsOptions.AuditLog.AdditionalSinks.OpenTelemetry;
        if (otelSink?.Enabled == true)
        {
            services.AddSingleton<IAuditLogWriter, OtelAuditLogWriter>();
        }
    }
}
