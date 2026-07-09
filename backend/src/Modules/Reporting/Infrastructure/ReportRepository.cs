using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Reporting.Application;

namespace Ordevo.Modules.Reporting.Infrastructure;

public interface IReportRepository
{
    Task<DailyStatsRow> DailyStatsAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default);
    Task<IReadOnlyList<TopItemRow>> TopItemsAsync(string tenantId, string branchId, DateTime start, DateTime end, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<HourlyRow>> HourlyAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default);
    Task<IReadOnlyList<CategorySalesRow>> CategorySalesAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentMethodRow>> PaymentMethodsAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<DailySummaryRow>> DailySummaryAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<MlExportRow>> MlExportAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default);

    Task ArchiveDailyAsync(string tenantId, string branchId, DateTime date, string userId, CancellationToken ct = default);
    Task<string?> DailyStatsJsonAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default);
    Task RefreshMaterializedViewAsync(CancellationToken ct = default);
}

public sealed class ReportRepository(IDbConnectionFactory factory) : IReportRepository
{
    public async Task<DailyStatsRow> DailyStatsAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleAsync<DailyStatsRow>(
            """
            SELECT COUNT(DISTINCT o.ID) AS OrderCount,
                   NVL(SUM(oi.LINE_TOTAL),0) AS Revenue,
                   NVL(SUM(oi.QUANTITY),0) AS ItemCount,
                   CASE WHEN COUNT(DISTINCT o.ID) = 0 THEN 0
                        ELSE ROUND(NVL(SUM(oi.LINE_TOTAL),0) / COUNT(DISTINCT o.ID), 2) END AS AvgTicket
            FROM ORDERS o
            LEFT JOIN ORDER_ITEMS oi ON oi.ORDER_ID = o.ID AND oi.STATUS <> 'void' AND oi.IS_COMP = 0
            WHERE o.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId
              AND o.STATUS = 'closed' AND TRUNC(o.CLOSED_AT) = :dt
            """,
            new OracleParams(new { tenantId, branchId, dt = date }));
    }

    public async Task<IReadOnlyList<TopItemRow>> TopItemsAsync(string tenantId, string branchId, DateTime start, DateTime end, int limit, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<TopItemRow>(
            """
            SELECT oi.NAME_SNAPSHOT AS Name, mc.NAME AS Category,
                   SUM(oi.QUANTITY) AS Quantity, SUM(oi.LINE_TOTAL) AS Revenue
            FROM ORDER_ITEMS oi
            JOIN ORDERS o ON o.ID = oi.ORDER_ID
            LEFT JOIN MENU_ITEMS mi ON mi.ID = oi.MENU_ITEM_ID
            LEFT JOIN MENU_CATEGORIES mc ON mc.ID = mi.CATEGORY_ID
            WHERE o.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId AND o.STATUS = 'closed'
              AND TRUNC(o.CLOSED_AT) BETWEEN :startDate AND :endDate AND oi.STATUS <> 'void'
            GROUP BY oi.NAME_SNAPSHOT, mc.NAME
            ORDER BY SUM(oi.QUANTITY) DESC
            FETCH FIRST :lim ROWS ONLY
            """,
            new OracleParams(new { tenantId, branchId, startDate = start, endDate = end, lim = limit }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<HourlyRow>> HourlyAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<HourlyRow>(
            """
            SELECT EXTRACT(HOUR FROM o.CLOSED_AT) AS Hour, COUNT(*) AS OrderCount, NVL(SUM(o.TOTAL),0) AS Revenue
            FROM ORDERS o
            WHERE o.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId AND o.STATUS = 'closed' AND TRUNC(o.CLOSED_AT) = :dt
            GROUP BY EXTRACT(HOUR FROM o.CLOSED_AT) ORDER BY 1
            """,
            new OracleParams(new { tenantId, branchId, dt = date }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<CategorySalesRow>> CategorySalesAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<CategorySalesRow>(
            """
            SELECT NVL(mc.NAME, '(Diğer)') AS Category, SUM(oi.QUANTITY) AS Quantity, SUM(oi.LINE_TOTAL) AS Revenue
            FROM ORDER_ITEMS oi
            JOIN ORDERS o ON o.ID = oi.ORDER_ID
            LEFT JOIN MENU_ITEMS mi ON mi.ID = oi.MENU_ITEM_ID
            LEFT JOIN MENU_CATEGORIES mc ON mc.ID = mi.CATEGORY_ID
            WHERE o.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId AND o.STATUS = 'closed'
              AND TRUNC(o.CLOSED_AT) BETWEEN :startDate AND :endDate AND oi.STATUS <> 'void'
            GROUP BY mc.NAME ORDER BY SUM(oi.LINE_TOTAL) DESC
            """,
            new OracleParams(new { tenantId, branchId, startDate = start, endDate = end }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<PaymentMethodRow>> PaymentMethodsAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<PaymentMethodRow>(
            """
            SELECT METHOD AS Method, NVL(SUM(AMOUNT),0) AS Amount, COUNT(*) AS Cnt
            FROM PAYMENTS
            WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND IS_VOIDED = 0
              AND TRUNC(CREATED_AT) BETWEEN :startDate AND :endDate
            GROUP BY METHOD ORDER BY METHOD
            """,
            new OracleParams(new { tenantId, branchId, startDate = start, endDate = end }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<DailySummaryRow>> DailySummaryAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<DailySummaryRow>(
            """
            SELECT BUSINESS_DATE AS BusinessDate, ORDER_COUNT AS OrderCount, REVENUE, TAX_TOTAL AS TaxTotal, DISCOUNT_TOTAL AS DiscountTotal
            FROM MV_DAILY_SALES
            WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND BUSINESS_DATE BETWEEN :startDate AND :endDate
            ORDER BY BUSINESS_DATE
            """,
            new OracleParams(new { tenantId, branchId, startDate = start, endDate = end }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<MlExportRow>> MlExportAsync(string tenantId, string branchId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<MlExportRow>(
            """
            SELECT TO_CHAR(o.CLOSED_AT, 'YYYY-MM-DD') AS Dt, EXTRACT(HOUR FROM o.CLOSED_AT) AS Hr,
                   TO_CHAR(o.CLOSED_AT, 'DY') AS Dow, oi.NAME_SNAPSHOT AS Item, mc.NAME AS Category,
                   oi.QUANTITY AS Qty, oi.LINE_TOTAL AS Total
            FROM ORDER_ITEMS oi
            JOIN ORDERS o ON o.ID = oi.ORDER_ID
            LEFT JOIN MENU_ITEMS mi ON mi.ID = oi.MENU_ITEM_ID
            LEFT JOIN MENU_CATEGORIES mc ON mc.ID = mi.CATEGORY_ID
            WHERE o.TENANT_ID = :tenantId AND o.BRANCH_ID = :branchId AND o.STATUS = 'closed'
              AND TRUNC(o.CLOSED_AT) BETWEEN :startDate AND :endDate AND oi.STATUS <> 'void'
            ORDER BY o.CLOSED_AT
            """,
            new OracleParams(new { tenantId, branchId, startDate = start, endDate = end }));
        return rows.AsList();
    }

    public async Task ArchiveDailyAsync(string tenantId, string branchId, DateTime date, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_business_date", date, DbType.Date);
        p.Add("p_user_id", userId);
        await db.ExecuteAsync("PKG_REPORTING.ARCHIVE_DAILY", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<string?> DailyStatsJsonAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<string>(
            "SELECT PKG_REPORTING.DAILY_STATS_JSON(:tenantId, :branchId, :dt) FROM DUAL",
            new OracleParams(new { tenantId, branchId, dt = date }));
    }

    public async Task RefreshMaterializedViewAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync("BEGIN PKG_REPORTING.REFRESH_MV; END;");
    }
}
