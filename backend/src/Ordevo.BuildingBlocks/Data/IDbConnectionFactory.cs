using System.Data.Common;

namespace Ordevo.BuildingBlocks.Data;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
