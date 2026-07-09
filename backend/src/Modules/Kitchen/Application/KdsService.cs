using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Kitchen.Realtime;
using Ordevo.Modules.Ordering.Application;

namespace Ordevo.Modules.Kitchen.Application;

public sealed class KdsService(
    IKdsRepository kds,
    IOrderingProcedures ordering,
    IKitchenNotifier kitchenNotifier,
    IOrderNotifier orderNotifier)
{
    private static readonly Dictionary<string, string[]> Transitions = new()
    {
        ["pending"] = ["in_kitchen"],
        ["in_kitchen"] = ["ready", "pending"],
        ["ready"] = ["served", "in_kitchen"],
        ["served"] = ["ready"]
    };

    public async Task<IReadOnlyList<KdsTicketDto>> GetBoardAsync(string tenantId, string branchId, string? stationCode, CancellationToken ct = default)
    {
        var rows = await kds.GetBoardAsync(tenantId, branchId, stationCode, ct);
        if (rows.Count == 0) return [];

        var now = await kds.GetDatabaseNowAsync(ct);

        return rows.GroupBy(r => r.OrderId).Select(grp =>
        {
            var first = grp.OrderBy(x => x.CreatedAt).First();
            var items = grp.Select(r => new KdsItemDto(
                r.OrderItemId, r.ItemName, r.Quantity, r.CourseNo, r.Status, r.Station, r.Note, r.Modifiers,
                (int)Math.Max(0, (now - r.CreatedAt).TotalSeconds), r.CreatedAt, r.IsAdditional)).ToList();

            return new KdsTicketDto(
                first.OrderId, first.OrderNo, first.TableName, first.CreatedAt,
                (int)Math.Max(0, (now - first.CreatedAt).TotalSeconds), items);
        })
        .OrderBy(t => t.OpenedAt)
        .ToList();
    }

    public async Task<Result> SetStatusAsync(string tenantId, string itemId, string targetStatus, string userId, CancellationToken ct = default)
    {
        if (targetStatus is not ("pending" or "in_kitchen" or "ready" or "served"))
            return Error.Validation("kds.bad_status", "Geçersiz durum.");

        var state = await kds.GetItemStateAsync(tenantId, itemId, ct);
        if (state is null) return Error.NotFound("kds.item_not_found", "Kalem bulunamadı.");
        if (state.OrderStatus != "open") return Error.Conflict("kds.order_closed", "Adisyon kapalı.");

        if (!Transitions.TryGetValue(state.ItemStatus, out var allowed) || !allowed.Contains(targetStatus))
            return Error.Validation("kds.bad_transition", $"'{state.ItemStatus}' → '{targetStatus}' geçişi geçersiz.");

        await ordering.UpdateItemStatusAsync(itemId, targetStatus, ct);
        await NotifyAsync(tenantId, state.OrderId, $"item_{targetStatus}", ct);
        return Result.Success();
    }

    public async Task<Result> BumpOrderAsync(string tenantId, string orderId, string userId, CancellationToken ct = default)
    {
        var itemIds = await kds.GetActiveItemIdsAsync(tenantId, orderId, ct);
        if (itemIds.Count == 0) return Error.NotFound("kds.no_active_items", "Hazırlanacak kalem yok.");

        foreach (var itemId in itemIds)
            await ordering.UpdateItemStatusAsync(itemId, "ready", ct);

        await NotifyAsync(tenantId, orderId, "bumped", ct);
        return Result.Success();
    }

    private async Task NotifyAsync(string tenantId, string orderId, string action, CancellationToken ct)
    {
        await kitchenNotifier.TicketChangedAsync(tenantId, orderId, action, ct);
        await orderNotifier.OrderChangedAsync(tenantId, orderId, action, ct);
    }
}
