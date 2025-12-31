using System.Diagnostics;
using System.Text.Json;
using CSharpVitamins;
using Mediator;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;

namespace App.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that logs command (write) operations to all registered audit log writers.
/// Only writes to sinks - queries are handled by QueryLoggingBehavior.
/// </summary>
public class AuditBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IEnumerable<IAuditLogWriter> _writers;
    private readonly ICurrentUser _currentUser;

    public AuditBehavior(IEnumerable<IAuditLogWriter> writers, ICurrentUser currentUser)
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
        var stopwatch = Stopwatch.StartNew();
        var response = await next(message, cancellationToken);
        stopwatch.Stop();

        var interfaces = message.GetType().GetInterfaces();
        bool isLoggableRequest = interfaces.Any(p => p == typeof(ILoggableRequest));
        bool isLoggableEntityRequest = interfaces.Any(p => p == typeof(ILoggableEntityRequest));

        if (isLoggableRequest || isLoggableEntityRequest)
        {
            dynamic responseAsDynamic = response as dynamic;
            dynamic messageAsDynamic = message as dynamic;

            if (responseAsDynamic.Success)
            {
                Guid? entityId = null;
                if (isLoggableEntityRequest)
                {
                    ShortGuid shortGuid = messageAsDynamic.Id;
                    entityId = shortGuid.Guid;
                }

                var entry = new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    Category = messageAsDynamic.GetLogName(),
                    RequestType = "Command",
                    RequestPayload = JsonSerializer.Serialize(messageAsDynamic),
                    ResponsePayload = null,
                    Success = true,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    UserEmail = _currentUser.EmailAddress,
                    IpAddress = _currentUser.RemoteIpAddress,
                    EntityId = entityId,
                    Timestamp = DateTime.UtcNow
                };

                // Write to all registered writers
                // Commands go to all writers (regardless of mode)
                var writeTasks = _writers.Select(w => w.WriteAsync(entry, cancellationToken));
                await Task.WhenAll(writeTasks);
            }
        }

        return response;
    }
}
