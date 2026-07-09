namespace Ordevo.Modules.Print.Application;

public sealed record ReceiptLineDto(
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Note);

public sealed record ReceiptPaymentDto(
    string Method,
    decimal Amount,
    decimal TipAmount,
    string? Reference);

public sealed record ReceiptDocumentDto(
    string OrderId,
    long OrderNo,
    string? TableName,
    string OrderType,
    string Status,
    long? InvoiceNo,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal Total,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<ReceiptLineDto> Lines,
    IReadOnlyList<ReceiptPaymentDto> Payments,
    string PlainText,
    string Html);

public sealed record KitchenTicketLineDto(
    string Name,
    decimal Quantity,
    int CourseNo,
    string Status,
    string? Station,
    string? Note,
    string? Modifiers);

public sealed record KitchenTicketDocumentDto(
    string OrderId,
    long OrderNo,
    string? TableName,
    string OrderType,
    DateTimeOffset OpenedAt,
    IReadOnlyList<KitchenTicketLineDto> Lines,
    string PlainText,
    string Html);

public sealed record QueuePrintRequest(
    string? TerminalId,
    int Copies,
    string? PrinterName);

public sealed record PrintJobDto(
    string Id,
    string BranchId,
    string JobType,
    string OrderId,
    string? TerminalId,
    string Status,
    int Copies,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class ReceiptHeaderRow
{
    public string OrderId { get; set; } = default!;
    public long OrderNo { get; set; }
    public string? TableName { get; set; }
    public string OrderType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public long? InvoiceNo { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class ReceiptLineRow
{
    public string Name { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
}

public sealed class ReceiptPaymentRow
{
    public string Method { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal TipAmount { get; set; }
    public string? Reference { get; set; }
}

public sealed class KitchenTicketLineRow
{
    public string Name { get; set; } = default!;
    public decimal Quantity { get; set; }
    public int CourseNo { get; set; }
    public string Status { get; set; } = default!;
    public string? Station { get; set; }
    public string? Note { get; set; }
    public string? Modifiers { get; set; }
}

public sealed class PrintJobRow
{
    public string Id { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string JobType { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string? TerminalId { get; set; }
    public string Status { get; set; } = default!;
    public int Copies { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
