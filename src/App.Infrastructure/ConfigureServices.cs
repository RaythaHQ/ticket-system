using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;
using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Infrastructure.BackgroundTasks;
using App.Infrastructure.Configurations;
using App.Infrastructure.FileStorage;
using App.Infrastructure.Persistence;
using App.Infrastructure.Persistence.Interceptors;
using App.Infrastructure.Services;
using App.Application.Teams.Services;
using App.Application.SlaRules.Services;

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
        services.AddScoped<IAppDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>()
        );

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

        // Ticket export background task
        services.AddScoped<App.Application.Exports.Commands.TicketExportJob, TicketExportBackgroundTask>();
        
        // Export cleanup background job
        services.AddSingleton<IHostedService, ExportCleanupJob>();

        // Ticketing services
        services.AddScoped<IRoundRobinService, RoundRobinService>();
        services.AddScoped<ISlaService, SlaService>();
        services.AddScoped<ITicketConfigService, TicketConfigService>();

        return services;
    }
}
