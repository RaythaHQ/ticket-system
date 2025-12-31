using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Infrastructure.Persistence;

/// <summary>
/// Writes audit log entries to PostgreSQL database.
/// Always enabled, always WritesOnly (commands only).
/// Source of truth for the in-app Audit Logs UI.
/// </summary>
public class PostgresAuditLogWriter : IAuditLogWriter
{
    private readonly IAppDbContext _db;

    public PostgresAuditLogWriter(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// PostgreSQL writer always operates in WritesOnly mode.
    /// This is the source of truth for the in-app Audit Logs UI.
    /// </summary>
    public AuditLogMode Mode => AuditLogMode.WritesOnly;

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            Id = entry.Id,
            Category = entry.Category,
            Request = entry.RequestPayload,
            UserEmail = entry.UserEmail ?? string.Empty,
            IpAddress = entry.IpAddress ?? string.Empty,
            EntityId = entry.EntityId.HasValue ? (ShortGuid)entry.EntityId.Value : null,
            CreationTime = entry.Timestamp
        };

        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

