//Force build and push
using App.Application.Common.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Sinks.OpenTelemetry;

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
                        // Suppress verbose HTTP client logs (OTLP exporter traffic)
                        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
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

                    // OpenTelemetry sink (optional)
                    if (
                        obsOptions.Logging.EnableOpenTelemetry
                        && !string.IsNullOrEmpty(obsOptions.OpenTelemetry.OtlpEndpoint)
                    )
                    {
                        var otelEndpoint = obsOptions.OpenTelemetry.OtlpEndpoint.TrimEnd('/');
                        var logsEndpoint = $"{otelEndpoint}/v1/logs";

                        configuration.WriteTo.OpenTelemetry(options =>
                        {
                            options.Endpoint = logsEndpoint;
                            options.Protocol = OtlpProtocol.HttpProtobuf;

                            if (!string.IsNullOrEmpty(obsOptions.OpenTelemetry.Authorization))
                            {
                                options.Headers = new Dictionary<string, string>
                                {
                                    ["Authorization"] = obsOptions.OpenTelemetry.Authorization,
                                };
                            }

                            if (!string.IsNullOrEmpty(obsOptions.OpenTelemetry.StreamName))
                            {
                                options.Headers ??= new Dictionary<string, string>();
                                options.Headers["stream-name"] = obsOptions
                                    .OpenTelemetry
                                    .StreamName;
                            }

                            if (!string.IsNullOrEmpty(obsOptions.OpenTelemetry.Organization))
                            {
                                options.Headers ??= new Dictionary<string, string>();
                                options.Headers["organization"] = obsOptions
                                    .OpenTelemetry
                                    .Organization;
                            }

                            options.ResourceAttributes = new Dictionary<string, object>
                            {
                                ["service.name"] = obsOptions.OpenTelemetry.ServiceName,
                            };
                        });

                        Log.Information(
                            "OpenTelemetry logging enabled at {Endpoint}",
                            logsEndpoint
                        );
                    }
                }
            )
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // Read config to check if Sentry should be enabled
                var env =
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{env}.json", optional: true)
                    .AddUserSecrets<Program>(optional: true) // Include User Secrets for local dev
                    .AddEnvironmentVariables()
                    .Build();

                var obsOptions =
                    configuration
                        .GetSection(ObservabilityOptions.SectionName)
                        .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

                // Only configure Sentry if DSN is provided
                if (!string.IsNullOrEmpty(obsOptions.Sentry.Dsn))
                {
                    Console.WriteLine($"[Sentry] Initializing with DSN: {obsOptions.Sentry.Dsn}");
                    Console.WriteLine($"[Sentry] Environment: {obsOptions.Sentry.Environment}");
                    webBuilder.UseSentry(options =>
                    {
                        options.Dsn = obsOptions.Sentry.Dsn;
                        options.Environment = obsOptions.Sentry.Environment;
                        options.TracesSampleRate = 1.0;
                        options.Debug = true; // Enable Sentry debug mode
                    });
                }
                else
                {
                    Console.WriteLine("[Sentry] DSN not configured, Sentry disabled");
                }

                webBuilder.UseStartup<Startup>();
            });
}
