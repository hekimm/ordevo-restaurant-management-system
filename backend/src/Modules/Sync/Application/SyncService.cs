using Oracle.ManagedDataAccess.Client;
using Ordevo.BuildingBlocks.Results;

namespace Ordevo.Modules.Sync.Application;

public sealed class SyncService(ISyncRepository repo, ISyncProcedures procs)
{
    public Task<IReadOnlyList<SyncEntityDto>> ListEntitiesAsync(CancellationToken ct = default)
        => repo.ListEntitiesAsync(ct);

    public async Task<Result<DeviceDto>> RegisterDeviceAsync(
        string tenantId, string? fallbackBranchId, RegisterDeviceRequest request, bool autoApprove, string userId, CancellationToken ct = default)
    {
        try
        {
            return await procs.RegisterDeviceAsync(tenantId, request.BranchId ?? fallbackBranchId, request, autoApprove, userId, ct);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> HeartbeatAsync(
        string tenantId, string? fallbackBranchId, string? claimDeviceId, HeartbeatRequest request, CancellationToken ct = default)
    {
        var deviceId = ResolveDeviceId(claimDeviceId, request.DeviceId);
        if (deviceId is null)
            return Error.Validation("sync.device.required", "Sync için cihaz kimliği gerekli.");

        try
        {
            await procs.HeartbeatAsync(tenantId, deviceId, fallbackBranchId, request.LocalStoreId, request.AppVersion, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<PullChangesResponse> PullAsync(
        string tenantId, string? branchId, long sinceVersion, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var changes = await repo.PullChangesAsync(tenantId, branchId, Math.Max(0, sinceVersion), take, ct);
        var highWatermark = await repo.GetHighWatermarkAsync(tenantId, branchId, ct);
        return new PullChangesResponse(highWatermark, DateTimeOffset.UtcNow, changes.Count == take, changes);
    }

    public async Task<Result> AckPullAsync(
        string tenantId, string? branchId, string? claimDeviceId, AckPullRequest request, CancellationToken ct = default)
    {
        var deviceId = ResolveDeviceId(claimDeviceId, request.DeviceId);
        if (deviceId is null)
            return Error.Validation("sync.device.required", "Sync için cihaz kimliği gerekli.");

        try
        {
            await procs.AckPullAsync(tenantId, deviceId, branchId, request.LastPullVersion, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<PushChangesResponse>> PushAsync(
        string tenantId, string? branchId, string? claimDeviceId, PushChangesRequest request, string userId, CancellationToken ct = default)
    {
        var deviceId = ResolveDeviceId(claimDeviceId, request.DeviceId);
        if (deviceId is null)
            return Error.Validation("sync.device.required", "Sync için cihaz kimliği gerekli.");

        try
        {
            var results = new List<MutationResultDto>(request.Mutations.Count);
            foreach (var mutation in request.Mutations)
                results.Add(await procs.StageMutationAsync(tenantId, branchId, deviceId, mutation, userId, ct));

            var highWatermark = await repo.GetHighWatermarkAsync(tenantId, branchId, ct);
            return new PushChangesResponse(highWatermark, results);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<AppendChangeResponse>> AppendChangeAsync(
        string tenantId, string? claimDeviceId, AppendChangeRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            return await procs.AppendChangeAsync(tenantId, request, claimDeviceId, userId, ct);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<PendingMutationDto>> ListPendingMutationsAsync(string tenantId, int take, CancellationToken ct = default)
        => repo.ListPendingMutationsAsync(tenantId, Math.Clamp(take, 1, 500), ct);

    public Task<IReadOnlyList<SyncConflictDto>> ListOpenConflictsAsync(string tenantId, int take, CancellationToken ct = default)
        => repo.ListOpenConflictsAsync(tenantId, Math.Clamp(take, 1, 500), ct);

    private static string? ResolveDeviceId(string? claimDeviceId, string? requestDeviceId)
        => !string.IsNullOrWhiteSpace(claimDeviceId) ? claimDeviceId
         : !string.IsNullOrWhiteSpace(requestDeviceId) ? requestDeviceId
         : null;

    private static bool TryMapOracle(OracleException ex, out Error error)
    {
        if (ex.Number is >= 20401 and <= 20420)
        {
            error = ex.Number switch
            {
                20401 => Error.Forbidden("sync.device.not_approved", "Cihaz kayıtlı veya onaylı değil."),
                20402 => Error.Validation("sync.entity.push_disabled", "Bu varlık istemci push için açık değil."),
                20403 => Error.Validation("sync.entity.unknown", "Sync varlığı bilinmiyor."),
                20404 => Error.Validation("sync.operation.invalid", "Sync operasyonu geçersiz."),
                20405 => Error.NotFound("sync.mutation.not_found", "Sync mutasyonu bulunamadı."),
                _ => Error.Validation("sync.rule", CleanMessage(ex))
            };
            return true;
        }

        error = Error.Failure("sync.db", CleanMessage(ex));
        return false;
    }

    private static string CleanMessage(OracleException ex)
    {
        var first = ex.Message.Split('\n')[0];
        return first.Replace($"ORA-{ex.Number}:", "", StringComparison.OrdinalIgnoreCase).Trim();
    }
}
