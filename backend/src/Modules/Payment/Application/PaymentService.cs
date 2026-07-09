using Oracle.ManagedDataAccess.Client;
using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.Ordering.Application;

namespace Ordevo.Modules.Payment.Application;

public sealed class PaymentService(
    IPaymentProcedures procs,
    IPaymentReadRepository read,
    IOrderNotifier notifier)
{
    private static readonly HashSet<string> Methods =
        ["cash", "card", "meal_voucher", "on_account", "comp", "other"];

    public async Task<Result<PaymentResultDto>> AddPaymentAsync(
        string tenantId, string orderId, string userId, AddPaymentRequest r, CancellationToken ct = default)
    {
        if (!Methods.Contains(r.Method)) return Error.Validation("payment.bad_method", "Geçersiz ödeme yöntemi.");
        if (r.Amount < 0) return Error.Validation("payment.bad_amount", "Tutar negatif olamaz.");

        try
        {
            var (paymentId, closed, change, balance) =
                await procs.ProcessPaymentAsync(orderId, r.Method, r.Amount, r.Tip, r.Reference, userId, ct);

            await notifier.OrderChangedAsync(tenantId, orderId, closed ? "paid_closed" : "payment", ct);
            if (closed) await notifier.TablesChangedAsync(tenantId, ct);

            var totals = await read.GetOrderTotalsAsync(tenantId, orderId, ct);
            var paid = await read.GetPaidTotalAsync(orderId, ct);
            var payments = await LoadLinesAsync(tenantId, orderId, ct);

            return new PaymentResultDto(orderId, paymentId, closed, change, balance,
                totals?.Total ?? 0, paid, payments);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<PaymentsViewDto>> GetPaymentsAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        var totals = await read.GetOrderTotalsAsync(tenantId, orderId, ct);
        if (totals is null) return Error.NotFound("order.not_found", "Adisyon bulunamadı.");

        var paid = await read.GetPaidTotalAsync(orderId, ct);
        var payments = await LoadLinesAsync(tenantId, orderId, ct);
        return new PaymentsViewDto(orderId, totals.Total, paid, Math.Max(totals.Total - paid, 0), totals.Status, payments);
    }

    public async Task<Result<PaymentsViewDto>> VoidPaymentAsync(string tenantId, string paymentId, string userId, CancellationToken ct = default)
    {
        var orderId = await read.GetOrderIdOfPaymentAsync(tenantId, paymentId, ct);
        if (orderId is null) return Error.NotFound("payment.not_found", "Ödeme bulunamadı.");

        try
        {
            await procs.VoidPaymentAsync(paymentId, userId, ct);
            await notifier.OrderChangedAsync(tenantId, orderId, "payment_void", ct);
            return await GetPaymentsAsync(tenantId, orderId, ct);
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<string>> RefundAsync(
        string tenantId, string orderId, string userId, RefundRequest r, CancellationToken ct = default)
    {
        if (r.Amount <= 0) return Error.Validation("refund.bad_amount", "İade tutarı pozitif olmalı.");
        if (await read.GetOrderTotalsAsync(tenantId, orderId, ct) is null)
            return Error.NotFound("order.not_found", "Adisyon bulunamadı.");

        try
        {
            var refundId = await procs.RefundAsync(orderId, r.PaymentId, r.Amount, r.Reason, userId, ct);
            await notifier.OrderChangedAsync(tenantId, orderId, "refund", ct);
            return refundId;
        }
        catch (OracleException ex) when (TryBusiness(ex, out var error)) { return error; }
    }

    public async Task<Result<InvoiceDto>> GetInvoiceAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        var inv = await read.GetInvoiceAsync(tenantId, orderId, ct);
        return inv is null
            ? Error.NotFound("invoice.not_found", "Fiş/fatura bulunamadı.")
            : new InvoiceDto(inv.Id, inv.InvoiceNo, inv.InvoiceType, inv.Subtotal, inv.TaxTotal, inv.Total, inv.CreatedAt);
    }

    private async Task<IReadOnlyList<PaymentLineDto>> LoadLinesAsync(string tenantId, string orderId, CancellationToken ct)
        => (await read.ListPaymentsAsync(tenantId, orderId, ct))
            .Select(p => new PaymentLineDto(p.Id, p.Method, p.Amount, p.TipAmount, p.Reference, p.IsVoided, p.CreatedAt))
            .ToList();

    private static bool TryBusiness(OracleException ex, out Error error)
    {
        if (ex.Number is >= 20101 and <= 20110)
        {
            var message = ex.Message.Split('\n')[0].Replace($"ORA-{ex.Number}:", "").Trim();
            error = ex.Number switch
            {
                20101 or 20103 => Error.NotFound("payment.not_found", "Kayıt bulunamadı."),
                20105 or 20106 => Error.Conflict("payment.order_state", "Adisyon durumu uygun değil."),
                _ => Error.Validation("payment.rule", string.IsNullOrWhiteSpace(message) ? "Ödeme kuralı ihlali." : message)
            };
            return true;
        }
        error = Error.Failure("payment.db", "Veritabanı hatası.");
        return false;
    }
}
