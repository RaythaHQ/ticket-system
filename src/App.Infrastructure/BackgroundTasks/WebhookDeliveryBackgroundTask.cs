using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Webhooks.Commands;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace App.Infrastructure.BackgroundTasks;

/// <summary>
/// Background task that delivers webhook payloads with retry logic using Polly.
/// Implements exponential backoff: 2s, 4s, 8s delays between retries.
/// </summary>
public class WebhookDeliveryBackgroundTask : WebhookDeliveryJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookDeliveryBackgroundTask> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // Retry policy: 3 attempts with exponential backoff (2s, 4s, 8s)
    private static readonly ResiliencePipeline<HttpResponseMessage> RetryPipeline =
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(
                new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 2, // 2 retries = 3 total attempts
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(r => !r.IsSuccessStatusCode && (int)r.StatusCode >= 500),
                }
            )
            .Build();

    public WebhookDeliveryBackgroundTask(
        IServiceProvider serviceProvider,
        ILogger<WebhookDeliveryBackgroundTask> logger,
        IHttpClientFactory httpClientFactory
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public override async Task Execute(
        Guid jobId,
        JsonElement args,
        CancellationToken cancellationToken
    )
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // Parse arguments
        WebhookDeliveryArgs deliveryArgs;
        try
        {
            deliveryArgs =
                args.Deserialize<WebhookDeliveryArgs>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                )
                ?? throw new InvalidOperationException(
                    "Failed to deserialize webhook delivery args"
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to parse webhook delivery arguments for job {JobId}",
                jobId
            );
            return;
        }

        // Create log entry
        var webhookLog = new WebhookLog
        {
            Id = Guid.NewGuid(),
            WebhookId = deliveryArgs.WebhookId,
            TicketId = deliveryArgs.TicketId,
            TriggerType = deliveryArgs.TriggerType,
            PayloadJson = deliveryArgs.PayloadJson,
            AttemptCount = 0,
            Success = false,
            CreatedAt = DateTime.UtcNow,
        };

        db.WebhookLogs.Add(webhookLog);
        await db.SaveChangesAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        var attemptCount = 0;
        HttpResponseMessage? lastResponse = null;
        Exception? lastException = null;

        try
        {
            var httpClient = _httpClientFactory.CreateClient("WebhookClient");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var content = new StringContent(
                deliveryArgs.PayloadJson,
                Encoding.UTF8,
                "application/json"
            );

            // Execute with retry policy
            var context = ResilienceContextPool.Shared.Get(cancellationToken);
            try
            {
                lastResponse = await RetryPipeline.ExecuteAsync(
                    async (ctx) =>
                    {
                        attemptCount++;
                        _logger.LogInformation(
                            "Webhook delivery attempt {Attempt} for webhook {WebhookId} to {Url}",
                            attemptCount,
                            deliveryArgs.WebhookId,
                            deliveryArgs.Url
                        );

                        var request = new HttpRequestMessage(HttpMethod.Post, deliveryArgs.Url)
                        {
                            Content = new StringContent(
                                deliveryArgs.PayloadJson,
                                Encoding.UTF8,
                                "application/json"
                            ),
                        };
                        request.Headers.Add("User-Agent", "TicketSystem-Webhook/1.0");
                        request.Headers.Add("X-Webhook-Event", deliveryArgs.TriggerType);

                        return await httpClient.SendAsync(request, ctx.CancellationToken);
                    },
                    context
                );
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }

            stopwatch.Stop();

            // Read response body (first 1KB)
            string? responseBody = null;
            if (lastResponse.Content != null)
            {
                try
                {
                    var fullBody = await lastResponse.Content.ReadAsStringAsync(cancellationToken);
                    responseBody = fullBody.Length > 1024 ? fullBody[..1024] : fullBody;
                }
                catch
                {
                    // Ignore errors reading response body
                }
            }

            // Update log entry
            webhookLog.AttemptCount = attemptCount;
            webhookLog.Success = lastResponse.IsSuccessStatusCode;
            webhookLog.HttpStatusCode = (int)lastResponse.StatusCode;
            webhookLog.ResponseBody = responseBody;
            webhookLog.CompletedAt = DateTime.UtcNow;
            webhookLog.Duration = stopwatch.Elapsed;

            if (lastResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook delivered successfully to {Url} after {Attempts} attempt(s). Status: {StatusCode}",
                    deliveryArgs.Url,
                    attemptCount,
                    (int)lastResponse.StatusCode
                );
            }
            else
            {
                _logger.LogWarning(
                    "Webhook delivery failed to {Url} after {Attempts} attempt(s). Status: {StatusCode}",
                    deliveryArgs.Url,
                    attemptCount,
                    (int)lastResponse.StatusCode
                );
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            lastException = ex;

            _logger.LogError(
                ex,
                "Webhook delivery failed to {Url} after {Attempts} attempt(s)",
                deliveryArgs.Url,
                attemptCount
            );

            webhookLog.AttemptCount = attemptCount;
            webhookLog.Success = false;
            webhookLog.ErrorMessage = TruncateMessage(ex.Message, 2000);
            webhookLog.CompletedAt = DateTime.UtcNow;
            webhookLog.Duration = stopwatch.Elapsed;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string TruncateMessage(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        return message.Length <= maxLength ? message : message[..maxLength];
    }
}
