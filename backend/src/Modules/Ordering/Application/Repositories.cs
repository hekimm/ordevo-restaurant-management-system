using Ordevo.Modules.Ordering.Domain;

namespace Ordevo.Modules.Ordering.Application;

public interface ITableRepository
{
    Task<IReadOnlyList<TableSection>> ListSectionsAsync(string tenantId, string branchId, CancellationToken ct = default);
    Task<TableSection?> GetSectionAsync(string tenantId, string id, CancellationToken ct = default);
    Task InsertSectionAsync(TableSection section, CancellationToken ct = default);
    Task<bool> UpdateSectionAsync(TableSection section, CancellationToken ct = default);
    Task<int> DeleteSectionAsync(string tenantId, string id, CancellationToken ct = default);

    Task<IReadOnlyList<DiningTable>> ListTablesAsync(string tenantId, string branchId, CancellationToken ct = default);
    Task<DiningTable?> GetTableAsync(string tenantId, string id, CancellationToken ct = default);
    Task InsertTableAsync(DiningTable table, CancellationToken ct = default);
    Task<bool> UpdateTableAsync(DiningTable table, CancellationToken ct = default);
    Task<int> DeleteTableAsync(string tenantId, string id, CancellationToken ct = default);
}

public interface IMenuPricing
{
    Task<MenuItemPrice?> GetItemAsync(string tenantId, string menuItemId, CancellationToken ct = default);
    Task<IReadOnlyList<ModifierPrice>> GetModifiersAsync(string tenantId, IEnumerable<string> modifierIds, CancellationToken ct = default);
}

public sealed class MenuItemPrice
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public decimal VatRate { get; set; }
}

public sealed class ModifierPrice
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal PriceDelta { get; set; }
}

public interface IOrderReadRepository
{
    Task<Order?> GetOrderAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderItem>> GetItemsAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderItemModifier>> GetItemModifiersAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderSummaryDto>> ListOrdersAsync(string tenantId, string branchId, string? status, CancellationToken ct = default);
    Task<string?> GetOrderIdOfItemAsync(string tenantId, string itemId, CancellationToken ct = default);
}

public interface IOrderingProcedures
{
    Task<(string OrderId, long OrderNo)> OpenOrderAsync(
        string tenantId, string branchId, string? tableId, string orderType, int guestCount, string userId, CancellationToken ct = default);

    Task<string> AddItemAsync(
        string orderId, string menuItemId, string name, decimal unitPrice, decimal qty,
        decimal vatRate, int courseNo, string? note, string userId, CancellationToken ct = default);

    Task AddItemModifierAsync(string orderItemId, string? modifierId, string name, decimal priceDelta, CancellationToken ct = default);
    Task SetItemQtyAsync(string orderItemId, decimal qty, CancellationToken ct = default);
    Task VoidItemAsync(string orderItemId, string? reason, string userId, CancellationToken ct = default);
    Task CompItemAsync(string orderItemId, string userId, CancellationToken ct = default);
    Task UpdateItemStatusAsync(string orderItemId, string status, CancellationToken ct = default);
    Task<string> ApplyDiscountAsync(string orderId, string type, decimal value, string? reason, string userId, CancellationToken ct = default);
    Task MoveItemAsync(string orderItemId, string targetOrderId, CancellationToken ct = default);
    Task TransferOrderAsync(string orderId, string toTableId, string userId, CancellationToken ct = default);
    Task MergeOrdersAsync(string sourceOrderId, string targetOrderId, string userId, CancellationToken ct = default);
    Task CloseOrderAsync(string orderId, string userId, CancellationToken ct = default);
    Task CancelOrderAsync(string orderId, string? reason, string userId, CancellationToken ct = default);

    Task LogTransferAsync(
        string tenantId, string orderId, string action, string? fromTable, string? toTable,
        string? relatedOrder, string userId, CancellationToken ct = default);
}
