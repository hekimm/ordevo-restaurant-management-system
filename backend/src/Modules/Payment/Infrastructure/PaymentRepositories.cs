using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Payment.Application;

namespace Ordevo.Modules.Payment.Infrastructure;

public sealed class PaymentReadRepository(IDbConnectionFactory factory) : IPaymentReadRepository
{
    public async Task<OrderTotals?> GetOrderTotalsAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<OrderTotals>(
            "SELECT TOTAL AS Total, STATUS AS Status FROM ORDERS WHERE TENANT_ID = :tenantId AND ID = :orderId",
            new OracleParams(new { tenantId, orderId }));
    }

    public async Task<decimal> GetPaidTotalAsync(string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<decimal>(
            "SELECT NVL(SUM(AMOUNT),0) FROM PAYMENTS WHERE ORDER_ID = :orderId AND IS_VOIDED = 0",
            new OracleParams(new { orderId }));
    }

    public async Task<IReadOnlyList<PaymentRow>> ListPaymentsAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<PaymentRow>(
            """
            SELECT ID, METHOD, AMOUNT, TIP_AMOUNT, REFERENCE, IS_VOIDED, CREATED_AT
            FROM PAYMENTS WHERE TENANT_ID = :tenantId AND ORDER_ID = :orderId ORDER BY CREATED_AT
            """,
            new OracleParams(new { tenantId, orderId }));
        return rows.AsList();
    }

    public async Task<InvoiceRow?> GetInvoiceAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<InvoiceRow>(
            """
            SELECT ID, INVOICE_NO, INVOICE_TYPE, SUBTOTAL, TAX_TOTAL, TOTAL, CREATED_AT
            FROM INVOICES WHERE TENANT_ID = :tenantId AND ORDER_ID = :orderId
            """,
            new OracleParams(new { tenantId, orderId }));
    }

    public async Task<string?> GetOrderIdOfPaymentAsync(string tenantId, string paymentId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<string>(
            "SELECT ORDER_ID FROM PAYMENTS WHERE ID = :paymentId AND TENANT_ID = :tenantId",
            new OracleParams(new { paymentId, tenantId }));
    }
}

public sealed class PaymentProcedures(IDbConnectionFactory factory) : IPaymentProcedures
{
    public async Task<(string PaymentId, bool Closed, decimal Change, decimal Balance)> ProcessPaymentAsync(
        string orderId, string method, decimal amount, decimal tip, string? reference, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_order_id", orderId);
        p.Add("p_method", method);
        p.Add("p_amount", amount);
        p.Add("p_tip", tip);
        p.Add("p_reference", reference);
        p.Add("p_user_id", userId);
        p.Add("p_payment_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        p.Add("p_closed", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("p_change", dbType: DbType.Decimal, direction: ParameterDirection.Output);
        p.Add("p_balance", dbType: DbType.Decimal, direction: ParameterDirection.Output);
        await db.ExecuteAsync("PKG_PAYMENT.PROCESS_PAYMENT", p, commandType: CommandType.StoredProcedure);
        return (p.Get<string>("p_payment_id"), p.Get<int>("p_closed") == 1, p.Get<decimal>("p_change"), p.Get<decimal>("p_balance"));
    }

    public async Task VoidPaymentAsync(string paymentId, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_payment_id", paymentId);
        p.Add("p_user_id", userId);
        await db.ExecuteAsync("PKG_PAYMENT.VOID_PAYMENT", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<string> RefundAsync(string orderId, string? paymentId, decimal amount, string? reason, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_order_id", orderId);
        p.Add("p_payment_id", paymentId);
        p.Add("p_amount", amount);
        p.Add("p_reason", reason);
        p.Add("p_user_id", userId);
        p.Add("p_refund_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_PAYMENT.REFUND", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_refund_id");
    }
}
