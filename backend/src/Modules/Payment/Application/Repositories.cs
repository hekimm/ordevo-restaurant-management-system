namespace Ordevo.Modules.Payment.Application;

public interface IPaymentReadRepository
{
    Task<OrderTotals?> GetOrderTotalsAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<decimal> GetPaidTotalAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentRow>> ListPaymentsAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<InvoiceRow?> GetInvoiceAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<string?> GetOrderIdOfPaymentAsync(string tenantId, string paymentId, CancellationToken ct = default);
}

public interface IPaymentProcedures
{
    Task<(string PaymentId, bool Closed, decimal Change, decimal Balance)> ProcessPaymentAsync(
        string orderId, string method, decimal amount, decimal tip, string? reference, string userId, CancellationToken ct = default);

    Task VoidPaymentAsync(string paymentId, string userId, CancellationToken ct = default);

    Task<string> RefundAsync(string orderId, string? paymentId, decimal amount, string? reason, string userId, CancellationToken ct = default);
}
