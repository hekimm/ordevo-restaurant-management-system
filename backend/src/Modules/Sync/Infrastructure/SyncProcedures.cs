using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Sync.Application;

namespace Ordevo.Modules.Sync.Infrastructure;

public sealed class SyncProcedures(IDbConnectionFactory factory) : ISyncProcedures
{
    public async Task<DeviceDto> RegisterDeviceAsync(
        string tenantId, string? branchId, RegisterDeviceRequest request, bool autoApprove, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_name", request.Name);
        p.Add("p_device_type", request.DeviceType);
        p.Add("p_fingerprint", request.Fingerprint);
        p.Add("p_auto_approve", autoApprove ? 1 : 0);
        p.Add("p_user_id", userId);
        p.Add("p_device_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        p.Add("p_is_approved", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await db.ExecuteAsync("PKG_SYNC.REGISTER_DEVICE", p, commandType: CommandType.StoredProcedure);

        var deviceId = p.Get<string>("p_device_id");
        return await db.QuerySingleAsync<DeviceDto>(
            """
            SELECT ID, BRANCH_ID AS BranchId, NAME, DEVICE_TYPE AS DeviceType, FINGERPRINT,
                   IS_APPROVED AS IsApproved, LAST_SEEN_AT AS LastSeenAt
            FROM DEVICES
            WHERE TENANT_ID = :tenantId AND ID = :deviceId
            """,
            new OracleParams(new { tenantId, deviceId }));
    }

    public Task HeartbeatAsync(
        string tenantId, string deviceId, string? branchId, string? localStoreId, string? appVersion, CancellationToken ct = default)
        => ExecAsync("PKG_SYNC.HEARTBEAT", ct,
            ("p_tenant_id", tenantId), ("p_device_id", deviceId), ("p_branch_id", branchId),
            ("p_local_store_id", localStoreId), ("p_app_version", appVersion));

    public Task AckPullAsync(
        string tenantId, string deviceId, string? branchId, long lastPullVersion, CancellationToken ct = default)
        => ExecAsync("PKG_SYNC.ACK_PULL", ct,
            ("p_tenant_id", tenantId), ("p_device_id", deviceId), ("p_branch_id", branchId),
            ("p_last_pull_version", lastPullVersion));

    public async Task<AppendChangeResponse> AppendChangeAsync(
        string tenantId, AppendChangeRequest request, string? originDeviceId, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", request.BranchId);
        p.Add("p_entity_name", request.EntityName);
        p.Add("p_entity_id", request.EntityId);
        p.Add("p_operation", request.Operation);
        p.Add("p_row_version", request.RowVersion);
        p.Add("p_payload", request.Payload);
        p.Add("p_origin_device_id", originDeviceId);
        p.Add("p_origin_user_id", userId);
        p.Add("p_change_version", dbType: DbType.Int64, direction: ParameterDirection.Output);
        await db.ExecuteAsync("PKG_SYNC.APPEND_CHANGE", p, commandType: CommandType.StoredProcedure);
        return new AppendChangeResponse(p.Get<long>("p_change_version"));
    }

    public async Task<MutationResultDto> StageMutationAsync(
        string tenantId, string? branchId, string deviceId, ClientMutationRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_device_id", deviceId);
        p.Add("p_client_mutation_id", request.ClientMutationId);
        p.Add("p_entity_name", request.EntityName);
        p.Add("p_entity_id", request.EntityId);
        p.Add("p_operation", request.Operation);
        p.Add("p_base_change_version", request.BaseChangeVersion);
        p.Add("p_expected_row_version", request.ExpectedRowVersion);
        p.Add("p_payload", request.Payload);
        p.Add("p_user_id", userId);
        p.Add("p_mutation_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        p.Add("p_status", dbType: DbType.String, direction: ParameterDirection.Output, size: 20);
        await db.ExecuteAsync("PKG_SYNC.STAGE_MUTATION", p, commandType: CommandType.StoredProcedure);
        return new MutationResultDto(request.ClientMutationId, p.Get<string>("p_mutation_id"), p.Get<string>("p_status"));
    }

    private async Task ExecAsync(string procName, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        foreach (var (name, value) in args)
            p.Add(name, value);
        await db.ExecuteAsync(procName, p, commandType: CommandType.StoredProcedure);
    }
}
