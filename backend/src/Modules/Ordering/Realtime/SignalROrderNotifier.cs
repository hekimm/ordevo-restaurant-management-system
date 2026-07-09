using Microsoft.AspNetCore.SignalR;
using Ordevo.Modules.Ordering.Application;

namespace Ordevo.Modules.Ordering.Realtime;

public sealed class SignalROrderNotifier(
    IHubContext<OrdersHub> ordersHub,
    IHubContext<TablesHub> tablesHub) : IOrderNotifier
{
    public Task OrderChangedAsync(string tenantId, string orderId, string action, CancellationToken ct = default)
        => ordersHub.Clients.Group(HubGroups.Tenant(tenantId))
            .SendAsync("orderChanged", new { orderId, action }, ct);

    public Task TablesChangedAsync(string tenantId, CancellationToken ct = default)
        => tablesHub.Clients.Group(HubGroups.Tenant(tenantId))
            .SendAsync("tablesChanged", new { tenantId }, ct);
}
