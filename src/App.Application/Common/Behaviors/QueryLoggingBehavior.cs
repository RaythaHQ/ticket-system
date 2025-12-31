using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Mediator;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;

namespace App.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that logs query (read) operations to audit log writers with Mode = All.
/// PostgreSQL (always WritesOnly) will NOT receive these entries.
/// </summary>
public class QueryLoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IEnumerable<IAuditLogWriter> _writers;
    private readonly ICurrentUser _currentUser;

    public QueryLoggingBehavior(IEnumerable<IAuditLogWriter> writers, ICurrentUser currentUser)
    {
        _writers = writers;
        _currentUser = currentUser;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Check if this is a loggable query
        var interfaces = message.GetType().GetInterfaces();
        bool isLoggableQuery = interfaces.Any(p => p == typeof(ILoggableQuery));

        if (!isLoggableQuery)
        {
            return await next(message, cancellationToken);
        }

        // Get writers that accept queries (Mode = All)
        var queryWriters = _writers.Where(w => w.Mode == AuditLogMode.All).ToList();

        // Skip entirely if no writers accept queries
        if (queryWriters.Count == 0)
        {
            return await next(message, cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await next(message, cancellationToken);
        stopwatch.Stop();

        try
        {
            dynamic messageAsDynamic = message as dynamic;
            dynamic? responseAsDynamic = response as dynamic;

            bool success = true;
            string? responsePayload = null;

            // Check if we should log the full result
            var logResultAttr = message.GetType().GetCustomAttribute<LogQueryResultAttribute>();
            if (logResultAttr?.LogFullResult == true && responseAsDynamic != null)
            {
                try
                {
                    responsePayload = JsonSerializer.Serialize(responseAsDynamic);
                }
                catch
                {
                    // If serialization fails, just log without response
                    responsePayload = "[Serialization failed]";
                }
            }

            // For queries, success is determined by whether we got a response without throwing
            // If the response has a Success property, use that
            try
            {
                success = responseAsDynamic?.Success ?? true;
            }
            catch
            {
                // Response doesn't have Success property, assume success
                success = true;
            }

            var entry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                Category = messageAsDynamic.GetLogName(),
                RequestType = "Query",
                RequestPayload = JsonSerializer.Serialize(messageAsDynamic),
                ResponsePayload = responsePayload,
                Success = success,
                DurationMs = stopwatch.ElapsedMilliseconds,
                UserEmail = _currentUser.EmailAddress,
                IpAddress = _currentUser.RemoteIpAddress,
                EntityId = null,
                Timestamp = DateTime.UtcNow
            };

            // Write to query-accepting writers only
            var writeTasks = queryWriters.Select(w => w.WriteAsync(entry, cancellationToken));
            await Task.WhenAll(writeTasks);
        }
        catch
        {
            // Don't let audit logging failures affect the query response
        }

        return response;
    }
}

