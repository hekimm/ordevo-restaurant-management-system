namespace Ordevo.Modules.Integration.Application;

public sealed class ConnectorDto
{
    public string Id { get; set; } = default!;
    public string? BranchId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string ConnectorType { get; set; } = default!;
    public string ProviderCode { get; set; } = default!;
    public string? BaseUrl { get; set; }
    public string AuthType { get; set; } = default!;
    public string? SecretRef { get; set; }
    public string? Settings { get; set; }
    public string Status { get; set; } = default!;
    public int IsActive { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public DateTimeOffset? LastFailureAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed record CreateConnectorRequest(
    string Code, string Name, string ConnectorType, string ProviderCode,
    string? BranchId = null, string? BaseUrl = null, string AuthType = "none",
    string? SecretRef = null, string? Settings = null);

public sealed record SetConnectorStatusRequest(string Status, string? Reason = null);

public sealed class WebhookSubscriptionDto
{
    public string Id { get; set; } = default!;
    public string? BranchId { get; set; }
    public string? ConnectorId { get; set; }
    public string Name { get; set; } = default!;
    public string TargetUrl { get; set; } = default!;
    public string? SecretRef { get; set; }
    public string EventPattern { get; set; } = default!;
    public string? EventFilter { get; set; }
    public string? Headers { get; set; }
    public string Status { get; set; } = default!;
    public int MaxAttempts { get; set; }
    public int TimeoutSeconds { get; set; }
    public int IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed record CreateWebhookSubscriptionRequest(
    string Name, string TargetUrl, string? BranchId = null, string? ConnectorId = null,
    string? SecretRef = null, string EventPattern = "*", string? EventFilter = null,
    string? Headers = null, int MaxAttempts = 5, int TimeoutSeconds = 15);

public sealed record SetWebhookStatusRequest(string Status);

public sealed class IntegrationEventDto
{
    public string Id { get; set; } = default!;
    public string? BranchId { get; set; }
    public string SourceModule { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string AggregateType { get; set; } = default!;
    public string AggregateId { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string Status { get; set; } = default!;
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed record QueueIntegrationEventRequest(
    string SourceModule, string EventType, string AggregateType, string AggregateId,
    string Payload, string? BranchId = null, string? CorrelationId = null);

public sealed record QueueIntegrationEventResponse(string EventId, int DeliveryCount);

public sealed class WebhookDeliveryDto
{
    public string Id { get; set; } = default!;
    public string EventId { get; set; } = default!;
    public string SubscriptionId { get; set; } = default!;
    public string SubscriptionName { get; set; } = default!;
    public string TargetUrl { get; set; } = default!;
    public int AttemptNo { get; set; }
    public string Status { get; set; } = default!;
    public int? StatusCode { get; set; }
    public string? RequestHeaders { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public int? LatencyMs { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record MarkDeliverySuccessRequest(
    int? StatusCode = null, string? RequestHeaders = null, string? ResponseBody = null,
    int? LatencyMs = null);

public sealed record MarkDeliveryFailureRequest(
    int? StatusCode = null, string? RequestHeaders = null, string? ResponseBody = null,
    string? ErrorMessage = null, int? LatencyMs = null, DateTimeOffset? NextAttemptAt = null);

public sealed class TerminalDto
{
    public string Id { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string? ConnectorId { get; set; }
    public string? DeviceId { get; set; }
    public string Name { get; set; } = default!;
    public string TerminalType { get; set; } = default!;
    public string? ProviderTerminalId { get; set; }
    public string ConnectionMode { get; set; } = default!;
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public string? SerialPath { get; set; }
    public string? Settings { get; set; }
    public int IsActive { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed record RegisterTerminalRequest(
    string Name, string TerminalType, string? BranchId = null, string? ConnectorId = null,
    string? DeviceId = null, string? ProviderTerminalId = null,
    string ConnectionMode = "cloud", string? IpAddress = null, int? Port = null,
    string? SerialPath = null, string? Settings = null);

public sealed class TerminalCommandDto
{
    public string Id { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string? ConnectorId { get; set; }
    public string? TerminalId { get; set; }
    public string? OrderId { get; set; }
    public string? PaymentId { get; set; }
    public string CommandType { get; set; } = default!;
    public string? IdempotencyKey { get; set; }
    public string Payload { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? ProviderReference { get; set; }
    public string? ResultPayload { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed record QueueTerminalCommandRequest(
    string CommandType, string Payload, string? BranchId = null, string? ConnectorId = null,
    string? TerminalId = null, string? OrderId = null, string? PaymentId = null,
    string? IdempotencyKey = null);

public sealed record QueueTerminalCommandResponse(string CommandId, string Status);

public sealed record MarkCommandSentRequest(string? ProviderReference = null);

public sealed record MarkCommandCompletedRequest(
    string? ProviderReference = null, string? ResultPayload = null);

public sealed record MarkCommandFailedRequest(
    string? ErrorCode = null, string? ErrorMessage = null, string? ResultPayload = null);
