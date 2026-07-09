namespace Ordevo.Modules.Fiscal.Application;

public sealed record FiscalPaymentRequest(
    string Method,
    decimal Amount,
    decimal Tip = 0,
    string? TerminalId = null,
    string? Currency = "TRY",
    bool IssueEInvoice = false,
    string? DocumentType = null,
    string? BuyerName = null,
    string? BuyerTaxNumber = null,
    string? BuyerTaxOffice = null,
    string? BuyerAddress = null,
    string? BuyerCity = null,
    string? BuyerEmail = null,
    string? Notes = null);

public sealed record ManualCardOverrideRequest(
    string Method,
    decimal Amount,
    decimal Tip,
    string Reason,
    string? Reference = null);

public sealed record TerminalTestSaleRequest(decimal Amount = 0.01m, string Currency = "TRY");

public sealed record FiscalPaymentResultDto(
    string FiscalTransactionId,
    string Status,
    string UserMessage,
    string? CommandId,
    string? PaymentId,
    bool OrderClosed,
    decimal Change,
    decimal Balance,
    string? ProviderReference,
    string? AuthorizationCode,
    string? Rrn,
    string? FiscalReceiptNo,
    string? EInvoiceDocumentId,
    string? EInvoiceStatus);

public sealed record FiscalOverviewDto(
    string PaymentTerminalProvider,
    string EAdisyonProvider,
    bool EAdisyonEnabled,
    int ActiveTerminalCount,
    int OpenCommandCount,
    IReadOnlyList<FiscalTerminalDto> Terminals,
    IReadOnlyList<FiscalTransactionDto> RecentTransactions);

public sealed record FiscalTerminalDto(
    string Id,
    string Name,
    string TerminalType,
    string? ProviderTerminalId,
    string ConnectionMode,
    bool IsActive,
    DateTimeOffset? LastSeenAt);

public sealed record FiscalTransactionDto(
    string Id,
    string? BranchId,
    string? OrderId,
    string? PaymentId,
    string? CommandId,
    string? TerminalId,
    string Provider,
    string Method,
    decimal Amount,
    decimal TipAmount,
    string Currency,
    string Status,
    string? AuthorizationCode,
    string? BatchNo,
    string? Stan,
    string? Rrn,
    string? FiscalReceiptNo,
    string? ZNo,
    string? DeviceSerial,
    string? DocumentUuid,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed class FiscalTransactionRow
{
    public string Id { get; set; } = default!;
    public string? BranchId { get; set; }
    public string? OrderId { get; set; }
    public string? PaymentId { get; set; }
    public string? CommandId { get; set; }
    public string? TerminalId { get; set; }
    public string Provider { get; set; } = default!;
    public string Method { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal TipAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = default!;
    public string? AuthorizationCode { get; set; }
    public string? BatchNo { get; set; }
    public string? Stan { get; set; }
    public string? Rrn { get; set; }
    public string? FiscalReceiptNo { get; set; }
    public string? ZNo { get; set; }
    public string? DeviceSerial { get; set; }
    public string? DocumentUuid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed record FiscalTransactionDraft(
    string TenantId,
    string? BranchId,
    string? OrderId,
    string? TerminalId,
    string Provider,
    string Method,
    decimal Amount,
    decimal Tip,
    string Currency,
    string Status,
    string RequestPayload,
    string? CreatedBy);
