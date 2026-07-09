using Oracle.ManagedDataAccess.Client;
using Ordevo.BuildingBlocks.Results;

namespace Ordevo.Modules.Inventory.Application;

public sealed class InventoryService(IInventoryRepository repo, IInventoryProcedures procs)
{
    public Task<IReadOnlyList<UnitDto>> ListUnitsAsync(string tenantId, CancellationToken ct = default)
        => repo.ListUnitsAsync(tenantId, ct);

    public async Task<UnitDto> CreateUnitAsync(string tenantId, CreateUnitRequest r, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await repo.InsertUnitAsync(id, tenantId, r.Code.Trim(), r.Name.Trim(), ct);
        return new UnitDto(id, r.Code.Trim(), r.Name.Trim());
    }

    public async Task<Result<UnitDto>> UpdateUnitAsync(string tenantId, string id, UpdateUnitRequest r, CancellationToken ct = default)
    {
        if (!await repo.UpdateUnitAsync(tenantId, id, new UpdateUnitRequest(r.Code.Trim(), r.Name.Trim()), ct))
            return Error.NotFound("unit.not_found", "Birim bulunamadı.");

        var unit = (await repo.ListUnitsAsync(tenantId, ct)).First(x => x.Id == id);
        return unit;
    }

    public async Task<Result> DeleteUnitAsync(string tenantId, string id, CancellationToken ct = default)
    {
        try
        {
            var affected = await repo.DeleteUnitAsync(tenantId, id, ct);
            return affected > 0 ? Result.Success() : Error.NotFound("unit.not_found", "Birim bulunamadı.");
        }
        catch (OracleException)
        {
            return Error.Conflict("unit.in_use", "Bu birim stok kalemlerinde kullanıldığı için silinemez.");
        }
    }

    public async Task<IReadOnlyList<StockItemDto>> ListStockAsync(string tenantId, string branchId, CancellationToken ct = default)
        => (await repo.ListStockAsync(tenantId, branchId, ct)).Select(ToDto).ToList();

