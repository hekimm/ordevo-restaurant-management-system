namespace Ordevo.Modules.Fiscal.Application;

public interface IPaymentTerminalProvider
{
    string Name { get; }

    Task<PaymentTerminalResult> SaleAsync(PaymentTerminalSaleRequest request, CancellationToken ct = default);

    Task<PaymentTerminalResult> RefundAsync(PaymentTerminalRefundRequest request, CancellationToken ct = default);

    Task<PaymentTerminalResult> VoidAsync(PaymentTerminalVoidRequest request, CancellationToken ct = default);

    Task<PaymentTerminalResult> SettlementAsync(PaymentTerminalSettlementRequest request, CancellationToken ct = default);

    Task<PaymentTerminalStatusResult> GetStatusAsync(string terminalId, CancellationToken ct = default);
}

public interface IEAdisyonProvider
{
    string Name { get; }

    Task<EAdisyonNotifyResult> NotifyPaidAsync(EAdisyonNotifyRequest request, CancellationToken ct = default);
}

public sealed record PaymentTerminalSaleRequest(
    string TenantId,
    string? BranchId,
    string? OrderId,
    string TerminalId,
    decimal Amount,
    decimal Tip,
    string Currency,
    string Method,
    string IdempotencyKey,
    bool IsTest,
    string? TerminalName,
    IReadOnlyDictionary<string, string?> Metadata);

public sealed record PaymentTerminalRefundRequest(string TerminalId, decimal Amount, string Currency, string? ProviderReference);

public sealed record PaymentTerminalVoidRequest(string TerminalId, string ProviderReference);

public sealed record PaymentTerminalSettlementRequest(string TerminalId, DateOnly BusinessDate);

public sealed record PaymentTerminalResult(
    bool Success,
    string Status,
    string? ProviderReference,
    string? AuthorizationCode,
    string? BatchNo,
    string? Stan,
    string? Rrn,
    string? FiscalReceiptNo,
    string? ZNo,
    string? DeviceSerial,
    string? RawResponseJson,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static PaymentTerminalResult Failed(string code, string message, string? raw = null)
        => new(false, "failed", null, null, null, null, null, null, null, null, raw, code, message);
}

public sealed record PaymentTerminalStatusResult(
    bool Success,
    string Status,
    string? DeviceSerial,
    DateTimeOffset? LastSeenAt,
    string? RawResponseJson,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record EAdisyonNotifyRequest(
    string TenantId,
    string? BranchId,
    string OrderId,
    string? PaymentId,
    decimal Amount,
    string Currency,
    string FiscalTransactionId);

public sealed record EAdisyonNotifyResult(
    bool Success,
    string Status,
    string? ExternalId,
    string? RawResponseJson,
    string? ErrorCode,
    string? ErrorMessage);
