using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Shift.Application;

namespace Ordevo.Modules.Shift.Infrastructure;

public sealed class ShiftRepository(IDbConnectionFactory factory) : IShiftRepository
{
    private const string SessionCols =
        "ID, BRANCH_ID AS BranchId, REGISTER_ID AS RegisterId, STATUS, OPENING_AMOUNT AS OpeningAmount, OPENED_AT AS OpenedAt, " +
        "CLOSING_COUNTED AS ClosingCounted, CLOSING_EXPECTED AS ClosingExpected, DIFFERENCE, CLOSED_AT AS ClosedAt";

    public async Task<IReadOnlyList<RegisterDto>> ListRegistersAsync(string tenantId, string branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<RegisterDto>(
            "SELECT ID, NAME, IS_ACTIVE FROM CASH_REGISTERS WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId ORDER BY NAME",
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task InsertRegisterAsync(string id, string tenantId, string branchId, string name, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            "INSERT INTO CASH_REGISTERS (ID, TENANT_ID, BRANCH_ID, NAME) VALUES (:id, :tenantId, :branchId, :name)",
            new OracleParams(new { id, tenantId, branchId, name }));
    }

    public async Task<bool> UpdateRegisterAsync(string tenantId, string branchId, string id, UpdateRegisterRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE CASH_REGISTERS
               SET NAME = :Name, IS_ACTIVE = :IsActive
             WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND ID = :id
            """,
            new OracleParams(new { tenantId, branchId, id, request.Name, request.IsActive }));
        return rows > 0;
    }

    public async Task<int> DeleteRegisterAsync(string tenantId, string branchId, string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "UPDATE CASH_REGISTERS SET IS_ACTIVE = 0 WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND ID = :id",
            new OracleParams(new { tenantId, branchId, id }));
    }

    public async Task<SessionRow?> GetSessionAsync(string tenantId, string sessionId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<SessionRow>(
            $"SELECT {SessionCols} FROM REGISTER_SESSIONS WHERE TENANT_ID = :tenantId AND ID = :sessionId",
            new OracleParams(new { tenantId, sessionId }));
    }

    public async Task<SessionRow?> GetOpenSessionForRegisterAsync(string tenantId, string registerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<SessionRow>(
            $"SELECT {SessionCols} FROM REGISTER_SESSIONS WHERE TENANT_ID = :tenantId AND REGISTER_ID = :registerId AND STATUS = 'open'",
            new OracleParams(new { tenantId, registerId }));
    }

    public async Task<ZReportDto?> GetZReportAsync(string tenantId, string sessionId, CancellationToken ct = default)
    {
        var s = await GetSessionAsync(tenantId, sessionId, ct);
        if (s is null) return null;

        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new OracleParams(new { branch = s.BranchId, opened = s.OpenedAt, closed = (object?)s.ClosedAt, sessionId });

        var flow = await db.ExecuteScalarAsync<decimal>(
            """
            SELECT NVL(SUM(AMOUNT),0) FROM CASH_MOVEMENTS
            WHERE BRANCH_ID = :branch AND CREATED_AT >= :opened AND (:closed IS NULL OR CREATED_AT <= :closed)
              AND MOVE_TYPE IN ('sale','refund','payin','payout','void')
            """, p);
        var expected = s.OpeningAmount + flow;

        var payInOut = await db.QuerySingleAsync<PayInOut>(
            """
            SELECT NVL(SUM(CASE WHEN MOVE_TYPE = 'payin'  THEN AMOUNT  ELSE 0 END),0) AS PayIn,
                   NVL(SUM(CASE WHEN MOVE_TYPE = 'payout' THEN -AMOUNT ELSE 0 END),0) AS PayOut
            FROM CASH_MOVEMENTS WHERE SESSION_ID = :sessionId
            """, p);

        var sales = await db.QuerySingleAsync<SalesAgg>(
            """
            SELECT COUNT(*) AS OrderCount, NVL(SUM(TOTAL),0) AS GrossSales
            FROM ORDERS
            WHERE BRANCH_ID = :branch AND STATUS = 'closed'
              AND CLOSED_AT >= :opened AND (:closed IS NULL OR CLOSED_AT <= :closed)
            """, p);

        var breakdown = (await db.QueryAsync<PaymentBreakdownRow>(
            """
            SELECT METHOD AS Method, NVL(SUM(AMOUNT),0) AS Amount, COUNT(*) AS Cnt
            FROM PAYMENTS
            WHERE BRANCH_ID = :branch AND IS_VOIDED = 0
              AND CREATED_AT >= :opened AND (:closed IS NULL OR CREATED_AT <= :closed)
            GROUP BY METHOD ORDER BY METHOD
            """, p))
            .Select(r => new PaymentBreakdownDto(r.Method, r.Amount, r.Cnt)).ToList();

        return new ZReportDto(
            s.Id, s.RegisterId, s.Status, s.OpenedAt, s.ClosedAt,
            s.OpeningAmount, expected, s.ClosingCounted, s.Difference,
            payInOut.PayIn, payInOut.PayOut, sales.OrderCount, sales.GrossSales, breakdown);
    }

    private sealed class PayInOut { public decimal PayIn { get; set; } public decimal PayOut { get; set; } }
    private sealed class SalesAgg { public int OrderCount { get; set; } public decimal GrossSales { get; set; } }
    private sealed class PaymentBreakdownRow { public string Method { get; set; } = default!; public decimal Amount { get; set; } public int Cnt { get; set; } }
}
