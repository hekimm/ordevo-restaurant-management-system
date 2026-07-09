using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Ordering.Application;

namespace Ordevo.Modules.Ordering.Infrastructure;

public sealed class OrderingProcedures(IDbConnectionFactory factory) : IOrderingProcedures
{
    public async Task<(string OrderId, long OrderNo)> OpenOrderAsync(
        string tenantId, string branchId, string? tableId, string orderType, int guestCount, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_table_id", tableId);
        p.Add("p_order_type", orderType);
        p.Add("p_guest_count", guestCount);
        p.Add("p_user_id", userId);
        p.Add("p_order_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        p.Add("p_order_no", dbType: DbType.Int64, direction: ParameterDirection.Output);
        await db.ExecuteAsync("PKG_ORDERING.OPEN_ORDER", p, commandType: CommandType.StoredProcedure);
        return (p.Get<string>("p_order_id"), p.Get<long>("p_order_no"));
    }

    public async Task<string> AddItemAsync(
        string orderId, string menuItemId, string name, decimal unitPrice, decimal qty,
        decimal vatRate, int courseNo, string? note, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_order_id", orderId);
        p.Add("p_menu_item_id", menuItemId);
        p.Add("p_name", name);
        p.Add("p_unit_price", unitPrice);
        p.Add("p_qty", qty);
        p.Add("p_vat_rate", vatRate);
        p.Add("p_course_no", courseNo);
        p.Add("p_note", note);
        p.Add("p_user_id", userId);
        p.Add("p_item_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_ORDERING.ADD_ITEM", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_item_id");
    }

    public async Task AddItemModifierAsync(string orderItemId, string? modifierId, string name, decimal priceDelta, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_order_item_id", orderItemId);
        p.Add("p_modifier_id", modifierId);
        p.Add("p_name", name);
        p.Add("p_price_delta", priceDelta);
        await db.ExecuteAsync("PKG_ORDERING.ADD_ITEM_MODIFIER", p, commandType: CommandType.StoredProcedure);
    }

    public async Task SetItemQtyAsync(string orderItemId, decimal qty, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.SET_ITEM_QTY", ct, ("p_order_item_id", orderItemId), ("p_qty", qty));

    public async Task VoidItemAsync(string orderItemId, string? reason, string userId, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.VOID_ITEM", ct, ("p_order_item_id", orderItemId), ("p_reason", reason), ("p_user_id", userId));

    public async Task CompItemAsync(string orderItemId, string userId, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.COMP_ITEM", ct, ("p_order_item_id", orderItemId), ("p_user_id", userId));

    public async Task UpdateItemStatusAsync(string orderItemId, string status, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.UPDATE_ITEM_STATUS", ct, ("p_order_item_id", orderItemId), ("p_status", status));

    public async Task<string> ApplyDiscountAsync(string orderId, string type, decimal value, string? reason, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_order_id", orderId);
        p.Add("p_disc_type", type);
        p.Add("p_disc_value", value);
        p.Add("p_reason", reason);
        p.Add("p_user_id", userId);
        p.Add("p_disc_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_ORDERING.APPLY_DISCOUNT", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_disc_id");
    }

    public async Task MoveItemAsync(string orderItemId, string targetOrderId, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.MOVE_ITEM", ct, ("p_order_item_id", orderItemId), ("p_target_order_id", targetOrderId));

    public async Task TransferOrderAsync(string orderId, string toTableId, string userId, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.TRANSFER_ORDER", ct, ("p_order_id", orderId), ("p_to_table_id", toTableId), ("p_user_id", userId));

    public async Task MergeOrdersAsync(string sourceOrderId, string targetOrderId, string userId, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.MERGE_ORDERS", ct, ("p_source_order_id", sourceOrderId), ("p_target_order_id", targetOrderId), ("p_user_id", userId));

    public async Task CloseOrderAsync(string orderId, string userId, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.CLOSE_ORDER", ct, ("p_order_id", orderId), ("p_user_id", userId));

    public async Task CancelOrderAsync(string orderId, string? reason, string userId, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.CANCEL_ORDER", ct, ("p_order_id", orderId), ("p_reason", reason), ("p_user_id", userId));

    public async Task LogTransferAsync(
        string tenantId, string orderId, string action, string? fromTable, string? toTable,
        string? relatedOrder, string userId, CancellationToken ct = default)
        => await ExecAsync("PKG_ORDERING.LOG_TRANSFER", ct,
            ("p_tenant_id", tenantId), ("p_order_id", orderId), ("p_action", action),
            ("p_from_table", fromTable), ("p_to_table", toTable), ("p_related_order", relatedOrder), ("p_user_id", userId));

    private async Task ExecAsync(string procName, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        foreach (var (name, value) in args)
            p.Add(name, value);
        await db.ExecuteAsync(procName, p, commandType: CommandType.StoredProcedure);
    }
}
