using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Kitchen.Application;

namespace Ordevo.Modules.Kitchen.Infrastructure;

public sealed class StationRepository(IDbConnectionFactory factory) : IStationRepository
{
    public async Task<IReadOnlyList<KdsStation>> ListAsync(string tenantId, string branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<KdsStation>(
            "SELECT ID, TENANT_ID, BRANCH_ID, NAME, CODE, SORT_ORDER, IS_ACTIVE FROM KDS_STATIONS WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId ORDER BY SORT_ORDER, NAME",
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task<KdsStation?> GetAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<KdsStation>(
            "SELECT ID, TENANT_ID, BRANCH_ID, NAME, CODE, SORT_ORDER, IS_ACTIVE FROM KDS_STATIONS WHERE TENANT_ID = :tenantId AND ID = :id",
            new OracleParams(new { tenantId, id }));
    }

    public async Task InsertAsync(KdsStation s, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "INSERT INTO KDS_STATIONS (ID, TENANT_ID, BRANCH_ID, NAME, CODE, SORT_ORDER, IS_ACTIVE) VALUES (:Id, :TenantId, :BranchId, :Name, :Code, :SortOrder, :IsActive)",
            new OracleParams(new { s.Id, s.TenantId, s.BranchId, s.Name, s.Code, s.SortOrder, s.IsActive }));
    }

    public async Task<bool> UpdateAsync(KdsStation s, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE KDS_STATIONS SET NAME = :Name, CODE = :Code, SORT_ORDER = :SortOrder, IS_ACTIVE = :IsActive,
                   UPDATED_AT = SYSTIMESTAMP, ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :TenantId AND ID = :Id
            """,
            new OracleParams(new { s.Id, s.TenantId, s.Name, s.Code, s.SortOrder, s.IsActive }));
        return rows > 0;
    }

    public async Task<int> DeleteAsync(string tenantId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync("DELETE FROM KDS_STATIONS WHERE TENANT_ID = :tenantId AND ID = :id", new OracleParams(new { tenantId, id }));
    }
}

public sealed class KdsRepository(IDbConnectionFactory factory) : IKdsRepository
{
    public async Task<IReadOnlyList<KdsItemRow>> GetBoardAsync(string tenantId, string branchId, string? stationCode, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<KdsItemRow>(
            """
            SELECT oi.ID AS OrderItemId, o.ID AS OrderId, o.ORDER_NO AS OrderNo, dt.NAME AS TableName,
                   oi.NAME_SNAPSHOT AS ItemName, oi.QUANTITY AS Quantity, oi.COURSE_NO AS CourseNo,
                   oi.STATUS AS Status, mi.PREP_STATION AS Station, oi.NOTE AS Note, oi.CREATED_AT AS CreatedAt,
                   (SELECT LISTAGG(m.NAME_SNAPSHOT, ', ') WITHIN GROUP (ORDER BY m.NAME_SNAPSHOT)
                      FROM ORDER_ITEM_MODIFIERS m WHERE m.ORDER_ITEM_ID = oi.ID) AS Modifiers,
                   CASE WHEN EXISTS (
                       SELECT 1
                         FROM ORDER_ITEMS prev
                        WHERE prev.ORDER_ID = oi.ORDER_ID
                          AND prev.ID <> oi.ID
                          AND prev.STATUS <> 'void'
                          AND prev.CREATED_AT < oi.CREATED_AT
                          AND (prev.STATUS IN ('in_kitchen','ready','served')
                               OR prev.CREATED_AT < oi.CREATED_AT - INTERVAL '90' SECOND)
                   ) THEN 1 ELSE 0 END AS IsAdditional
            FROM ORDER_ITEMS oi
            JOIN ORDERS o ON o.ID = oi.ORDER_ID
            LEFT JOIN DINING_TABLES dt ON dt.ID = o.TABLE_ID
            LEFT JOIN MENU_ITEMS mi ON mi.ID = oi.MENU_ITEM_ID
            WHERE o.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId
              AND o.STATUS = 'open'
              AND oi.STATUS IN ('pending','in_kitchen','ready')
              AND (:stationCode IS NULL OR mi.PREP_STATION = :stationCode)
            ORDER BY o.ORDER_NO, oi.COURSE_NO, oi.CREATED_AT
            """,
            new OracleParams(new { tenantId, branchId, stationCode }));
        return rows.AsList();
    }

    public async Task<DateTimeOffset> GetDatabaseNowAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleAsync<DateTimeOffset>("SELECT SYS_EXTRACT_UTC(SYSTIMESTAMP) FROM DUAL");
    }

    public async Task<KdsItemState?> GetItemStateAsync(string tenantId, string itemId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<KdsItemState>(
            """
            SELECT oi.ORDER_ID AS OrderId, oi.STATUS AS ItemStatus, o.STATUS AS OrderStatus
            FROM ORDER_ITEMS oi JOIN ORDERS o ON o.ID = oi.ORDER_ID
            WHERE oi.TENANT_ID = :tenantId AND oi.ID = :itemId
            """,
            new OracleParams(new { tenantId, itemId }));
    }

    public async Task<IReadOnlyList<string>> GetActiveItemIdsAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<string>(
            "SELECT ID FROM ORDER_ITEMS WHERE TENANT_ID = :tenantId AND ORDER_ID = :orderId AND STATUS IN ('pending','in_kitchen')",
            new OracleParams(new { tenantId, orderId }));
        return rows.AsList();
    }
}
