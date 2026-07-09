using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.Integration.Application;

namespace Ordevo.Modules.Integration.Infrastructure;

public sealed class IntegrationRepository(IDbConnectionFactory factory) : IIntegrationRepository
{
    public async Task<IReadOnlyList<ConnectorDto>> ListConnectorsAsync(
        string tenantId, string? branchId, string? connectorType, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<ConnectorDto>(
            """
            SELECT ID, BRANCH_ID AS BranchId, CODE, NAME, CONNECTOR_TYPE AS ConnectorType,
                   PROVIDER_CODE AS ProviderCode, BASE_URL AS BaseUrl, AUTH_TYPE AS AuthType,
                   SECRET_REF AS SecretRef, SETTINGS, STATUS, IS_ACTIVE AS IsActive,
                   LAST_SUCCESS_AT AS LastSuccessAt, LAST_FAILURE_AT AS LastFailureAt,
                   FAILURE_REASON AS FailureReason, CREATED_AT AS CreatedAt, ROW_VERSION AS RowVersion
            FROM INTEGRATION_CONNECTORS
            WHERE TENANT_ID = :tenantId
              AND (:branchId IS NULL OR BRANCH_ID IS NULL OR BRANCH_ID = :branchId)
              AND (:connectorType IS NULL OR CONNECTOR_TYPE = :connectorType)
            ORDER BY CONNECTOR_TYPE, CODE
            """,
            new OracleParams(new { tenantId, branchId, connectorType }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<WebhookSubscriptionDto>> ListWebhookSubscriptionsAsync(
        string tenantId, string? branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<WebhookSubscriptionDto>(
            """
            SELECT ID, BRANCH_ID AS BranchId, CONNECTOR_ID AS ConnectorId, NAME,
                   TARGET_URL AS TargetUrl, SECRET_REF AS SecretRef, EVENT_PATTERN AS EventPattern,
                   EVENT_FILTER AS EventFilter, HEADERS, STATUS, MAX_ATTEMPTS AS MaxAttempts,
                   TIMEOUT_SECONDS AS TimeoutSeconds, IS_ACTIVE AS IsActive,
                   CREATED_AT AS CreatedAt, ROW_VERSION AS RowVersion
            FROM WEBHOOK_SUBSCRIPTIONS
            WHERE TENANT_ID = :tenantId
              AND (:branchId IS NULL OR BRANCH_ID IS NULL OR BRANCH_ID = :branchId)
            ORDER BY NAME
            """,
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<IntegrationEventDto>> ListEventsAsync(
        string tenantId, string? status, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<IntegrationEventDto>(
            """
            SELECT ID, BRANCH_ID AS BranchId, SOURCE_MODULE AS SourceModule,
                   EVENT_TYPE AS EventType, AGGREGATE_TYPE AS AggregateType,
                   AGGREGATE_ID AS AggregateId, PAYLOAD, CORRELATION_ID AS CorrelationId,
                   STATUS, ATTEMPTS, NEXT_ATTEMPT_AT AS NextAttemptAt,
                   CREATED_AT AS CreatedAt, PROCESSED_AT AS ProcessedAt, ROW_VERSION AS RowVersion
            FROM INTEGRATION_EVENTS
            WHERE TENANT_ID = :tenantId
              AND (:status IS NULL OR STATUS = :status)
            ORDER BY CREATED_AT DESC
            FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, status, take }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<WebhookDeliveryDto>> ListPendingDeliveriesAsync(
        string tenantId, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<WebhookDeliveryDto>(
            """
            SELECT d.ID, d.EVENT_ID AS EventId, d.SUBSCRIPTION_ID AS SubscriptionId,
                   s.NAME AS SubscriptionName, s.TARGET_URL AS TargetUrl,
                   d.ATTEMPT_NO AS AttemptNo, d.STATUS, d.STATUS_CODE AS StatusCode,
                   d.REQUEST_HEADERS AS RequestHeaders, d.RESPONSE_BODY AS ResponseBody,
                   d.ERROR_MESSAGE AS ErrorMessage, d.LATENCY_MS AS LatencyMs,
                   d.NEXT_ATTEMPT_AT AS NextAttemptAt, d.SENT_AT AS SentAt,
                   d.CREATED_AT AS CreatedAt
            FROM WEBHOOK_DELIVERIES d
            JOIN WEBHOOK_SUBSCRIPTIONS s ON s.ID = d.SUBSCRIPTION_ID
            JOIN INTEGRATION_EVENTS e ON e.ID = d.EVENT_ID
            WHERE d.TENANT_ID = :tenantId
              AND d.STATUS = 'pending'
              AND d.NEXT_ATTEMPT_AT <= SYSTIMESTAMP
              AND e.STATUS IN ('pending','processing')
            ORDER BY d.NEXT_ATTEMPT_AT, d.CREATED_AT
            FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, take }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<TerminalDto>> ListTerminalsAsync(
        string tenantId, string? branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<TerminalDto>(
            """
            SELECT ID, BRANCH_ID AS BranchId, CONNECTOR_ID AS ConnectorId,
                   DEVICE_ID AS DeviceId, NAME, TERMINAL_TYPE AS TerminalType,
                   PROVIDER_TERMINAL_ID AS ProviderTerminalId, CONNECTION_MODE AS ConnectionMode,
                   IP_ADDRESS AS IpAddress, PORT, SERIAL_PATH AS SerialPath, SETTINGS,
                   IS_ACTIVE AS IsActive, LAST_SEEN_AT AS LastSeenAt,
                   CREATED_AT AS CreatedAt, ROW_VERSION AS RowVersion
            FROM INTEGRATION_TERMINALS
            WHERE TENANT_ID = :tenantId
              AND (:branchId IS NULL OR BRANCH_ID = :branchId)
            ORDER BY TERMINAL_TYPE, NAME
            """,
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<TerminalCommandDto>> ListCommandsAsync(
        string tenantId, string? branchId, string? status, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<TerminalCommandDto>(
            """
            SELECT ID, BRANCH_ID AS BranchId, CONNECTOR_ID AS ConnectorId,
                   TERMINAL_ID AS TerminalId, ORDER_ID AS OrderId, PAYMENT_ID AS PaymentId,
                   COMMAND_TYPE AS CommandType, IDEMPOTENCY_KEY AS IdempotencyKey,
                   PAYLOAD, STATUS, PROVIDER_REFERENCE AS ProviderReference,
                   RESULT_PAYLOAD AS ResultPayload, ERROR_CODE AS ErrorCode,
                   ERROR_MESSAGE AS ErrorMessage, REQUESTED_BY AS RequestedBy,
                   CREATED_AT AS CreatedAt, SENT_AT AS SentAt,
                   COMPLETED_AT AS CompletedAt, ROW_VERSION AS RowVersion
            FROM INTEGRATION_COMMANDS
            WHERE TENANT_ID = :tenantId
              AND (:branchId IS NULL OR BRANCH_ID = :branchId)
              AND (:status IS NULL OR STATUS = :status)
            ORDER BY CREATED_AT DESC
            FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, branchId, status, take }));
        return rows.AsList();
    }
}
