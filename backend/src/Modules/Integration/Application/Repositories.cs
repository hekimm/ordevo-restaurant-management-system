namespace Ordevo.Modules.Integration.Application;

public interface IIntegrationRepository
{
    Task<IReadOnlyList<ConnectorDto>> ListConnectorsAsync(string tenantId, string? branchId, string? connectorType, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookSubscriptionDto>> ListWebhookSubscriptionsAsync(string tenantId, string? branchId, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationEventDto>> ListEventsAsync(string tenantId, string? status, int take, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookDeliveryDto>> ListPendingDeliveriesAsync(string tenantId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<TerminalDto>> ListTerminalsAsync(string tenantId, string? branchId, CancellationToken ct = default);
    Task<IReadOnlyList<TerminalCommandDto>> ListCommandsAsync(string tenantId, string? branchId, string? status, int take, CancellationToken ct = default);
}

public interface IIntegrationProcedures
{
    Task<string> CreateConnectorAsync(string tenantId, string? branchId, CreateConnectorRequest request, string userId, CancellationToken ct = default);
    Task SetConnectorStatusAsync(string tenantId, string connectorId, SetConnectorStatusRequest request, string userId, CancellationToken ct = default);
    Task<string> CreateWebhookSubscriptionAsync(string tenantId, string? branchId, CreateWebhookSubscriptionRequest request, string userId, CancellationToken ct = default);
    Task SetWebhookStatusAsync(string tenantId, string subscriptionId, SetWebhookStatusRequest request, string userId, CancellationToken ct = default);
    Task<QueueIntegrationEventResponse> QueueEventAsync(string tenantId, string? branchId, QueueIntegrationEventRequest request, string userId, CancellationToken ct = default);
    Task MarkDeliverySuccessAsync(string tenantId, string deliveryId, MarkDeliverySuccessRequest request, CancellationToken ct = default);
    Task MarkDeliveryFailureAsync(string tenantId, string deliveryId, MarkDeliveryFailureRequest request, CancellationToken ct = default);
    Task<string> RegisterTerminalAsync(string tenantId, string branchId, RegisterTerminalRequest request, string userId, CancellationToken ct = default);
    Task<QueueTerminalCommandResponse> QueueCommandAsync(string tenantId, string branchId, QueueTerminalCommandRequest request, string userId, CancellationToken ct = default);
    Task MarkCommandSentAsync(string tenantId, string commandId, MarkCommandSentRequest request, CancellationToken ct = default);
    Task MarkCommandCompletedAsync(string tenantId, string commandId, MarkCommandCompletedRequest request, CancellationToken ct = default);
    Task MarkCommandFailedAsync(string tenantId, string commandId, MarkCommandFailedRequest request, CancellationToken ct = default);
}
