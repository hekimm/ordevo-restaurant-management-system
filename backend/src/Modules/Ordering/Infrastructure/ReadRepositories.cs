using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Ordering.Application;
using Ordevo.Modules.Ordering.Domain;

namespace Ordevo.Modules.Ordering.Infrastructure;

public sealed class TableRepository(IDbConnectionFactory factory) : ITableRepository
{
    public async Task<IReadOnlyList<TableSection>> ListSectionsAsync(string tenantId, string branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<TableSection>(
            "SELECT ID, TENANT_ID, BRANCH_ID, NAME, SORT_ORDER FROM TABLE_SECTIONS WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId ORDER BY SORT_ORDER, NAME",
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task InsertSectionAsync(TableSection s, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "INSERT INTO TABLE_SECTIONS (ID, TENANT_ID, BRANCH_ID, NAME, SORT_ORDER) VALUES (:Id, :TenantId, :BranchId, :Name, :SortOrder)",
            new OracleParams(new { s.Id, s.TenantId, s.BranchId, s.Name, s.SortOrder }));
    }

    public async Task<TableSection?> GetSectionAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<TableSection>(
            "SELECT ID, TENANT_ID, BRANCH_ID, NAME, SORT_ORDER FROM TABLE_SECTIONS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<bool> UpdateSectionAsync(TableSection s, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE TABLE_SECTIONS
               SET NAME = :Name, SORT_ORDER = :SortOrder, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :TenantId AND ID = :Id
            """,
            new OracleParams(new { s.Id, s.TenantId, s.Name, s.SortOrder }));
        return rows > 0;
    }

    public async Task<int> DeleteSectionAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM TABLE_SECTIONS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task<IReadOnlyList<DiningTable>> ListTablesAsync(string tenantId, string branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<DiningTable>(
            """
            SELECT ID, TENANT_ID, BRANCH_ID, SECTION_ID, NAME, CAPACITY, STATUS, SORT_ORDER, IS_ACTIVE
            FROM DINING_TABLES WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId ORDER BY SORT_ORDER, NAME
            """,
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task<DiningTable?> GetTableAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<DiningTable>(
            "SELECT ID, TENANT_ID, BRANCH_ID, SECTION_ID, NAME, CAPACITY, STATUS, SORT_ORDER, IS_ACTIVE FROM DINING_TABLES WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task InsertTableAsync(DiningTable t, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO DINING_TABLES (ID, TENANT_ID, BRANCH_ID, SECTION_ID, NAME, CAPACITY, STATUS, SORT_ORDER, IS_ACTIVE)
            VALUES (:Id, :TenantId, :BranchId, :SectionId, :Name, :Capacity, 'idle', :SortOrder, :IsActive)
            """,
            new OracleParams(new { t.Id, t.TenantId, t.BranchId, t.SectionId, t.Name, t.Capacity, t.SortOrder, t.IsActive }));
    }

