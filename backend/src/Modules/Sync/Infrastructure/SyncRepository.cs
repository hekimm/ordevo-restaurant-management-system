using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Sync.Application;

namespace Ordevo.Modules.Sync.Infrastructure;

public sealed class SyncRepository(IDbConnectionFactory factory) : ISyncRepository
{
    public async Task<IReadOnlyList<SyncEntityDto>> ListEntitiesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<SyncEntityDto>(
            """
            SELECT ENTITY_NAME AS EntityName, TABLE_NAME AS TableName,
                   IS_BRANCH_SCOPED AS IsBranchScoped, ALLOW_CLIENT_PUSH AS AllowClientPush,
                   IS_ACTIVE AS IsActive, SORT_ORDER AS SortOrder
            FROM SYNC_ENTITY_CONFIG
            ORDER BY SORT_ORDER, ENTITY_NAME
            """);
        return rows.AsList();
    }

    public async Task<DeviceDto?> GetDeviceAsync(string tenantId, string deviceId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<DeviceDto>(
            """
            SELECT ID, BRANCH_ID AS BranchId, NAME, DEVICE_TYPE AS DeviceType, FINGERPRINT,
                   IS_APPROVED AS IsApproved, LAST_SEEN_AT AS LastSeenAt
            FROM DEVICES
            WHERE TENANT_ID = :tenantId AND ID = :deviceId
            """,
            new OracleParams(new { tenantId, deviceId }));
    }

    public async Task<long> GetHighWatermarkAsync(string tenantId, string? branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<long>(
            """
            SELECT NVL(MAX(CHANGE_VERSION),0)
            FROM SYNC_OUTBOX
            WHERE TENANT_ID = :tenantId
              AND (:branchId IS NULL OR BRANCH_ID IS NULL OR BRANCH_ID = :branchId)
            """,
            new OracleParams(new { tenantId, branchId }));
    }

    public async Task<IReadOnlyList<SyncChangeDto>> PullChangesAsync(
        string tenantId, string? branchId, long sinceVersion, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<SyncChangeDto>(
            """
            SELECT CHANGE_VERSION AS ChangeVersion, ID, BRANCH_ID AS BranchId,
                   ENTITY_NAME AS EntityName, ENTITY_ID AS EntityId, OPERATION AS Operation,
                   ROW_VERSION AS RowVersion, PAYLOAD AS Payload, ORIGIN_DEVICE_ID AS OriginDeviceId,
                   ORIGIN_USER_ID AS OriginUserId, OCCURRED_AT AS OccurredAt
            FROM SYNC_OUTBOX
            WHERE TENANT_ID = :tenantId
              AND CHANGE_VERSION > :sinceVersion
              AND (:branchId IS NULL OR BRANCH_ID IS NULL OR BRANCH_ID = :branchId)
            ORDER BY CHANGE_VERSION
            FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, branchId, sinceVersion, take }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<PendingMutationDto>> ListPendingMutationsAsync(
        string tenantId, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<PendingMutationRow>(
            """
            SELECT ID, BRANCH_ID AS BranchId, DEVICE_ID AS DeviceId, CLIENT_MUTATION_ID AS ClientMutationId,
                   ENTITY_NAME AS EntityName, ENTITY_ID AS EntityId, OPERATION AS Operation,
                   BASE_CHANGE_VERSION AS BaseChangeVersion, EXPECTED_ROW_VERSION AS ExpectedRowVersion,
                   PAYLOAD AS Payload, STATUS AS Status, ERROR_CODE AS ErrorCode,
                   ERROR_MESSAGE AS ErrorMessage, CREATED_AT AS CreatedAt
            FROM SYNC_CLIENT_MUTATIONS
            WHERE TENANT_ID = :tenantId AND STATUS = 'pending'
            ORDER BY CREATED_AT
            FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, take }));
        return rows.Select(x => new PendingMutationDto(
            x.Id, x.BranchId, x.DeviceId, x.ClientMutationId, x.EntityName, x.EntityId, x.Operation,
            ToLong(x.BaseChangeVersion), ToLong(x.ExpectedRowVersion), x.Payload, x.Status,
            x.ErrorCode, x.ErrorMessage, x.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<SyncConflictDto>> ListOpenConflictsAsync(
        string tenantId, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<SyncConflictRow>(
            """
            SELECT ID, BRANCH_ID AS BranchId, DEVICE_ID AS DeviceId, MUTATION_ID AS MutationId,
                   ENTITY_NAME AS EntityName, ENTITY_ID AS EntityId,
                   SERVER_CHANGE_VERSION AS ServerChangeVersion, CLIENT_PAYLOAD AS ClientPayload,
                   SERVER_PAYLOAD AS ServerPayload, RESOLUTION_STATUS AS ResolutionStatus,
                   CREATED_AT AS CreatedAt
            FROM SYNC_CONFLICTS
            WHERE TENANT_ID = :tenantId AND RESOLUTION_STATUS = 'open'
            ORDER BY CREATED_AT
            FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, take }));
        return rows.Select(x => new SyncConflictDto(
            x.Id, x.BranchId, x.DeviceId, x.MutationId, x.EntityName, x.EntityId,
            ToLong(x.ServerChangeVersion), x.ClientPayload, x.ServerPayload, x.ResolutionStatus,
            x.CreatedAt)).ToList();
    }

    private static long? ToLong(decimal? value) => value.HasValue ? decimal.ToInt64(value.Value) : null;

    private sealed class PendingMutationRow
    {
        public string Id { get; set; } = default!;
        public string? BranchId { get; set; }
        public string DeviceId { get; set; } = default!;
        public string ClientMutationId { get; set; } = default!;
        public string EntityName { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public string Operation { get; set; } = default!;
        public decimal? BaseChangeVersion { get; set; }
        public decimal? ExpectedRowVersion { get; set; }
        public string? Payload { get; set; }
        public string Status { get; set; } = default!;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    private sealed class SyncConflictRow
    {
        public string Id { get; set; } = default!;
        public string? BranchId { get; set; }
        public string DeviceId { get; set; } = default!;
        public string MutationId { get; set; } = default!;
        public string EntityName { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public decimal? ServerChangeVersion { get; set; }
        public string? ClientPayload { get; set; }
        public string? ServerPayload { get; set; }
        public string ResolutionStatus { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
