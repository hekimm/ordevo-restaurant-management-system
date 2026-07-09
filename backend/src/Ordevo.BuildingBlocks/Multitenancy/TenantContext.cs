using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Ordevo.BuildingBlocks.Multitenancy;

public sealed class TenantContext : ITenantContext
{
    public const string TenantClaim = "tenant_id";
    public const string BranchClaim = "branch_id";
    public const string DeviceClaim = "device_id";

    public TenantContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return;

        TenantId = user.FindFirstValue(TenantClaim);
        BranchId = user.FindFirstValue(BranchClaim);
        DeviceId = user.FindFirstValue(DeviceClaim);
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        IsAuthenticated = true;
    }

    public string? TenantId { get; }
    public string? BranchId { get; }
    public string? UserId { get; }
    public string? DeviceId { get; }
    public bool IsAuthenticated { get; }

    public string RequireTenantId() => TenantId
        ?? throw new InvalidOperationException("No tenant is resolved on the current request.");
}