    public async Task<bool> UpdateTableAsync(DiningTable t, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE DINING_TABLES
               SET SECTION_ID = :SectionId, NAME = :Name, CAPACITY = :Capacity, SORT_ORDER = :SortOrder,
                   IS_ACTIVE = :IsActive, UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :TenantId AND ID = :Id
            """,
            new OracleParams(new { t.Id, t.TenantId, t.SectionId, t.Name, t.Capacity, t.SortOrder, t.IsActive }));
        return rows > 0;
    }

    public async Task<int> DeleteTableAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM DINING_TABLES WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }
}

public sealed class MenuPricing(IDbConnectionFactory factory) : IMenuPricing
{
    public async Task<MenuItemPrice?> GetItemAsync(string tenantId, string menuItemId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<MenuItemPrice>(
            "SELECT ID AS Id, NAME AS Name, PRICE AS Price, VAT_RATE AS VatRate FROM MENU_ITEMS WHERE TENANT_ID = :tenantId AND ID = :menuItemId AND IS_ACTIVE = 1",
            new OracleParams(new { tenantId, menuItemId }));
    }

    public async Task<IReadOnlyList<ModifierPrice>> GetModifiersAsync(string tenantId, IEnumerable<string> modifierIds, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var result = new List<ModifierPrice>();
        foreach (var id in modifierIds.Distinct())
        {
            var m = await db.QuerySingleOrDefaultAsync<ModifierPrice>(
                "SELECT ID AS Id, NAME AS Name, PRICE_DELTA AS PriceDelta FROM MODIFIERS WHERE TENANT_ID = :tenantId AND ID = :id AND IS_ACTIVE = 1",
                new OracleParams(new { tenantId, id }));
            if (m is not null) result.Add(m);
        }
        return result;
    }
}

public sealed class OrderReadRepository(IDbConnectionFactory factory) : IOrderReadRepository
{
    public async Task<Order?> GetOrderAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Order>(
            """
            SELECT ID, TENANT_ID, BRANCH_ID, ORDER_NO, TABLE_ID, ORDER_TYPE, STATUS, GUEST_COUNT,
                   SUBTOTAL, DISCOUNT_TOTAL, TAX_TOTAL, TOTAL, NOTE, OPENED_AT, CLOSED_AT
            FROM ORDERS WHERE TENANT_ID = :tenantId AND ID = :orderId
            """,
            new OracleParams(new { tenantId, orderId }));
    }

    public async Task<IReadOnlyList<OrderItem>> GetItemsAsync(string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<OrderItem>(
            """
            SELECT ID, ORDER_ID, MENU_ITEM_ID, NAME_SNAPSHOT, UNIT_PRICE, QUANTITY, MODIFIER_TOTAL,
                   LINE_TOTAL, VAT_RATE, COURSE_NO, STATUS, IS_COMP, NOTE
            FROM ORDER_ITEMS WHERE ORDER_ID = :orderId ORDER BY COURSE_NO, CREATED_AT
            """,
            new OracleParams(new { orderId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<OrderItemModifier>> GetItemModifiersAsync(string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<OrderItemModifier>(
            """
            SELECT m.ID, m.ORDER_ITEM_ID, m.MODIFIER_ID, m.NAME_SNAPSHOT, m.PRICE_DELTA
            FROM ORDER_ITEM_MODIFIERS m
            JOIN ORDER_ITEMS i ON i.ID = m.ORDER_ITEM_ID
            WHERE i.ORDER_ID = :orderId
            """,
            new OracleParams(new { orderId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> ListOrdersAsync(string tenantId, string branchId, string? status, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var sql = """
            SELECT o.ID AS Id, o.ORDER_NO AS OrderNo, o.TABLE_ID AS TableId,
                   o.ORDER_TYPE AS OrderType, o.STATUS AS Status, o.TOTAL AS Total, o.OPENED_AT AS OpenedAt,
                   COUNT(CASE WHEN oi.STATUS <> 'void' THEN 1 END) AS ItemCount
              FROM ORDERS o
              LEFT JOIN ORDER_ITEMS oi ON oi.ORDER_ID = o.ID
             WHERE o.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId
            """ + (status is null ? "" : " AND o.STATUS = :status") + """

             GROUP BY o.ID, o.ORDER_NO, o.TABLE_ID, o.ORDER_TYPE, o.STATUS, o.TOTAL, o.OPENED_AT
             ORDER BY o.OPENED_AT DESC
            """;
        // Materialize into a mutable row first: ODP.NET returns ORDER_NO / COUNT(...) as NUMBER (decimal),
        // and Dapper's positional-record binding does not coerce decimal->long/int. Setters do coerce.
        var rows = await db.QueryAsync<OrderSummaryRow>(sql, new OracleParams(new { tenantId, branchId, status }));
        return rows
            .Select(r => new OrderSummaryDto(r.Id, r.OrderNo, r.TableId, r.OrderType, r.Status, r.Total, r.OpenedAt, r.ItemCount))
            .ToList();
    }

    public async Task<string?> GetOrderIdOfItemAsync(string tenantId, string itemId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<string>(
            "SELECT ORDER_ID FROM ORDER_ITEMS WHERE ID = :itemId AND TENANT_ID = :tenantId",
            new OracleParams(new { itemId, tenantId }));
    }
}

// Mutable materialization target for ListOrdersAsync (Dapper coerces Oracle NUMBER via setters,
// whereas positional-record ctor binding of OrderSummaryDto does not).
internal sealed class OrderSummaryRow
{
    public string Id { get; set; } = default!;
    public long OrderNo { get; set; }
    public string? TableId { get; set; }
    public string OrderType { get; set; } = "dinein";
    public string Status { get; set; } = default!;
    public decimal Total { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public int ItemCount { get; set; }
}
