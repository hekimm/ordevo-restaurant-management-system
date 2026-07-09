namespace Ordevo.Modules.Inventory.Application;

public interface IInventoryRepository
{
    Task<IReadOnlyList<UnitDto>> ListUnitsAsync(string tenantId, CancellationToken ct = default);
    Task InsertUnitAsync(string id, string tenantId, string code, string name, CancellationToken ct = default);
    Task<bool> UpdateUnitAsync(string tenantId, string id, UpdateUnitRequest request, CancellationToken ct = default);
    Task<int> DeleteUnitAsync(string tenantId, string id, CancellationToken ct = default);

    Task<IReadOnlyList<StockItemRow>> ListStockAsync(string tenantId, string branchId, CancellationToken ct = default);
    Task<StockItemRow?> GetStockAsync(string tenantId, string id, CancellationToken ct = default);
    Task InsertStockAsync(string id, string tenantId, string branchId, UpsertStockItemRequest r, CancellationToken ct = default);
    Task<bool> UpdateStockAsync(string tenantId, string id, UpsertStockItemRequest r, CancellationToken ct = default);
    Task<int> DeleteStockAsync(string tenantId, string id, CancellationToken ct = default);

    Task<RecipeDto?> GetRecipeAsync(string tenantId, string menuItemId, CancellationToken ct = default);
    Task SetRecipeAsync(string tenantId, string menuItemId, decimal yieldQty, IReadOnlyList<RecipeLineInput> lines, CancellationToken ct = default);

    Task<IReadOnlyList<SupplierDto>> ListSuppliersAsync(string tenantId, CancellationToken ct = default);
    Task InsertSupplierAsync(string id, string tenantId, CreateSupplierRequest r, CancellationToken ct = default);
    Task<bool> UpdateSupplierAsync(string tenantId, string id, UpdateSupplierRequest request, CancellationToken ct = default);
    Task<int> DeleteSupplierAsync(string tenantId, string id, CancellationToken ct = default);

    Task<string> CreatePurchaseAsync(string tenantId, string branchId, string userId, CreatePurchaseRequest r, CancellationToken ct = default);
    Task<PurchaseDto?> GetPurchaseAsync(string tenantId, string id, CancellationToken ct = default);

    Task<IReadOnlyList<StockMovementDto>> ListMovementsAsync(string tenantId, string stockItemId, CancellationToken ct = default);
}

public interface IInventoryProcedures
{
    Task ReceivePurchaseAsync(string purchaseId, string userId, CancellationToken ct = default);
    Task AdjustStockAsync(string stockItemId, decimal newQty, string? reason, string userId, CancellationToken ct = default);
    Task<string> RecordWastageAsync(string stockItemId, decimal qty, string? reason, string userId, CancellationToken ct = default);
}
