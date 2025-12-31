using Mediator;

namespace App.Application.Common.Models;

/// <summary>
/// Marker interface for queries that should be logged to audit sinks.
/// Only sinks with Mode = All will receive these entries.
/// </summary>
public interface ILoggableQuery { }

/// <summary>
/// Interface for controlling result logging behavior.
/// </summary>
public interface ILoggableQueryWithResult
{
    bool LogFullResult { get; }
}

/// <summary>
/// Base class for queries that should be logged to audit sinks with Mode = All.
/// PostgreSQL audit logs (source for in-app UI) will NOT receive these entries.
/// </summary>
/// <typeparam name="T">The response type of the query.</typeparam>
public abstract record LoggableQuery<T> : IRequest<T>, ILoggableQuery
{
    /// <summary>
    /// Gets the log name for this query, used as the Category in audit entries.
    /// </summary>
    public virtual string GetLogName()
    {
        return this.GetType()
            .FullName!.Replace("App.Application.", string.Empty)
            .Replace("+Query", string.Empty);
    }
}

/// <summary>
/// Attribute to control per-query result logging behavior.
/// Apply to query classes that should log their full response.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class LogQueryResultAttribute : Attribute
{
    /// <summary>
    /// If true, the full response will be serialized and logged.
    /// Default is false (only metadata is logged).
    /// </summary>
    public bool LogFullResult { get; set; } = false;
}

