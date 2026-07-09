using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

namespace Ordevo.BuildingBlocks.Data;

public sealed class OracleConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public OracleConnectionFactory(string connectionString)
        => _connectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString));

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }
}
