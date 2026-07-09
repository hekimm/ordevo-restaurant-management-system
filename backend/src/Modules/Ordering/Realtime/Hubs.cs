using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Ordevo.BuildingBlocks.Multitenancy;

namespace Ordevo.Modules.Ordering.Realtime;

public static class HubGroups
{
    public static string Tenant(string tenantId) => $"tenant:{tenantId}";
}

[Authorize]
public sealed class OrdersHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst(TenantContext.TenantClaim)?.Value;
        if (tenantId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Tenant(tenantId));
        await base.OnConnectedAsync();
    }
}

[Authorize]
public sealed class TablesHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst(TenantContext.TenantClaim)?.Value;
        if (tenantId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Tenant(tenantId));
        await base.OnConnectedAsync();
    }
}
