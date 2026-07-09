namespace Ordevo.Modules.Sync.Application;

public sealed record SyncEntityDto(
    string EntityName, string TableName, bool IsBranchScoped, bool AllowClientPush, bool IsActive, int SortOrder);

public sealed record DeviceDto(
    string Id, string? BranchId, string Name, string DeviceType, string Fingerprint,
    bool IsApproved, DateTimeOffset? LastSeenAt);

public sealed record RegisterDeviceRequest(
    string Name, string DeviceType, string Fingerprint, string? BranchId = null);

public sealed record HeartbeatRequest(
    string? DeviceId, string? LocalStoreId, string? AppVersion);

public sealed record SyncChangeDto(
    long ChangeVersion, string Id, string? BranchId, string EntityName, string EntityId,
    string Operation, long? RowVersion, string? Payload, string? OriginDeviceId,
    string? OriginUserId, DateTimeOffset OccurredAt);

public sealed record PullChangesResponse(
    long HighWatermark, DateTimeOffset ServerTime, bool HasMore, IReadOnlyList<SyncChangeDto> Changes);

public sealed record AckPullRequest(string? DeviceId, long LastPullVersion);

public sealed record ClientMutationRequest(
    string ClientMutationId, string EntityName, string EntityId, string Operation,
    long? BaseChangeVersion, long? ExpectedRowVersion, string? Payload);

public sealed record PushChangesRequest(string? DeviceId, IReadOnlyList<ClientMutationRequest> Mutations);

public sealed record MutationResultDto(
    string ClientMutationId, string MutationId, string Status);

public sealed record PushChangesResponse(
    long HighWatermark, IReadOnlyList<MutationResultDto> Results);

public sealed record AppendChangeRequest(
    string? BranchId, string EntityName, string EntityId, string Operation,
    long? RowVersion, string? Payload);

public sealed record AppendChangeResponse(long ChangeVersion);

public sealed record PendingMutationDto(
    string Id, string? BranchId, string DeviceId, string ClientMutationId, string EntityName,
    string EntityId, string Operation, long? BaseChangeVersion, long? ExpectedRowVersion,
    string? Payload, string Status, string? ErrorCode, string? ErrorMessage, DateTimeOffset CreatedAt);

public sealed record SyncConflictDto(
    string Id, string? BranchId, string DeviceId, string MutationId, string EntityName,
    string EntityId, long? ServerChangeVersion, string? ClientPayload, string? ServerPayload,
    string ResolutionStatus, DateTimeOffset CreatedAt);
