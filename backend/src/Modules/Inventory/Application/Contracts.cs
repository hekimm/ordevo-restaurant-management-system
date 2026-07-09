namespace Ordevo.Modules.Inventory.Application;

public sealed record UnitDto(string Id, string Code, string Name);
public sealed record CreateUnitRequest(string Code, string Name);
public sealed record UpdateUnitRequest(string Code, string Name);

public sealed class StockItemRow
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Sku { get; set; }
    public string UnitId { get; set; } = default!;
    public string? UnitCode { get; set; }
    public decimal OnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
    public bool IsActive { get; set; }
}

public sealed record StockItemDto(string Id, string Name, string? Sku, string UnitId, string? UnitCode, decimal OnHand, decimal ReorderLevel, decimal UnitCost, bool IsActive);
public sealed record UpsertStockItemRequest(string Name, string? Sku, string UnitId, decimal ReorderLevel, decimal UnitCost, bool IsActive = true);
public sealed record AdjustStockRequest(decimal NewQuantity, string? Reason);

public sealed record RecipeLineDto(string StockItemId, string? StockItemName, decimal Quantity);
public sealed record RecipeDto(string MenuItemId, decimal YieldQty, IReadOnlyList<RecipeLineDto> Lines);
public sealed record SetRecipeRequest(decimal YieldQty, RecipeLineInput[] Lines);
public sealed record RecipeLineInput(string StockItemId, decimal Quantity);

public sealed record SupplierDto(string Id, string Name, string? Phone, string? Email, string? TaxNo, bool IsActive);
public sealed record CreateSupplierRequest(string Name, string? Phone, string? Email, string? TaxNo);
public sealed record UpdateSupplierRequest(string Name, string? Phone, string? Email, string? TaxNo, bool IsActive = true);

public sealed record PurchaseLineInput(string StockItemId, decimal Quantity, decimal UnitCost);
public sealed record CreatePurchaseRequest(string? SupplierId, string? Note, PurchaseLineInput[] Lines);
public sealed record PurchaseLineDto(string StockItemId, string? StockItemName, decimal Quantity, decimal UnitCost, decimal LineTotal);
public sealed record PurchaseDto(string Id, string? SupplierId, string Status, decimal Total, string? Note, IReadOnlyList<PurchaseLineDto> Lines);

public sealed record RecordWastageRequest(string StockItemId, decimal Quantity, string? Reason);
public sealed record StockMovementDto(string Id, string MoveType, decimal Quantity, decimal UnitCost, string? RefType, string? Note, DateTimeOffset CreatedAt);
