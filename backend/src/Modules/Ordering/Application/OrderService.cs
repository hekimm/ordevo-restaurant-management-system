using Oracle.ManagedDataAccess.Client;
using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Ordering.Domain;

namespace Ordevo.Modules.Ordering.Application;

public sealed class OrderService(
    IOrderingProcedures procs,
    IOrderReadRepository read,
    IMenuPricing menu,
    IOrderNotifier notifier)
{
    public async Task<Result<OrderDto>> OpenAsync(
        string tenantId, string branchId, string userId, OpenOrderRequest r, CancellationToken ct = default)
    {
        try
        {
            var (orderId, _) = await procs.OpenOrderAsync(tenantId, branchId, r.TableId, r.OrderType, r.GuestCount, userId, ct);
            await NotifyAsync(tenantId, orderId, "opened", r.TableId is not null, ct);
            return await BuildAsync(tenantId, orderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<OrderDto>> GetAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        var dto = await BuildOrNullAsync(tenantId, orderId, ct);
        return dto is null ? Error.NotFound("order.not_found", "Adisyon bulunamadı.") : dto;
    }

    public Task<IReadOnlyList<OrderSummaryDto>> ListAsync(string tenantId, string branchId, string? status, CancellationToken ct = default)
        => read.ListOrdersAsync(tenantId, branchId, status, ct);

    public async Task<Result<OrderDto>> AddItemAsync(
        string tenantId, string orderId, string userId, AddItemRequest r, CancellationToken ct = default)
    {
        var item = await menu.GetItemAsync(tenantId, r.MenuItemId, ct);
        if (item is null) return Error.Validation("order.invalid_item", "Ürün bulunamadı veya pasif.");
        if (r.Quantity <= 0) return Error.Validation("order.invalid_qty", "Adet pozitif olmalı.");

        var modifiers = r.ModifierIds is { Length: > 0 }
            ? await menu.GetModifiersAsync(tenantId, r.ModifierIds, ct)
            : [];

        try
        {
            var itemId = await procs.AddItemAsync(orderId, item.Id, item.Name, item.Price, r.Quantity, item.VatRate, r.CourseNo, r.Note, userId, ct);
            foreach (var m in modifiers)
                await procs.AddItemModifierAsync(itemId, m.Id, m.Name, m.PriceDelta, ct);

            await NotifyAsync(tenantId, orderId, "item_added", false, ct);
            return await BuildAsync(tenantId, orderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public Task<Result<OrderDto>> SetQuantityAsync(string tenantId, string itemId, decimal qty, string userId, CancellationToken ct = default)
        => MutateItemAsync(tenantId, itemId, "item_qty", ct, () => procs.SetItemQtyAsync(itemId, qty, ct));

    public Task<Result<OrderDto>> VoidItemAsync(string tenantId, string itemId, string? reason, string userId, CancellationToken ct = default)
        => MutateItemAsync(tenantId, itemId, "item_void", ct, () => procs.VoidItemAsync(itemId, reason, userId, ct));

    public Task<Result<OrderDto>> CompItemAsync(string tenantId, string itemId, string userId, CancellationToken ct = default)
        => MutateItemAsync(tenantId, itemId, "item_comp", ct, () => procs.CompItemAsync(itemId, userId, ct));

    public Task<Result<OrderDto>> UpdateItemStatusAsync(string tenantId, string itemId, string status, string userId, CancellationToken ct = default)
        => MutateItemAsync(tenantId, itemId, "item_status", ct, () => procs.UpdateItemStatusAsync(itemId, status, ct));

    public async Task<Result<OrderDto>> ApplyDiscountAsync(
        string tenantId, string orderId, ApplyDiscountRequest r, string userId, CancellationToken ct = default)
    {
        if (r.Type is not ("percent" or "amount")) return Error.Validation("order.bad_discount", "İskonto tipi percent veya amount olmalı.");
        try
        {
            await procs.ApplyDiscountAsync(orderId, r.Type, r.Value, r.Reason, userId, ct);
            await NotifyAsync(tenantId, orderId, "discount", false, ct);
            return await BuildAsync(tenantId, orderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<OrderDto>> TransferAsync(string tenantId, string orderId, string toTableId, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.TransferOrderAsync(orderId, toTableId, userId, ct);
            await NotifyAsync(tenantId, orderId, "transferred", true, ct);
            return await BuildAsync(tenantId, orderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<OrderDto>> MergeAsync(string tenantId, string targetOrderId, string sourceOrderId, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.MergeOrdersAsync(sourceOrderId, targetOrderId, userId, ct);
            await NotifyAsync(tenantId, targetOrderId, "merged", true, ct);
            return await BuildAsync(tenantId, targetOrderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<OrderDto>> SplitAsync(
        string tenantId, string branchId, string orderId, string[] itemIds, string? toTableId, string userId, CancellationToken ct = default)
    {
        if (itemIds.Length == 0) return Error.Validation("order.split_empty", "Bölünecek kalem seçilmeli.");
        var source = await read.GetOrderAsync(tenantId, orderId, ct);
        if (source is null) return Error.NotFound("order.not_found", "Adisyon bulunamadı.");

        try
        {
            var (newOrderId, _) = await procs.OpenOrderAsync(tenantId, branchId, toTableId, source.OrderType, 1, userId, ct);
            foreach (var itemId in itemIds)
            {
                var owner = await read.GetOrderIdOfItemAsync(tenantId, itemId, ct);
                if (owner == orderId)
                    await procs.MoveItemAsync(itemId, newOrderId, ct);
            }
            await procs.LogTransferAsync(tenantId, newOrderId, "split", source.TableId, toTableId, orderId, userId, ct);
            await NotifyAsync(tenantId, newOrderId, "split", true, ct);
            return await BuildAsync(tenantId, newOrderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<OrderDto>> CloseAsync(string tenantId, string orderId, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.CloseOrderAsync(orderId, userId, ct);
            await NotifyAsync(tenantId, orderId, "closed", true, ct);
            return await BuildAsync(tenantId, orderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<OrderDto>> CancelAsync(string tenantId, string orderId, string? reason, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.CancelOrderAsync(orderId, reason, userId, ct);
            await NotifyAsync(tenantId, orderId, "cancelled", true, ct);
            return await BuildAsync(tenantId, orderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    private async Task<Result<OrderDto>> MutateItemAsync(
        string tenantId, string itemId, string action, CancellationToken ct, Func<Task> mutation)
    {
        var orderId = await read.GetOrderIdOfItemAsync(tenantId, itemId, ct);
        if (orderId is null) return Error.NotFound("order.item_not_found", "Kalem bulunamadı.");
        try
        {
            await mutation();
            await NotifyAsync(tenantId, orderId, action, false, ct);
            return await BuildAsync(tenantId, orderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    private async Task NotifyAsync(string tenantId, string orderId, string action, bool tablesChanged, CancellationToken ct)
    {
        await notifier.OrderChangedAsync(tenantId, orderId, action, ct);
        if (tablesChanged) await notifier.TablesChangedAsync(tenantId, ct);
    }

    private async Task<OrderDto> BuildAsync(string tenantId, string orderId, CancellationToken ct)
        => (await BuildOrNullAsync(tenantId, orderId, ct))!;

    private async Task<OrderDto?> BuildOrNullAsync(string tenantId, string orderId, CancellationToken ct)
    {
        var order = await read.GetOrderAsync(tenantId, orderId, ct);
        if (order is null) return null;

        var items = await read.GetItemsAsync(orderId, ct);
        var mods = await read.GetItemModifiersAsync(orderId, ct);
        var modsByItem = mods.GroupBy(m => m.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Select(m => new OrderItemModifierDto(m.Id, m.NameSnapshot, m.PriceDelta)).ToList());

        var itemDtos = items.Select(i => new OrderItemDto(
            i.Id, i.MenuItemId, i.NameSnapshot, i.UnitPrice, i.Quantity, i.ModifierTotal, i.LineTotal,
            i.VatRate, i.CourseNo, i.Status, i.IsComp, i.Note,
            modsByItem.TryGetValue(i.Id, out var list) ? list : [])).ToList();

        return new OrderDto(
            order.Id, order.OrderNo, order.TableId, order.OrderType, order.Status, order.GuestCount,
            order.Subtotal, order.DiscountTotal, order.TaxTotal, order.Total, order.Note,
            order.OpenedAt, order.ClosedAt, itemDtos);
    }

    private static bool TryBusiness(OracleException ex, out Error error)
    {
        if (ex.Number is >= 20001 and <= 20010)
        {
            var message = ex.Message.Split('\n')[0].Replace($"ORA-{ex.Number}:", "").Trim();
            error = ex.Number switch
            {
                20001 => Error.NotFound("order.not_found", "Adisyon bulunamadı."),
                20003 => Error.Conflict("order.table_busy", "Masada açık adisyon var."),
                _ => Error.Validation("order.rule", string.IsNullOrWhiteSpace(message) ? "Adisyon kuralı ihlali." : message)
            };
            return true;
        }
        error = Error.Failure("order.db", "Veritabanı hatası.");
        return false;
    }
}
