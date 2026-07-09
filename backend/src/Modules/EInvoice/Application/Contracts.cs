namespace Ordevo.Modules.EInvoice.Application;

public sealed record IssueEInvoiceRequest(
    string? DocumentType,
    string? BuyerName,
    string? BuyerTaxNumber,
    string? BuyerTaxOffice,
    string? BuyerAddress,
    string? BuyerCity,
    string? BuyerEmail,
    string? Notes);

public sealed record CancelEInvoiceRequest(string Reason);

public sealed record EInvoiceDocumentDto(
    string Id, string? BranchId, string? OrderId, string? InvoiceId, string DocumentType,
    string Provider, string Scenario, string Status, string? ExternalId, string? Uuid,
    string? InvoiceNumber, string? BuyerName, string? BuyerTaxNo, string Currency,
    decimal Subtotal, decimal TaxTotal, decimal GrandTotal, string? PdfUrl, string? ErrorMessage,
    DateTimeOffset? IssuedAt, DateTimeOffset CreatedAt);

public sealed class EInvoiceDocumentRow
{
    public string Id { get; set; } = default!;
    public string? BranchId { get; set; }
    public string? OrderId { get; set; }
    public string? InvoiceId { get; set; }
    public string DocumentType { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string Scenario { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? ExternalId { get; set; }
    public string? Uuid { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? BuyerName { get; set; }
    public string? BuyerTaxNo { get; set; }
    public string Currency { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string? PdfUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class OrderInvoiceHeaderRow
{
    public string OrderId { get; set; } = default!;
    public long OrderNo { get; set; }
    public string? BranchId { get; set; }
    public string Status { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string SellerName { get; set; } = default!;
    public string? BranchName { get; set; }
    public string Currency { get; set; } = "TRY";
}

public sealed class OrderInvoiceLineRow
{
    public string Name { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ModifierTotal { get; set; }
    public decimal LineTotal { get; set; }
    public decimal VatRate { get; set; }
}
