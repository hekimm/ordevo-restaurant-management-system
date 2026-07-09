using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Inventory.Application;

namespace Ordevo.Modules.Inventory.Infrastructure;

file sealed class PurchaseHeader
{
    public string Id { get; set; } = default!;
    public string? SupplierId { get; set; }
    public string Status { get; set; } = default!;
    public decimal Total { get; set; }
    public string? Note { get; set; }
}

public sealed class InventoryRepository(IDbConnectionFactory factory) : IInventoryRepository
{
    private static string NewId() => Guid.NewGuid().ToString();

    public async Task<IReadOnlyList<UnitDto>> ListUnitsAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<UnitDto>(
            "SELECT ID, CODE, NAME FROM UNITS WHERE TENANT_ID = :tenantId ORDER BY CODE",
            new OracleParams(new { tenantId }));
        return rows.AsList();
    }

    public async Task InsertUnitAsync(string id, string tenantId, string code, string name, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "INSERT INTO UNITS (ID, TENANT_ID, CODE, NAME) VALUES (:id, :tenantId, :code, :name)",
            new OracleParams(new { id, tenantId, code, name }));
    }

    public async Task<bool> UpdateUnitAsync(string tenantId, string id, UpdateUnitRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            "UPDATE UNITS SET CODE = :Code, NAME = :Name WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id, request.Code, request.Name }));
        return rows > 0;
    }

    public async Task<int> DeleteUnitAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM UNITS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<IReadOnlyList<StockItemRow>> ListStockAsync(string tenantId, string branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<StockItemRow>(
            """
            SELECT s.ID, s.NAME, s.SKU, s.UNIT_ID, u.CODE AS UnitCode, s.ON_HAND, s.REORDER_LEVEL, s.UNIT_COST, s.IS_ACTIVE
            FROM STOCK_ITEMS s LEFT JOIN UNITS u ON u.ID = s.UNIT_ID
            WHERE s.TENANT_ID = :tenantId AND s.BRANCH_ID = :branchId ORDER BY s.NAME
            """,
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task<StockItemRow?> GetStockAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<StockItemRow>(
            """
            SELECT s.ID, s.NAME, s.SKU, s.UNIT_ID, u.CODE AS UnitCode, s.ON_HAND, s.REORDER_LEVEL, s.UNIT_COST, s.IS_ACTIVE
            FROM STOCK_ITEMS s LEFT JOIN UNITS u ON u.ID = s.UNIT_ID
            WHERE s.TENANT_ID = :tenantId AND s.ID = :id
            """,
            new OracleParams(new { tenantId, id }));
    }

    public async Task InsertStockAsync(string id, string tenantId, string branchId, UpsertStockItemRequest r, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO STOCK_ITEMS (ID, TENANT_ID, BRANCH_ID, NAME, SKU, UNIT_ID, ON_HAND, REORDER_LEVEL, UNIT_COST, IS_ACTIVE)
            VALUES (:id, :tenantId, :branchId, :Name, :Sku, :UnitId, 0, :ReorderLevel, :UnitCost, :IsActive)
            """,
            new OracleParams(new { id, tenantId, branchId, r.Name, r.Sku, r.UnitId, r.ReorderLevel, r.UnitCost, r.IsActive }));
    }

    public async Task<bool> UpdateStockAsync(string tenantId, string id, UpsertStockItemRequest r, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE STOCK_ITEMS SET NAME = :Name, SKU = :Sku, UNIT_ID = :UnitId, REORDER_LEVEL = :ReorderLevel,
                   UNIT_COST = :UnitCost, IS_ACTIVE = :IsActive, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id, r.Name, r.Sku, r.UnitId, r.ReorderLevel, r.UnitCost, r.IsActive }));
        return rows > 0;
    }

    public async Task<int> DeleteStockAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "UPDATE STOCK_ITEMS SET IS_ACTIVE = 0, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1 WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<RecipeDto?> GetRecipeAsync(string tenantId, string menuItemId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var recipeId = await db.QuerySingleOrDefaultAsync<string>(
            "SELECT ID FROM RECIPES WHERE TENANT_ID = :tenantId AND MENU_ITEM_ID = :menuItemId",
            new OracleParams(new { tenantId, menuItemId }));
        if (recipeId is null) return null;

        var yieldQty = await db.ExecuteScalarAsync<decimal>(
            "SELECT YIELD_QTY FROM RECIPES WHERE ID = :recipeId", new OracleParams(new { recipeId }));

        var lines = (await db.QueryAsync<RecipeLineDto>(
            """
            SELECT rl.STOCK_ITEM_ID AS StockItemId, s.NAME AS StockItemName, rl.QUANTITY AS Quantity
            FROM RECIPE_LINES rl LEFT JOIN STOCK_ITEMS s ON s.ID = rl.STOCK_ITEM_ID
            WHERE rl.RECIPE_ID = :recipeId
            """,
            new OracleParams(new { recipeId }))).AsList();

        return new RecipeDto(menuItemId, yieldQty, lines);
    }

    public async Task SetRecipeAsync(string tenantId, string menuItemId, decimal yieldQty, IReadOnlyList<RecipeLineInput> lines, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);

        var recipeId = await db.QuerySingleOrDefaultAsync<string>(
            "SELECT ID FROM RECIPES WHERE TENANT_ID = :tenantId AND MENU_ITEM_ID = :menuItemId",
            new OracleParams(new { tenantId, menuItemId }));

        if (recipeId is null)
        {
            recipeId = NewId();
            await db.ExecuteAsync(
                "INSERT INTO RECIPES (ID, TENANT_ID, MENU_ITEM_ID, YIELD_QTY) VALUES (:recipeId, :tenantId, :menuItemId, :yieldQty)",
                new OracleParams(new { recipeId, tenantId, menuItemId, yieldQty }));
        }
        else
        {
            await db.ExecuteAsync("UPDATE RECIPES SET YIELD_QTY = :yieldQty, UPDATED_AT = SYSTIMESTAMP WHERE ID = :recipeId",
                new OracleParams(new { recipeId, yieldQty }));
            await db.ExecuteAsync("DELETE FROM RECIPE_LINES WHERE RECIPE_ID = :recipeId", new OracleParams(new { recipeId }));
        }

        foreach (var line in lines)
        {
            await db.ExecuteAsync(
                "INSERT INTO RECIPE_LINES (ID, RECIPE_ID, STOCK_ITEM_ID, QUANTITY) VALUES (:id, :recipeId, :stockItemId, :quantity)",
                new OracleParams(new { id = NewId(), recipeId, stockItemId = line.StockItemId, quantity = line.Quantity }));
        }
    }

    public async Task<IReadOnlyList<SupplierDto>> ListSuppliersAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<SupplierDto>(
            "SELECT ID, NAME, PHONE, EMAIL, TAX_NO AS TaxNo, IS_ACTIVE FROM SUPPLIERS WHERE TENANT_ID = :tenantId ORDER BY NAME",
            new OracleParams(new { tenantId }));
        return rows.AsList();
    }

    public async Task InsertSupplierAsync(string id, string tenantId, CreateSupplierRequest r, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "INSERT INTO SUPPLIERS (ID, TENANT_ID, NAME, PHONE, EMAIL, TAX_NO) VALUES (:id, :tenantId, :Name, :Phone, :Email, :TaxNo)",
            new OracleParams(new { id, tenantId, r.Name, r.Phone, r.Email, r.TaxNo }));
    }

    public async Task<bool> UpdateSupplierAsync(string tenantId, string id, UpdateSupplierRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE SUPPLIERS
               SET NAME = :Name, PHONE = :Phone, EMAIL = :Email, TAX_NO = :TaxNo, IS_ACTIVE = :IsActive
             WHERE TENANT_ID = :tenantId AND ID = :id
            """,
            new OracleParams(new { tenantId, id, request.Name, request.Phone, request.Email, request.TaxNo, request.IsActive }));
        return rows > 0;
    }

    public async Task<int> DeleteSupplierAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "UPDATE SUPPLIERS SET IS_ACTIVE = 0 WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<string> CreatePurchaseAsync(string tenantId, string branchId, string userId, CreatePurchaseRequest r, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var id = NewId();
        decimal total = 0;
        await db.ExecuteAsync(
            """
            INSERT INTO PURCHASE_ORDERS (ID, TENANT_ID, BRANCH_ID, SUPPLIER_ID, STATUS, NOTE, CREATED_BY)
            VALUES (:id, :tenantId, :branchId, :supplierId, 'draft', :note, :userId)
            """,
            new OracleParams(new { id, tenantId, branchId, supplierId = r.SupplierId, note = r.Note, userId }));

        foreach (var line in r.Lines)
        {
            var lineTotal = line.Quantity * line.UnitCost;
            total += lineTotal;
            await db.ExecuteAsync(
                """
                INSERT INTO PURCHASE_LINES (ID, PURCHASE_ID, STOCK_ITEM_ID, QUANTITY, UNIT_COST, LINE_TOTAL)
                VALUES (:id, :purchaseId, :stockItemId, :quantity, :unitCost, :lineTotal)
                """,
                new OracleParams(new { id = NewId(), purchaseId = id, stockItemId = line.StockItemId, quantity = line.Quantity, unitCost = line.UnitCost, lineTotal }));
        }

        await db.ExecuteAsync("UPDATE PURCHASE_ORDERS SET TOTAL = :total WHERE ID = :id", new OracleParams(new { id, total }));
        return id;
    }

    public async Task<PurchaseDto?> GetPurchaseAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var header = await db.QuerySingleOrDefaultAsync<PurchaseHeader>(
            "SELECT ID, SUPPLIER_ID AS SupplierId, STATUS, TOTAL, NOTE FROM PURCHASE_ORDERS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
        if (header is null) return null;

        var lines = (await db.QueryAsync<PurchaseLineDto>(
            """
            SELECT pl.STOCK_ITEM_ID AS StockItemId, s.NAME AS StockItemName, pl.QUANTITY AS Quantity, pl.UNIT_COST AS UnitCost, pl.LINE_TOTAL AS LineTotal
            FROM PURCHASE_LINES pl LEFT JOIN STOCK_ITEMS s ON s.ID = pl.STOCK_ITEM_ID
            WHERE pl.PURCHASE_ID = :id
            """,
            new OracleParams(new { id }))).AsList();

        return new PurchaseDto(header.Id, header.SupplierId, header.Status, header.Total, header.Note, lines);
    }

    public async Task<IReadOnlyList<StockMovementDto>> ListMovementsAsync(string tenantId, string stockItemId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<StockMovementDto>(
            """
            SELECT ID, MOVE_TYPE AS MoveType, QUANTITY, UNIT_COST AS UnitCost, REF_TYPE AS RefType, NOTE, CREATED_AT
            FROM STOCK_MOVEMENTS WHERE TENANT_ID = :tenantId AND STOCK_ITEM_ID = :stockItemId ORDER BY CREATED_AT DESC
            """,
            new OracleParams(new { tenantId, stockItemId }));
        return rows.AsList();
    }
}
