//Force build and push
using App.Application.Common.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace App.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog(
                (context, services, configuration) =>
                {
                    var obsOptions =
                        context
                            .Configuration.GetSection(ObservabilityOptions.SectionName)
                            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

                    configuration
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Override(
                            "Microsoft.Hosting.Lifetime",
                            LogEventLevel.Information
                        )
                        .MinimumLevel.Override(
                            "Microsoft.EntityFrameworkCore",
                            LogEventLevel.Warning
                        )
                        .Enrich.FromLogContext()
                        .Enrich.WithEnvironmentName();

                    // Console sink (always enabled by default, configurable)
                    if (obsOptions.Logging.EnableConsole)
                    {
                        configuration.WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                        );
                    }

                    // Loki sink (optional)
                    if (
                        obsOptions.Logging.EnableLoki
                        && !string.IsNullOrEmpty(obsOptions.Logging.LokiUrl)
                    )
                    {
                        configuration.WriteTo.GrafanaLoki(
                            obsOptions.Logging.LokiUrl,
                            labels: new List<LokiLabel>
                            {
                                new() { Key = "app", Value = obsOptions.OpenTelemetry.ServiceName },
                                new()
                                {
                                    Key = "env",
                                    Value = context.HostingEnvironment.EnvironmentName,
                                },
                            },
                            restrictedToMinimumLevel: LogEventLevel.Information
                        );

                        Log.Information(
                            "Loki logging enabled at {LokiUrl}",
                            obsOptions.Logging.LokiUrl
                        );
                    }

                    // OpenTelemetry logging sink is configured via OTEL SDK in ConfigureServices
                }
            )
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // Read config to check if Sentry should be enabled
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile(
                        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                        optional: true
                    )
                    .AddEnvironmentVariables()
                    .Build();

                var obsOptions =
                    configuration
                        .GetSection(ObservabilityOptions.SectionName)
                        .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

                // Only configure Sentry if DSN is provided
                if (!string.IsNullOrEmpty(obsOptions.Sentry.Dsn))
                {
                    webBuilder.UseSentry(options =>
                    {
                        options.Dsn = obsOptions.Sentry.Dsn;
                        options.Environment = obsOptions.Sentry.Environment;
                        options.TracesSampleRate = 1.0;
                    });
                }

                webBuilder.UseStartup<Startup>();
            });
}
