using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.EInvoice.Application;
using Ordevo.Modules.Integration.Application;
using Ordevo.Modules.Payment.Application;

namespace Ordevo.Modules.Fiscal.Application;

public sealed class FiscalService(
    IFiscalTransactionRepository fiscalRepo,
    IntegrationService integration,
    PaymentService payments,
    EInvoiceService eInvoice,
    IPaymentTerminalProvider terminalProvider,
    IEAdisyonProvider eAdisyonProvider,
    IConfiguration configuration,
    ILogger<FiscalService> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<FiscalOverviewDto> GetOverviewAsync(string tenantId, string? branchId, CancellationToken ct = default)
    {
        var terminals = await integration.ListTerminalsAsync(tenantId, branchId, ct);
        var commands = await integration.ListCommandsAsync(tenantId, branchId, null, 100, ct);
        var recent = await fiscalRepo.ListAsync(tenantId, branchId, null, 25, ct);
        var fiscalTerminals = terminals
            .Where(x => x.TerminalType is "payment" or "fiscal")
            .Select(x => new FiscalTerminalDto(
                x.Id, x.Name, x.TerminalType, x.ProviderTerminalId, x.ConnectionMode,
                x.IsActive == 1, x.LastSeenAt))
            .ToList();

        return new FiscalOverviewDto(
            terminalProvider.Name,
            eAdisyonProvider.Name,
            configuration.GetValue("Fiscal:EAdisyon:Enabled", false),
            fiscalTerminals.Count(x => x.IsActive),
            commands.Count(x => x.Status is "queued" or "sent"),
            fiscalTerminals,
            recent.Select(Map).ToList());
    }

    public async Task<IReadOnlyList<FiscalTransactionDto>> ListTransactionsAsync(
        string tenantId, string? branchId, string? status, int take, CancellationToken ct = default)
        => (await fiscalRepo.ListAsync(tenantId, branchId, Clean(status), Math.Clamp(take, 1, 250), ct))
            .Select(Map)
            .ToList();

    public async Task<Result<FiscalPaymentResultDto>> ProcessPaymentAsync(
        string tenantId,
        string? branchId,
        string orderId,
        string userId,
        FiscalPaymentRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(branchId))
            return Error.Validation("branch.required", "Bu işlem için aktif bir şube seçili olmalı.");

        var method = NormalizeMethod(request.Method);
        var currency = NormalizeCurrency(request.Currency);
        if (method is "card" or "meal_voucher")
            return await ProcessTerminalPaymentAsync(tenantId, branchId, orderId, userId, request, method, currency, ct);

        var auditId = await fiscalRepo.CreateAsync(new FiscalTransactionDraft(
            tenantId, branchId, orderId, null, "manual-cash", method, request.Amount, request.Tip,
            currency, "queued", SafeJson(new { request.Method, request.Amount, request.Tip, currency }), userId), ct);

        var payment = await payments.AddPaymentAsync(tenantId, orderId, userId,
            new AddPaymentRequest(method, request.Amount, request.Tip, "POS"), ct);
        if (payment.IsFailure)
        {
            await fiscalRepo.FailAsync(tenantId, auditId, payment.Error.Code, payment.Error.Message, null, ct);
            return payment.Error;
        }

        var doc = await TryIssueEInvoiceAsync(tenantId, userId, orderId, request, payment.Value.Closed, ct);
        var status = doc.DocumentError is null ? "completed" : "paid_document_pending";
        await fiscalRepo.CompleteManualAsync(tenantId, auditId, payment.Value.PaymentId, "POS", status, ct);

        return new FiscalPaymentResultDto(
            auditId,
            status,
            payment.Value.Closed ? "Ödeme tamamlandı." : "Kısmi ödeme alındı.",
            null,
            payment.Value.PaymentId,
            payment.Value.Closed,
            payment.Value.Change,
            payment.Value.Balance,
            "POS",
            null,
            null,
            null,
            doc.DocumentId,
            doc.Status);
    }

    public async Task<Result<FiscalPaymentResultDto>> ProcessManualCardOverrideAsync(
        string tenantId,
        string? branchId,
        string orderId,
        string userId,
        ManualCardOverrideRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(branchId))
            return Error.Validation("branch.required", "Bu işlem için aktif bir şube seçili olmalı.");

        var method = NormalizeMethod(request.Method);
        var reference = string.IsNullOrWhiteSpace(request.Reference)
            ? $"MANUAL-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
            : request.Reference.Trim();

        var auditId = await fiscalRepo.CreateAsync(new FiscalTransactionDraft(
            tenantId, branchId, orderId, null, "manual-override", method, request.Amount, request.Tip,
            "TRY", "manual_override",
            SafeJson(new { request.Method, request.Amount, request.Tip, reference, reason = request.Reason }),
            userId), ct);

        var payment = await payments.AddPaymentAsync(tenantId, orderId, userId,
            new AddPaymentRequest(method, request.Amount, request.Tip, reference), ct);
        if (payment.IsFailure)
        {
            await fiscalRepo.FailAsync(tenantId, auditId, payment.Error.Code, payment.Error.Message, null, ct);
            return payment.Error;
        }

        await fiscalRepo.CompleteManualAsync(tenantId, auditId, payment.Value.PaymentId, reference, "completed", ct);

        return new FiscalPaymentResultDto(
            auditId, "manual_override", "Manuel kart tahsilatı denetim kaydıyla işlendi.",
            null, payment.Value.PaymentId, payment.Value.Closed, payment.Value.Change, payment.Value.Balance,
            reference, null, null, null, null, null);
    }

    public async Task<Result<FiscalPaymentResultDto>> TestSaleAsync(
        string tenantId,
        string? branchId,
        string terminalId,
        string userId,
        TerminalTestSaleRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(branchId))
            return Error.Validation("branch.required", "Bu işlem için aktif bir şube seçili olmalı.");

        var terminals = await integration.ListTerminalsAsync(tenantId, branchId, ct);
        var terminal = terminals.FirstOrDefault(x => x.Id == terminalId && x.IsActive == 1);
        if (terminal is null)
            return Error.NotFound("fiscal.terminal.not_found", "Seçilen POS cihazı bulunamadı veya pasif.");

        var idempotency = $"fiscal:test:{terminalId}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var sale = new PaymentTerminalSaleRequest(
            tenantId, branchId, null, terminalId, request.Amount, 0m, NormalizeCurrency(request.Currency),
            "card", idempotency, true, terminal.Name, new Dictionary<string, string?> { ["mode"] = "test" });

        var txId = await fiscalRepo.CreateAsync(new FiscalTransactionDraft(
            tenantId, branchId, null, terminalId, terminalProvider.Name, "card",
            request.Amount, 0m, sale.Currency, "queued", SafeJson(sale), userId), ct);

        var command = await QueueSaleCommandAsync(tenantId, branchId, null, terminal, sale, userId, ct);
        if (command.IsFailure)
        {
            await fiscalRepo.FailAsync(tenantId, txId, command.Error.Code, command.Error.Message, null, ct);
            return command.Error;
        }

        await fiscalRepo.AttachCommandAsync(tenantId, txId, command.Value, "sent", ct);
        var result = await terminalProvider.SaleAsync(sale, ct);
        if (!result.Success)
        {
            await integration.MarkCommandFailedAsync(tenantId, command.Value,
                new MarkCommandFailedRequest(result.ErrorCode, SafeTerminalMessage(result), result.RawResponseJson), ct);
            await fiscalRepo.FailAsync(tenantId, txId, result.ErrorCode, SafeTerminalMessage(result), result.RawResponseJson, ct);
            return Error.Conflict("fiscal.terminal.failed", SafeTerminalMessage(result));
        }

        await integration.MarkCommandCompletedAsync(tenantId, command.Value,
            new MarkCommandCompletedRequest(result.ProviderReference, result.RawResponseJson), ct);
        await fiscalRepo.CompleteAsync(tenantId, txId, null, result, null, "completed", ct);

        return new FiscalPaymentResultDto(
            txId, "completed", "Cihaz test işlemi başarılı.", command.Value, null, false, 0, request.Amount,
            result.ProviderReference, result.AuthorizationCode, result.Rrn, result.FiscalReceiptNo, null, null);
    }

    private async Task<Result<FiscalPaymentResultDto>> ProcessTerminalPaymentAsync(
        string tenantId,
        string branchId,
        string orderId,
        string userId,
        FiscalPaymentRequest request,
        string method,
        string currency,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TerminalId))
            return Error.Validation("fiscal.terminal.required", "Kartlı tahsilat için POS cihazı seçin.");

        var terminals = await integration.ListTerminalsAsync(tenantId, branchId, ct);
        var terminal = terminals.FirstOrDefault(x => x.Id == request.TerminalId && x.IsActive == 1);
        if (terminal is null)
            return Error.NotFound("fiscal.terminal.not_found", "Seçilen POS cihazı bulunamadı veya pasif.");

        var idempotency = $"fiscal:sale:{orderId}:{method}:{request.Amount:0.0000}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var sale = new PaymentTerminalSaleRequest(
            tenantId, branchId, orderId, terminal.Id, request.Amount, request.Tip, currency, method,
            idempotency, false, terminal.Name,
            new Dictionary<string, string?>
            {
                ["providerTerminalId"] = terminal.ProviderTerminalId,
                ["connectionMode"] = terminal.ConnectionMode
            });

        var txId = await fiscalRepo.CreateAsync(new FiscalTransactionDraft(
            tenantId, branchId, orderId, terminal.Id, terminalProvider.Name, method,
            request.Amount, request.Tip, currency, "queued", SafeJson(sale), userId), ct);

        var command = await QueueSaleCommandAsync(tenantId, branchId, orderId, terminal, sale, userId, ct);
        if (command.IsFailure)
        {
            await fiscalRepo.FailAsync(tenantId, txId, command.Error.Code, command.Error.Message, null, ct);
            return command.Error;
        }

        await fiscalRepo.AttachCommandAsync(tenantId, txId, command.Value, "sent", ct);
        await integration.MarkCommandSentAsync(tenantId, command.Value, new MarkCommandSentRequest(), ct);

        PaymentTerminalResult terminalResult;
        try
        {
            terminalResult = await terminalProvider.SaleAsync(sale, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment terminal sale failed for order {OrderId}", orderId);
            var message = "POS cihazından yanıt alınamadı. Ödeme kaydedilmedi.";
            await integration.MarkCommandFailedAsync(tenantId, command.Value,
                new MarkCommandFailedRequest("terminal.unreachable", message), ct);
            await fiscalRepo.FailAsync(tenantId, txId, "terminal.unreachable", message, null, ct);
            return Error.Conflict("fiscal.terminal.unreachable", message);
        }

        if (!terminalResult.Success)
        {
            var message = SafeTerminalMessage(terminalResult);
            await integration.MarkCommandFailedAsync(tenantId, command.Value,
                new MarkCommandFailedRequest(terminalResult.ErrorCode, message, terminalResult.RawResponseJson), ct);
            await fiscalRepo.FailAsync(tenantId, txId, terminalResult.ErrorCode, message, terminalResult.RawResponseJson, ct);
            return Error.Conflict("fiscal.terminal.failed", message);
        }

        await integration.MarkCommandCompletedAsync(tenantId, command.Value,
            new MarkCommandCompletedRequest(terminalResult.ProviderReference, terminalResult.RawResponseJson), ct);

        var payment = await payments.AddPaymentAsync(tenantId, orderId, userId,
            new AddPaymentRequest(method, request.Amount, request.Tip, TerminalReference(terminalResult)), ct);
        if (payment.IsFailure)
        {
            await fiscalRepo.FailAsync(tenantId, txId, payment.Error.Code, payment.Error.Message, terminalResult.RawResponseJson, ct);
            return payment.Error;
        }

        var document = await TryIssueEInvoiceAsync(tenantId, userId, orderId, request, payment.Value.Closed, ct);
        var status = document.DocumentError is null ? "completed" : "paid_document_pending";
        await fiscalRepo.CompleteAsync(tenantId, txId, payment.Value.PaymentId, terminalResult, document.DocumentUuid, status, ct);

        await TryNotifyEAdisyonAsync(tenantId, branchId, orderId, payment.Value.PaymentId, request.Amount, currency, txId, ct);

        var userMessage = payment.Value.Closed ? "Ödeme tamamlandı." : "Kısmi ödeme alındı.";
        if (document.DocumentError is not null)
            userMessage += " e-Belge daha sonra tekrar gönderilecek.";

        return new FiscalPaymentResultDto(
            txId,
            status,
            userMessage,
            command.Value,
            payment.Value.PaymentId,
            payment.Value.Closed,
            payment.Value.Change,
            payment.Value.Balance,
            terminalResult.ProviderReference,
            terminalResult.AuthorizationCode,
            terminalResult.Rrn,
            terminalResult.FiscalReceiptNo,
            document.DocumentId,
            document.Status);
    }

    private async Task<Result<string>> QueueSaleCommandAsync(
        string tenantId,
        string branchId,
        string? orderId,
        TerminalDto terminal,
        PaymentTerminalSaleRequest sale,
        string userId,
        CancellationToken ct)
    {
        var payload = SafeJson(new
        {
            protocol = "gmp3",
            operation = sale.IsTest ? "test_sale" : "sale",
            sale.Amount,
            sale.Tip,
            sale.Currency,
            sale.Method,
            sale.IsTest,
            sale.IdempotencyKey,
            terminal.ProviderTerminalId
        });

        var res = await integration.QueueCommandAsync(tenantId, branchId,
            new QueueTerminalCommandRequest(
                "sale", payload, branchId, terminal.ConnectorId, terminal.Id, orderId, null, sale.IdempotencyKey),
            userId, ct);

        return res.IsSuccess ? res.Value.CommandId : res.Error;
    }

    private async Task<(string? DocumentId, string? DocumentUuid, string? Status, string? DocumentError)> TryIssueEInvoiceAsync(
        string tenantId,
        string userId,
        string orderId,
        FiscalPaymentRequest request,
        bool orderClosed,
        CancellationToken ct)
    {
        if (!request.IssueEInvoice || !orderClosed)
            return (null, null, null, null);

        var res = await eInvoice.IssueForOrderAsync(tenantId, userId, orderId, new IssueEInvoiceRequest(
            request.DocumentType, request.BuyerName, request.BuyerTaxNumber, request.BuyerTaxOffice,
            request.BuyerAddress, request.BuyerCity, request.BuyerEmail, request.Notes), ct);

        if (res.IsFailure)
        {
            logger.LogWarning("EInvoice issue failed for order {OrderId}: {Code}", orderId, res.Error.Code);
            return (null, null, "error", res.Error.Message);
        }

        return (res.Value.Id, res.Value.Uuid, res.Value.Status, null);
    }

    private async Task TryNotifyEAdisyonAsync(
        string tenantId,
        string branchId,
        string orderId,
        string paymentId,
        decimal amount,
        string currency,
        string fiscalTransactionId,
        CancellationToken ct)
    {
        try
        {
            var result = await eAdisyonProvider.NotifyPaidAsync(
                new EAdisyonNotifyRequest(tenantId, branchId, orderId, paymentId, amount, currency, fiscalTransactionId), ct);
            if (!result.Success)
                logger.LogWarning("EAdisyon notification failed for order {OrderId}: {Code}", orderId, result.ErrorCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "EAdisyon notification failed for order {OrderId}", orderId);
        }
    }

    private static FiscalTransactionDto Map(FiscalTransactionRow row) => new(
        row.Id, row.BranchId, row.OrderId, row.PaymentId, row.CommandId, row.TerminalId, row.Provider,
        row.Method, row.Amount, row.TipAmount, row.Currency, row.Status, row.AuthorizationCode,
        row.BatchNo, row.Stan, row.Rrn, row.FiscalReceiptNo, row.ZNo, row.DeviceSerial,
        row.DocumentUuid, row.ErrorCode, row.ErrorMessage, row.CreatedAt, row.CompletedAt);

    private static string NormalizeMethod(string method) => (method ?? "").Trim().ToLowerInvariant();

    private static string NormalizeCurrency(string? currency)
        => string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim().ToUpperInvariant();

    private static string SafeJson<T>(T value) => JsonSerializer.Serialize(value, Json);

    private static string SafeTerminalMessage(PaymentTerminalResult result)
        => result.ErrorCode switch
        {
            "terminal.not_configured" => "POS cihazı entegrasyonu tamamlanmamış. Ayarlardan cihaz bağlantısını kontrol edin.",
            "terminal.timeout" => "POS cihazından zamanında yanıt alınamadı. Ödeme kaydedilmedi.",
            "terminal.declined" => "Ödeme cihaz tarafından onaylanmadı. Farklı kart veya yöntem deneyin.",
            _ => string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "POS cihazı işlemi tamamlayamadı. Ödeme kaydedilmedi."
                : result.ErrorMessage!.Length > 140
                    ? "POS cihazı işlemi tamamlayamadı. Ödeme kaydedilmedi."
                    : result.ErrorMessage!
        };

    private static string TerminalReference(PaymentTerminalResult result)
    {
        var parts = new[] { result.ProviderReference, result.AuthorizationCode, result.Rrn }
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(" / ", parts);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
