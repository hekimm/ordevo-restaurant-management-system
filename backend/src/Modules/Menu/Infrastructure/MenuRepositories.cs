using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Menu.Application;
using Ordevo.Modules.Menu.Domain;

namespace Ordevo.Modules.Menu.Infrastructure;

public sealed class CategoryRepository(IDbConnectionFactory factory) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Category>(
            "SELECT ID, TENANT_ID, BRANCH_ID, NAME, COLOR, SORT_ORDER, IS_ACTIVE FROM MENU_CATEGORIES WHERE TENANT_ID = :tenantId ORDER BY SORT_ORDER, NAME",
            new OracleParams(new { tenantId }));
        return rows.AsList();
    }

    public async Task<Category?> GetAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Category>(
            "SELECT ID, TENANT_ID, BRANCH_ID, NAME, COLOR, SORT_ORDER, IS_ACTIVE FROM MENU_CATEGORIES WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task InsertAsync(Category c, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO MENU_CATEGORIES (ID, TENANT_ID, BRANCH_ID, NAME, COLOR, SORT_ORDER, IS_ACTIVE)
            VALUES (:Id, :TenantId, :BranchId, :Name, :Color, :SortOrder, :IsActive)
            """,
            new OracleParams(new { c.Id, c.TenantId, c.BranchId, c.Name, c.Color, c.SortOrder, c.IsActive }));
    }

    public async Task<bool> UpdateAsync(Category c, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE MENU_CATEGORIES
               SET NAME = :Name, COLOR = :Color, SORT_ORDER = :SortOrder, IS_ACTIVE = :IsActive,
                   UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :TenantId AND ID = :Id
            """,
            new OracleParams(new { c.Id, c.TenantId, c.Name, c.Color, c.SortOrder, c.IsActive }));
        return rows > 0;
    }

    public async Task<int> DeleteAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM MENU_CATEGORIES WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<int> CountItemsAsync(string tenantId, string categoryId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MENU_ITEMS WHERE TENANT_ID = :tenantId AND CATEGORY_ID = :categoryId",
            new OracleParams(new { tenantId, categoryId }));
    }
}

public sealed class MenuItemRepository(IDbConnectionFactory factory) : IMenuItemRepository
{
    private const string Cols =
        "ID, TENANT_ID, CATEGORY_ID, NAME, DESCRIPTION, PRICE, VAT_RATE, SKU, IMAGE_URL, PREP_STATION, SORT_ORDER, IS_ACTIVE";

    public async Task<IReadOnlyList<MenuItem>> ListAsync(string tenantId, string? categoryId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var sql = $"SELECT {Cols} FROM MENU_ITEMS WHERE TENANT_ID = :tenantId"
                + (categoryId is null ? "" : " AND CATEGORY_ID = :categoryId")
                + " ORDER BY SORT_ORDER, NAME";
        var rows = await db.QueryAsync<MenuItem>(sql, new OracleParams(new { tenantId, categoryId }));
        return rows.AsList();
    }

