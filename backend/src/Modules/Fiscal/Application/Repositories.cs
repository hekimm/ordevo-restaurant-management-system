namespace Ordevo.Modules.Fiscal.Application;

public interface IFiscalTransactionRepository
{
    Task<string> CreateAsync(FiscalTransactionDraft draft, CancellationToken ct = default);

    Task AttachCommandAsync(string tenantId, string id, string commandId, string status, CancellationToken ct = default);

    Task CompleteAsync(
        string tenantId,
        string id,
        string? paymentId,
        PaymentTerminalResult terminalResult,
        string? documentUuid,
        string status,
        CancellationToken ct = default);

    Task CompleteManualAsync(
        string tenantId,
        string id,
        string paymentId,
        string reference,
        string status,
        CancellationToken ct = default);

    Task FailAsync(
        string tenantId,
        string id,
        string? code,
        string userMessage,
        string? responsePayload,
        CancellationToken ct = default);

    Task<IReadOnlyList<FiscalTransactionRow>> ListAsync(
        string tenantId,
        string? branchId,
        string? status,
        int take,
        CancellationToken ct = default);

    Task<FiscalTransactionRow?> GetAsync(string tenantId, string id, CancellationToken ct = default);
}
