using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Integration.Application;

namespace Ordevo.Modules.Integration.Infrastructure;

public sealed class IntegrationProcedures(IDbConnectionFactory factory) : IIntegrationProcedures
{
    public async Task<string> CreateConnectorAsync(
        string tenantId, string? branchId, CreateConnectorRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_code", request.Code);
        p.Add("p_name", request.Name);
        p.Add("p_connector_type", request.ConnectorType);
        p.Add("p_provider_code", request.ProviderCode);
        p.Add("p_base_url", request.BaseUrl);
        p.Add("p_auth_type", request.AuthType);
        p.Add("p_secret_ref", request.SecretRef);
        p.Add("p_settings", request.Settings);
        p.Add("p_user_id", userId);
        p.Add("p_connector_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_INTEGRATION.CREATE_CONNECTOR", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_connector_id");
    }

    public Task SetConnectorStatusAsync(
        string tenantId, string connectorId, SetConnectorStatusRequest request, string userId, CancellationToken ct = default)
        => ExecAsync("PKG_INTEGRATION.SET_CONNECTOR_STATUS", ct,
            ("p_tenant_id", tenantId), ("p_connector_id", connectorId),
            ("p_status", request.Status), ("p_reason", request.Reason), ("p_user_id", userId));

    public async Task<string> CreateWebhookSubscriptionAsync(
        string tenantId, string? branchId, CreateWebhookSubscriptionRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_connector_id", request.ConnectorId);
        p.Add("p_name", request.Name);
        p.Add("p_target_url", request.TargetUrl);
        p.Add("p_secret_ref", request.SecretRef);
        p.Add("p_event_pattern", request.EventPattern);
        p.Add("p_event_filter", request.EventFilter);
        p.Add("p_headers", request.Headers);
        p.Add("p_max_attempts", request.MaxAttempts);
        p.Add("p_timeout_seconds", request.TimeoutSeconds);
        p.Add("p_user_id", userId);
        p.Add("p_subscription_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_INTEGRATION.CREATE_WEBHOOK_SUBSCRIPTION", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_subscription_id");
    }

    public Task SetWebhookStatusAsync(
        string tenantId, string subscriptionId, SetWebhookStatusRequest request, string userId, CancellationToken ct = default)
        => ExecAsync("PKG_INTEGRATION.SET_WEBHOOK_STATUS", ct,
            ("p_tenant_id", tenantId), ("p_subscription_id", subscriptionId),
            ("p_status", request.Status), ("p_user_id", userId));

    public async Task<QueueIntegrationEventResponse> QueueEventAsync(
        string tenantId, string? branchId, QueueIntegrationEventRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_source_module", request.SourceModule);
        p.Add("p_event_type", request.EventType);
        p.Add("p_aggregate_type", request.AggregateType);
        p.Add("p_aggregate_id", request.AggregateId);
        p.Add("p_payload", request.Payload);
        p.Add("p_correlation_id", request.CorrelationId);
        p.Add("p_user_id", userId);
        p.Add("p_event_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        p.Add("p_delivery_count", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await db.ExecuteAsync("PKG_INTEGRATION.QUEUE_EVENT", p, commandType: CommandType.StoredProcedure);
        return new QueueIntegrationEventResponse(p.Get<string>("p_event_id"), p.Get<int>("p_delivery_count"));
    }

    public Task MarkDeliverySuccessAsync(
        string tenantId, string deliveryId, MarkDeliverySuccessRequest request, CancellationToken ct = default)
        => ExecAsync("PKG_INTEGRATION.MARK_DELIVERY_SUCCESS", ct,
            ("p_tenant_id", tenantId), ("p_delivery_id", deliveryId),
            ("p_status_code", request.StatusCode), ("p_request_headers", request.RequestHeaders),
            ("p_response_body", request.ResponseBody), ("p_latency_ms", request.LatencyMs));

    public async Task MarkDeliveryFailureAsync(
        string tenantId, string deliveryId, MarkDeliveryFailureRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_delivery_id", deliveryId);
        p.Add("p_status_code", request.StatusCode);
        p.Add("p_request_headers", request.RequestHeaders);
        p.Add("p_response_body", request.ResponseBody);
        p.Add("p_error_message", request.ErrorMessage);
        p.Add("p_latency_ms", request.LatencyMs);
        p.Add("p_next_attempt_at", request.NextAttemptAt?.UtcDateTime, DbType.DateTime);
        await db.ExecuteAsync("PKG_INTEGRATION.MARK_DELIVERY_FAILURE", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<string> RegisterTerminalAsync(
        string tenantId, string branchId, RegisterTerminalRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_connector_id", request.ConnectorId);
        p.Add("p_device_id", request.DeviceId);
        p.Add("p_name", request.Name);
        p.Add("p_terminal_type", request.TerminalType);
        p.Add("p_provider_terminal_id", request.ProviderTerminalId);
        p.Add("p_connection_mode", request.ConnectionMode);
        p.Add("p_ip_address", request.IpAddress);
        p.Add("p_port", request.Port);
        p.Add("p_serial_path", request.SerialPath);
        p.Add("p_settings", request.Settings);
        p.Add("p_user_id", userId);
        p.Add("p_terminal_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_INTEGRATION.REGISTER_TERMINAL", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_terminal_id");
    }

    public async Task<QueueTerminalCommandResponse> QueueCommandAsync(
        string tenantId, string branchId, QueueTerminalCommandRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_connector_id", request.ConnectorId);
        p.Add("p_terminal_id", request.TerminalId);
        p.Add("p_order_id", request.OrderId);
        p.Add("p_payment_id", request.PaymentId);
        p.Add("p_command_type", request.CommandType);
        p.Add("p_idempotency_key", request.IdempotencyKey);
        p.Add("p_payload", request.Payload);
        p.Add("p_user_id", userId);
        p.Add("p_command_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        p.Add("p_status", dbType: DbType.String, direction: ParameterDirection.Output, size: 20);
        await db.ExecuteAsync("PKG_INTEGRATION.QUEUE_COMMAND", p, commandType: CommandType.StoredProcedure);
        return new QueueTerminalCommandResponse(p.Get<string>("p_command_id"), p.Get<string>("p_status"));
    }

    public Task MarkCommandSentAsync(
        string tenantId, string commandId, MarkCommandSentRequest request, CancellationToken ct = default)
        => ExecAsync("PKG_INTEGRATION.MARK_COMMAND_SENT", ct,
            ("p_tenant_id", tenantId), ("p_command_id", commandId),
            ("p_provider_reference", request.ProviderReference));

    public Task MarkCommandCompletedAsync(
        string tenantId, string commandId, MarkCommandCompletedRequest request, CancellationToken ct = default)
        => ExecAsync("PKG_INTEGRATION.MARK_COMMAND_COMPLETED", ct,
            ("p_tenant_id", tenantId), ("p_command_id", commandId),
            ("p_provider_reference", request.ProviderReference), ("p_result_payload", request.ResultPayload));

    public Task MarkCommandFailedAsync(
        string tenantId, string commandId, MarkCommandFailedRequest request, CancellationToken ct = default)
        => ExecAsync("PKG_INTEGRATION.MARK_COMMAND_FAILED", ct,
            ("p_tenant_id", tenantId), ("p_command_id", commandId),
            ("p_error_code", request.ErrorCode), ("p_error_message", request.ErrorMessage),
            ("p_result_payload", request.ResultPayload));

    private async Task ExecAsync(string procName, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        foreach (var (name, value) in args)
            p.Add(name, value);
        await db.ExecuteAsync(procName, p, commandType: CommandType.StoredProcedure);
    }
}