    public async Task<MenuItem?> GetAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<MenuItem>(
            $"SELECT {Cols} FROM MENU_ITEMS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task InsertAsync(MenuItem i, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO MENU_ITEMS (ID, TENANT_ID, CATEGORY_ID, NAME, DESCRIPTION, PRICE, VAT_RATE, SKU, IMAGE_URL, PREP_STATION, SORT_ORDER, IS_ACTIVE)
            VALUES (:Id, :TenantId, :CategoryId, :Name, :Description, :Price, :VatRate, :Sku, :ImageUrl, :PrepStation, :SortOrder, :IsActive)
            """,
            new OracleParams(new
            {
                i.Id, i.TenantId, i.CategoryId, i.Name, i.Description, i.Price, i.VatRate,
                i.Sku, i.ImageUrl, i.PrepStation, i.SortOrder, i.IsActive
            }));
    }

    public async Task<bool> UpdateAsync(MenuItem i, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE MENU_ITEMS
               SET CATEGORY_ID = :CategoryId, NAME = :Name, DESCRIPTION = :Description, PRICE = :Price,
                   VAT_RATE = :VatRate, SKU = :Sku, IMAGE_URL = :ImageUrl, PREP_STATION = :PrepStation,
                   SORT_ORDER = :SortOrder, IS_ACTIVE = :IsActive,
                   UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :TenantId AND ID = :Id
            """,
            new OracleParams(new
            {
                i.Id, i.TenantId, i.CategoryId, i.Name, i.Description, i.Price, i.VatRate,
                i.Sku, i.ImageUrl, i.PrepStation, i.SortOrder, i.IsActive
            }));
        return rows > 0;
    }

    public async Task<int> DeleteAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM MENU_ITEMS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task SetModifierGroupsAsync(string itemId, IEnumerable<string> groupIds, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync("DELETE FROM ITEM_MODIFIER_GROUPS WHERE ITEM_ID = :itemId", new OracleParams(new { itemId }));
        var sortOrder = 0;
        foreach (var groupId in groupIds)
        {
            await db.ExecuteAsync(
                "INSERT INTO ITEM_MODIFIER_GROUPS (ITEM_ID, GROUP_ID, SORT_ORDER) VALUES (:itemId, :groupId, :sortOrder)",
                new OracleParams(new { itemId, groupId, sortOrder }));
            sortOrder++;
        }
    }

    public async Task<IReadOnlyList<string>> GetModifierGroupIdsAsync(string itemId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<string>(
            "SELECT GROUP_ID FROM ITEM_MODIFIER_GROUPS WHERE ITEM_ID = :itemId ORDER BY SORT_ORDER",
            new OracleParams(new { itemId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ItemModifierLink>> GetAllModifierLinksAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<ItemModifierLink>(
            """
            SELECT img.ITEM_ID AS ItemId, img.GROUP_ID AS GroupId
            FROM ITEM_MODIFIER_GROUPS img
            JOIN MENU_ITEMS mi ON mi.ID = img.ITEM_ID
            WHERE mi.TENANT_ID = :tenantId
            ORDER BY img.ITEM_ID, img.SORT_ORDER
            """,
            new OracleParams(new { tenantId }));
        return rows.AsList();
    }
}

public sealed class ModifierRepository(IDbConnectionFactory factory) : IModifierRepository
{
    public async Task<IReadOnlyList<ModifierGroup>> ListGroupsAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<ModifierGroup>(
            "SELECT ID, TENANT_ID, NAME, MIN_SELECT, MAX_SELECT, IS_REQUIRED FROM MODIFIER_GROUPS WHERE TENANT_ID = :tenantId ORDER BY NAME",
            new OracleParams(new { tenantId }));
        return rows.AsList();
    }

    public async Task<ModifierGroup?> GetGroupAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<ModifierGroup>(
            "SELECT ID, TENANT_ID, NAME, MIN_SELECT, MAX_SELECT, IS_REQUIRED FROM MODIFIER_GROUPS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task InsertGroupAsync(ModifierGroup g, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO MODIFIER_GROUPS (ID, TENANT_ID, NAME, MIN_SELECT, MAX_SELECT, IS_REQUIRED)
            VALUES (:Id, :TenantId, :Name, :MinSelect, :MaxSelect, :IsRequired)
            """,
            new OracleParams(new { g.Id, g.TenantId, g.Name, g.MinSelect, g.MaxSelect, g.IsRequired }));
    }

    public async Task<bool> UpdateGroupAsync(ModifierGroup g, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE MODIFIER_GROUPS
               SET NAME = :Name, MIN_SELECT = :MinSelect, MAX_SELECT = :MaxSelect, IS_REQUIRED = :IsRequired,
                   UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :TenantId AND ID = :Id
            """,
            new OracleParams(new { g.Id, g.TenantId, g.Name, g.MinSelect, g.MaxSelect, g.IsRequired }));
        return rows > 0;
    }

    public async Task<int> DeleteGroupAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM MODIFIER_GROUPS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<IReadOnlyList<Modifier>> ListModifiersAsync(string tenantId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Modifier>(
            "SELECT ID, TENANT_ID, GROUP_ID, NAME, PRICE_DELTA, SORT_ORDER, IS_ACTIVE FROM MODIFIERS WHERE TENANT_ID = :tenantId ORDER BY SORT_ORDER, NAME",
            new OracleParams(new { tenantId }));
        return rows.AsList();
    }

    public async Task InsertModifierAsync(Modifier m, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO MODIFIERS (ID, TENANT_ID, GROUP_ID, NAME, PRICE_DELTA, SORT_ORDER, IS_ACTIVE)
            VALUES (:Id, :TenantId, :GroupId, :Name, :PriceDelta, :SortOrder, :IsActive)
            """,
            new OracleParams(new { m.Id, m.TenantId, m.GroupId, m.Name, m.PriceDelta, m.SortOrder, m.IsActive }));
    }

    public async Task<bool> UpdateModifierAsync(Modifier m, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE MODIFIERS
               SET NAME = :Name, PRICE_DELTA = :PriceDelta, SORT_ORDER = :SortOrder, IS_ACTIVE = :IsActive,
                   UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :TenantId AND ID = :Id
            """,
            new OracleParams(new { m.Id, m.TenantId, m.Name, m.PriceDelta, m.SortOrder, m.IsActive }));
        return rows > 0;
    }

    public async Task<int> DeleteModifierAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM MODIFIERS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }
}

public sealed class BarcodeRepository(IDbConnectionFactory factory) : IBarcodeRepository
{
    public async Task AddAsync(string id, string tenantId, string menuItemId, string barcode, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "INSERT INTO ITEM_BARCODES (ID, TENANT_ID, MENU_ITEM_ID, BARCODE) VALUES (:id, :tenantId, :menuItemId, :barcode)",
            new OracleParams(new { id, tenantId, menuItemId, barcode }));
    }

    public async Task<BarcodeLookupDto?> LookupAsync(string tenantId, string barcode, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<BarcodeLookupDto>(
            """
            SELECT mi.ID AS MenuItemId, mi.NAME AS Name, mi.PRICE AS Price
            FROM ITEM_BARCODES b
            JOIN MENU_ITEMS mi ON mi.ID = b.MENU_ITEM_ID
            WHERE b.TENANT_ID = :tenantId AND b.BARCODE = :barcode
            """,
            new OracleParams(new { tenantId, barcode }));
    }
}
