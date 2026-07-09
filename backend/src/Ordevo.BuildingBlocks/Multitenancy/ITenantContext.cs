namespace Ordevo.BuildingBlocks.Multitenancy;

public interface ITenantContext
{
    string? TenantId { get; }
    string? BranchId { get; }
    string? UserId { get; }
    string? DeviceId { get; }
    bool IsAuthenticated { get; }

    string RequireTenantId();
}
