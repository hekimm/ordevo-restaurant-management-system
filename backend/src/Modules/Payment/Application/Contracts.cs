namespace Ordevo.Modules.Payment.Application;

public sealed class PaymentRow
{
    public string Id { get; set; } = default!;
    public string Method { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal TipAmount { get; set; }
    public string? Reference { get; set; }
    public bool IsVoided { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class InvoiceRow
{
    public string Id { get; set; } = default!;
    public long InvoiceNo { get; set; }
    public string InvoiceType { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class OrderTotals
{
    public decimal Total { get; set; }
    public string Status { get; set; } = default!;
}

public sealed record AddPaymentRequest(string Method, decimal Amount, decimal Tip = 0, string? Reference = null);
public sealed record RefundRequest(string? PaymentId, decimal Amount, string? Reason);

public sealed record PaymentLineDto(string Id, string Method, decimal Amount, decimal TipAmount, string? Reference, bool IsVoided, DateTimeOffset CreatedAt);

public sealed record PaymentResultDto(
    string OrderId, string PaymentId, bool Closed, decimal Change, decimal Balance,
    decimal OrderTotal, decimal PaidTotal, IReadOnlyList<PaymentLineDto> Payments);

public sealed record PaymentsViewDto(
    string OrderId, decimal OrderTotal, decimal PaidTotal, decimal Balance, string OrderStatus,
    IReadOnlyList<PaymentLineDto> Payments);

public sealed record InvoiceDto(string Id, long InvoiceNo, string Type, decimal Subtotal, decimal TaxTotal, decimal Total, DateTimeOffset CreatedAt);
