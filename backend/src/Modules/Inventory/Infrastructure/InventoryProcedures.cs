using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Inventory.Application;

namespace Ordevo.Modules.Inventory.Infrastructure;

public sealed class InventoryProcedures(IDbConnectionFactory factory) : IInventoryProcedures
{
    public async Task ReceivePurchaseAsync(string purchaseId, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_purchase_id", purchaseId);
        p.Add("p_user_id", userId);
        await db.ExecuteAsync("PKG_INVENTORY.RECEIVE_PURCHASE", p, commandType: CommandType.StoredProcedure);
    }

    public async Task AdjustStockAsync(string stockItemId, decimal newQty, string? reason, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_stock_item_id", stockItemId);
        p.Add("p_new_qty", newQty);
        p.Add("p_reason", reason);
        p.Add("p_user_id", userId);
        await db.ExecuteAsync("PKG_INVENTORY.ADJUST_STOCK", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<string> RecordWastageAsync(string stockItemId, decimal qty, string? reason, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_stock_item_id", stockItemId);
        p.Add("p_qty", qty);
        p.Add("p_reason", reason);
        p.Add("p_user_id", userId);
        p.Add("p_wastage_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_INVENTORY.RECORD_WASTAGE", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_wastage_id");
    }
}
