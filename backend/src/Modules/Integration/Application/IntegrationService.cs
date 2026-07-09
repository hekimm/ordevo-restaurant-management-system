using Oracle.ManagedDataAccess.Client;
using Ordevo.BuildingBlocks.Results;

namespace Ordevo.Modules.Integration.Application;

public sealed class IntegrationService(IIntegrationRepository repo, IIntegrationProcedures procs)
{
    public Task<IReadOnlyList<ConnectorDto>> ListConnectorsAsync(
        string tenantId, string? branchId, string? connectorType, CancellationToken ct = default)
        => repo.ListConnectorsAsync(tenantId, branchId, connectorType, ct);

    public async Task<Result<ConnectorDto>> CreateConnectorAsync(
        string tenantId, string? fallbackBranchId, CreateConnectorRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            var branchId = request.BranchId ?? fallbackBranchId;
            var id = await procs.CreateConnectorAsync(tenantId, branchId, request, userId, ct);
            var connector = (await repo.ListConnectorsAsync(tenantId, branchId, null, ct)).First(x => x.Id == id);
            return connector;
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> SetConnectorStatusAsync(
        string tenantId, string connectorId, SetConnectorStatusRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.SetConnectorStatusAsync(tenantId, connectorId, request, userId, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<WebhookSubscriptionDto>> ListWebhookSubscriptionsAsync(
        string tenantId, string? branchId, CancellationToken ct = default)
        => repo.ListWebhookSubscriptionsAsync(tenantId, branchId, ct);

    public async Task<Result<WebhookSubscriptionDto>> CreateWebhookSubscriptionAsync(
        string tenantId, string? fallbackBranchId, CreateWebhookSubscriptionRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            var branchId = request.BranchId ?? fallbackBranchId;
            var id = await procs.CreateWebhookSubscriptionAsync(tenantId, branchId, request, userId, ct);
            var webhook = (await repo.ListWebhookSubscriptionsAsync(tenantId, branchId, ct)).First(x => x.Id == id);
            return webhook;
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> SetWebhookStatusAsync(
        string tenantId, string subscriptionId, SetWebhookStatusRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.SetWebhookStatusAsync(tenantId, subscriptionId, request, userId, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<IntegrationEventDto>> ListEventsAsync(
        string tenantId, string? status, int take, CancellationToken ct = default)
        => repo.ListEventsAsync(tenantId, NormalizeStatus(status), Math.Clamp(take, 1, 500), ct);

    public async Task<Result<QueueIntegrationEventResponse>> QueueEventAsync(
        string tenantId, string? fallbackBranchId, QueueIntegrationEventRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            return await procs.QueueEventAsync(tenantId, request.BranchId ?? fallbackBranchId, request, userId, ct);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<WebhookDeliveryDto>> ListPendingDeliveriesAsync(
        string tenantId, int take, CancellationToken ct = default)
        => repo.ListPendingDeliveriesAsync(tenantId, Math.Clamp(take, 1, 500), ct);

    public async Task<Result> MarkDeliverySuccessAsync(
        string tenantId, string deliveryId, MarkDeliverySuccessRequest request, CancellationToken ct = default)
    {
        try
        {
            await procs.MarkDeliverySuccessAsync(tenantId, deliveryId, request, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> MarkDeliveryFailureAsync(
        string tenantId, string deliveryId, MarkDeliveryFailureRequest request, CancellationToken ct = default)
    {
        try
        {
            await procs.MarkDeliveryFailureAsync(tenantId, deliveryId, request, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<TerminalDto>> ListTerminalsAsync(
        string tenantId, string? branchId, CancellationToken ct = default)
        => repo.ListTerminalsAsync(tenantId, branchId, ct);

    public async Task<Result<TerminalDto>> RegisterTerminalAsync(
        string tenantId, string? fallbackBranchId, RegisterTerminalRequest request, string userId, CancellationToken ct = default)
    {
        var branchId = request.BranchId ?? fallbackBranchId;
        if (string.IsNullOrWhiteSpace(branchId))
            return Error.Validation("integration.branch.required", "Terminal kaydı için şube bağlamı gerekli.");

        try
        {
            var id = await procs.RegisterTerminalAsync(tenantId, branchId, request, userId, ct);
            var terminal = (await repo.ListTerminalsAsync(tenantId, branchId, ct)).First(x => x.Id == id);
            return terminal;
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<TerminalCommandDto>> ListCommandsAsync(
        string tenantId, string? branchId, string? status, int take, CancellationToken ct = default)
        => repo.ListCommandsAsync(tenantId, branchId, NormalizeStatus(status), Math.Clamp(take, 1, 500), ct);

    public async Task<Result<QueueTerminalCommandResponse>> QueueCommandAsync(
        string tenantId, string? fallbackBranchId, QueueTerminalCommandRequest request, string userId, CancellationToken ct = default)
    {
        var branchId = request.BranchId ?? fallbackBranchId;
        if (string.IsNullOrWhiteSpace(branchId))
            return Error.Validation("integration.branch.required", "Terminal komutu için şube bağlamı gerekli.");

        try
        {
            return await procs.QueueCommandAsync(tenantId, branchId, request, userId, ct);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> MarkCommandSentAsync(
        string tenantId, string commandId, MarkCommandSentRequest request, CancellationToken ct = default)
    {
        try
        {
            await procs.MarkCommandSentAsync(tenantId, commandId, request, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> MarkCommandCompletedAsync(
        string tenantId, string commandId, MarkCommandCompletedRequest request, CancellationToken ct = default)
    {
        try
        {
            await procs.MarkCommandCompletedAsync(tenantId, commandId, request, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> MarkCommandFailedAsync(
        string tenantId, string commandId, MarkCommandFailedRequest request, CancellationToken ct = default)
    {
        try
        {
            await procs.MarkCommandFailedAsync(tenantId, commandId, request, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    private static string? NormalizeStatus(string? status)
        => string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();

    private static bool TryMapOracle(OracleException ex, out Error error)
    {
        if (ex.Number == 1)
        {
            error = Error.Conflict("integration.duplicate", "Aynı entegrasyon kodu veya idempotency anahtarı zaten kayıtlı.");
            return true;
        }

        if (ex.Number is >= 20501 and <= 20530)
        {
            error = ex.Number switch
            {
                20501 => Error.NotFound("integration.connector.not_found", "Connector bulunamadı."),
                20503 => Error.NotFound("integration.webhook.not_found", "Webhook aboneliği bulunamadı."),
                20504 => Error.NotFound("integration.delivery.not_found", "Webhook teslimat kaydı bulunamadı."),
                20511 => Error.NotFound("integration.terminal.not_found", "Terminal bulunamadı."),
                20512 => Error.Conflict("integration.command.state", "Komut bulunamadı veya nihai durumda."),
                20514 => Error.Conflict("integration.terminal.inactive", "Terminal pasif durumda."),
                _ => Error.Validation("integration.rule", CleanMessage(ex))
            };
            return true;
        }

        error = Error.Failure("integration.db", CleanMessage(ex));
        return false;
    }

    private static string CleanMessage(OracleException ex)
    {
        var first = ex.Message.Split('\n')[0];
        return first.Replace($"ORA-{ex.Number}:", "", StringComparison.OrdinalIgnoreCase).Trim();
    }
}
