using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using App.Application.Common.Interfaces;

namespace App.Infrastructure.Persistence;

/// <summary>
/// Retrieves raw database information.
/// </summary>
public class AppRawDbInfo : IAppRawDbInfo
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _configuration;

    public AppRawDbInfo(IDbConnection db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public DbSpaceUsed GetDatabaseSize()
    {
        // PostgreSQL only
        string query =
            "SELECT pg_size_pretty(pg_database_size(current_database())) AS reserved FROM pg_class LIMIT 1;";
        DbSpaceUsed dbSizeInfo = _db.QueryFirst<DbSpaceUsed>(query);
        return dbSizeInfo;
    }
}
