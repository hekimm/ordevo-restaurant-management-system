using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Ordevo.BuildingBlocks.Multitenancy;

namespace Ordevo.Modules.Kitchen.Realtime;

[Authorize]
public sealed class KdsHub : Hub
{
    public static string Group(string tenantId) => $"kds:{tenantId}";

    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst(TenantContext.TenantClaim)?.Value;
        if (tenantId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, Group(tenantId));
        await base.OnConnectedAsync();
    }
}

public interface IKitchenNotifier
{
    Task TicketChangedAsync(string tenantId, string orderId, string action, CancellationToken ct = default);
}

public sealed class KdsNotifier(IHubContext<KdsHub> hub) : IKitchenNotifier
{
    public Task TicketChangedAsync(string tenantId, string orderId, string action, CancellationToken ct = default)
        => hub.Clients.Group(KdsHub.Group(tenantId)).SendAsync("ticketChanged", new { orderId, action }, ct);
}