    public async Task<Result<StockItemDto>> GetStockAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var s = await repo.GetStockAsync(tenantId, id, ct);
        return s is null ? Error.NotFound("stock.not_found", "Stok kalemi bulunamadı.") : ToDto(s);
    }

    public async Task<StockItemDto> CreateStockAsync(string tenantId, string branchId, UpsertStockItemRequest r, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await repo.InsertStockAsync(id, tenantId, branchId, r, ct);
        return (await repo.GetStockAsync(tenantId, id, ct) is { } s) ? ToDto(s)
            : new StockItemDto(id, r.Name, r.Sku, r.UnitId, null, 0, r.ReorderLevel, r.UnitCost, r.IsActive);
    }

    public async Task<Result<StockItemDto>> UpdateStockAsync(string tenantId, string id, UpsertStockItemRequest r, CancellationToken ct = default)
    {
        if (!await repo.UpdateStockAsync(tenantId, id, r, ct))
            return Error.NotFound("stock.not_found", "Stok kalemi bulunamadı.");
        return ToDto((await repo.GetStockAsync(tenantId, id, ct))!);
    }

    public async Task<Result> DeleteStockAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await repo.DeleteStockAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("stock.not_found", "Stok kalemi bulunamadı.");
    }

    public Task<Result<StockItemDto>> AdjustAsync(string tenantId, string id, AdjustStockRequest r, string userId, CancellationToken ct = default)
        => RunStockAsync(tenantId, id, () => procs.AdjustStockAsync(id, r.NewQuantity, r.Reason, userId, ct), ct);

    public async Task<Result<RecipeDto>> GetRecipeAsync(string tenantId, string menuItemId, CancellationToken ct = default)
    {
        var recipe = await repo.GetRecipeAsync(tenantId, menuItemId, ct);
        return recipe is null ? Error.NotFound("recipe.not_found", "Reçete bulunamadı.") : recipe;
    }

    public async Task<RecipeDto> SetRecipeAsync(string tenantId, string menuItemId, SetRecipeRequest r, CancellationToken ct = default)
    {
        await repo.SetRecipeAsync(tenantId, menuItemId, r.YieldQty <= 0 ? 1 : r.YieldQty, r.Lines, ct);
        return (await repo.GetRecipeAsync(tenantId, menuItemId, ct))!;
    }

    public Task<IReadOnlyList<SupplierDto>> ListSuppliersAsync(string tenantId, CancellationToken ct = default)
        => repo.ListSuppliersAsync(tenantId, ct);

    public async Task<SupplierDto> CreateSupplierAsync(string tenantId, CreateSupplierRequest r, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await repo.InsertSupplierAsync(id, tenantId, r, ct);
        return new SupplierDto(id, r.Name, r.Phone, r.Email, r.TaxNo, true);
    }

    public async Task<Result<SupplierDto>> UpdateSupplierAsync(string tenantId, string id, UpdateSupplierRequest r, CancellationToken ct = default)
    {
        var request = new UpdateSupplierRequest(r.Name.Trim(), r.Phone, r.Email, r.TaxNo, r.IsActive);
        if (!await repo.UpdateSupplierAsync(tenantId, id, request, ct))
            return Error.NotFound("supplier.not_found", "Tedarikçi bulunamadı.");

        var supplier = (await repo.ListSuppliersAsync(tenantId, ct)).First(x => x.Id == id);
        return supplier;
    }

    public async Task<Result> DeleteSupplierAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var affected = await repo.DeleteSupplierAsync(tenantId, id, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("supplier.not_found", "Tedarikçi bulunamadı.");
    }

    public async Task<Result<PurchaseDto>> CreatePurchaseAsync(string tenantId, string branchId, string userId, CreatePurchaseRequest r, CancellationToken ct = default)
    {
        if (r.Lines.Length == 0) return Error.Validation("purchase.no_lines", "Alım kalemi gerekli.");
        var id = await repo.CreatePurchaseAsync(tenantId, branchId, userId, r, ct);
        return (await repo.GetPurchaseAsync(tenantId, id, ct))!;
    }

    public async Task<Result<PurchaseDto>> GetPurchaseAsync(string tenantId, string id, CancellationToken ct = default)
    {
        var p = await repo.GetPurchaseAsync(tenantId, id, ct);
        return p is null ? Error.NotFound("purchase.not_found", "Alım bulunamadı.") : p;
    }

    public async Task<Result<PurchaseDto>> ReceivePurchaseAsync(string tenantId, string id, string userId, CancellationToken ct = default)
    {
        if (await repo.GetPurchaseAsync(tenantId, id, ct) is null)
            return Error.NotFound("purchase.not_found", "Alım bulunamadı.");
        try
        {
            await procs.ReceivePurchaseAsync(id, userId, ct);
            return (await repo.GetPurchaseAsync(tenantId, id, ct))!;
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<string>> RecordWastageAsync(string tenantId, RecordWastageRequest r, string userId, CancellationToken ct = default)
    {
        if (await repo.GetStockAsync(tenantId, r.StockItemId, ct) is null)
            return Error.NotFound("stock.not_found", "Stok kalemi bulunamadı.");
        try
        {
            return await procs.RecordWastageAsync(r.StockItemId, r.Quantity, r.Reason, userId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<StockMovementDto>> ListMovementsAsync(string tenantId, string stockItemId, CancellationToken ct = default)
        => repo.ListMovementsAsync(tenantId, stockItemId, ct);

    private async Task<Result<StockItemDto>> RunStockAsync(string tenantId, string id, Func<Task> op, CancellationToken ct)
    {
        if (await repo.GetStockAsync(tenantId, id, ct) is null)
            return Error.NotFound("stock.not_found", "Stok kalemi bulunamadı.");
        try
        {
            await op();
            return ToDto((await repo.GetStockAsync(tenantId, id, ct))!);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    private static StockItemDto ToDto(StockItemRow s)
        => new(s.Id, s.Name, s.Sku, s.UnitId, s.UnitCode, s.OnHand, s.ReorderLevel, s.UnitCost, s.IsActive);

    private static bool TryBusiness(OracleException ex, out Error error)
    {
        if (ex.Number is >= 20201 and <= 20210)
        {
            var message = ex.Message.Split('\n')[0].Replace($"ORA-{ex.Number}:", "").Trim();
            error = ex.Number switch
            {
                20202 or 20203 => Error.NotFound("inventory.not_found", "Kayıt bulunamadı."),
                20201 => Error.Conflict("inventory.state", "Kayıt zaten işlenmiş."),
                _ => Error.Validation("inventory.rule", string.IsNullOrWhiteSpace(message) ? "Stok kuralı ihlali." : message)
            };
            return true;
        }
        error = Error.Failure("inventory.db", "Veritabanı hatası.");
        return false;
    }
}
