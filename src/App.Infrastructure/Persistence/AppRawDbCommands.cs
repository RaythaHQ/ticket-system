using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using App.Application.Common.Interfaces;

namespace App.Infrastructure.Persistence;

/// <summary>
/// Executes raw database commands that require database-specific SQL.
/// </summary>
public class AppRawDbCommands : IAppRawDbCommands
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _configuration;

    public AppRawDbCommands(IDbConnection db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task ClearAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        // PostgreSQL only
        string query = "TRUNCATE TABLE \"AuditLogs\"";
        await _db.ExecuteAsync(query);
    }
}

