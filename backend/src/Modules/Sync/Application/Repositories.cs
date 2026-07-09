namespace Ordevo.Modules.Sync.Application;

public interface ISyncRepository
{
    Task<IReadOnlyList<SyncEntityDto>> ListEntitiesAsync(CancellationToken ct = default);
    Task<DeviceDto?> GetDeviceAsync(string tenantId, string deviceId, CancellationToken ct = default);
    Task<long> GetHighWatermarkAsync(string tenantId, string? branchId, CancellationToken ct = default);
    Task<IReadOnlyList<SyncChangeDto>> PullChangesAsync(string tenantId, string? branchId, long sinceVersion, int take, CancellationToken ct = default);
    Task<IReadOnlyList<PendingMutationDto>> ListPendingMutationsAsync(string tenantId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<SyncConflictDto>> ListOpenConflictsAsync(string tenantId, int take, CancellationToken ct = default);
}

public interface ISyncProcedures
{
    Task<DeviceDto> RegisterDeviceAsync(string tenantId, string? branchId, RegisterDeviceRequest request, bool autoApprove, string userId, CancellationToken ct = default);
    Task HeartbeatAsync(string tenantId, string deviceId, string? branchId, string? localStoreId, string? appVersion, CancellationToken ct = default);
    Task AckPullAsync(string tenantId, string deviceId, string? branchId, long lastPullVersion, CancellationToken ct = default);
    Task<AppendChangeResponse> AppendChangeAsync(string tenantId, AppendChangeRequest request, string? originDeviceId, string userId, CancellationToken ct = default);
    Task<MutationResultDto> StageMutationAsync(string tenantId, string? branchId, string deviceId, ClientMutationRequest request, string userId, CancellationToken ct = default);
}
