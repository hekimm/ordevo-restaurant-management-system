namespace Ordevo.Modules.EInvoice.Application;

public interface IEInvoiceProvider
{
    string Name { get; }

    Task<EInvoiceIssueResult> IssueAsync(EInvoiceDocumentModel document, CancellationToken ct = default);

    Task<EInvoiceStatusResult> GetStatusAsync(string externalId, CancellationToken ct = default);

    Task<EInvoiceCancelResult> CancelAsync(string externalId, string reason, CancellationToken ct = default);
}

public sealed record EInvoiceParty(
    string Name,
    string? TaxNumber,
    string? TaxOffice,
    string? Address,
    string? City,
    string? Country,
    string? Email,
    string? Phone);

public sealed record EInvoiceLine(
    string Name,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineNet,
    decimal VatAmount,
    decimal LineTotal);

public sealed record EInvoiceDocumentModel(
    string DocumentId,
    string DocumentType,
    string Scenario,
    string Currency,
    DateTimeOffset IssuedAt,
    long OrderNo,
    EInvoiceParty Seller,
    EInvoiceParty Buyer,
    IReadOnlyList<EInvoiceLine> Lines,
    decimal Subtotal,
    decimal TaxTotal,
    decimal GrandTotal,
    bool PricesIncludeVat,
    string? Notes);

public sealed record EInvoiceIssueResult(
    bool Success,
    string Status,
    string? ExternalId,
    string? Uuid,
    string? InvoiceNumber,
    string? PdfUrl,
    string? RawResponseJson,
    string? Error);

public sealed record EInvoiceStatusResult(bool Success, string Status, string? RawResponseJson, string? Error);

public sealed record EInvoiceCancelResult(bool Success, string Status, string? RawResponseJson, string? Error);
