namespace Ordevo.Desktop.Wpf.Models;

public sealed record ApiResult<T>(T? Value, int StatusCode, string? Error, bool FromCache = false)
{
    public bool IsSuccess => Error is null;
}

public sealed record NoContent;

public sealed record LoginRequest(string TenantSlug, string Email, string Password, string? DeviceFingerprint);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record AuthResult(TokenPair Tokens, UserProfile User);
public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record UserProfile(
    string Id,
    string TenantId,
    string TenantSlug,
    string Email,
    string FullName,
    string[] Roles,
    string[] Permissions,
    string[] BranchIds);

public sealed record DailyStatsDto(string Date, int OrderCount, decimal Revenue, decimal ItemCount, decimal AvgTicket);
public sealed record TopItemDto(string Name, string? Category, decimal Quantity, decimal Revenue);
public sealed record HourlyDto(int Hour, int OrderCount, decimal Revenue);
public sealed record CategorySalesDto(string Category, decimal Quantity, decimal Revenue);
public sealed record PaymentMethodDto(string Method, decimal Amount, int Count);
public sealed record DailySummaryDto(string BusinessDate, int OrderCount, decimal Revenue, decimal TaxTotal, decimal DiscountTotal);

public sealed record SectionDto(string Id, string Name, int SortOrder);
public sealed record UpsertSectionRequest(string Name, int SortOrder);
public sealed record TableDto(string Id, string? SectionId, string Name, int Capacity, string Status, int SortOrder, bool IsActive);
public sealed record UpsertTableRequest(string Name, string? SectionId, int Capacity, int SortOrder, bool IsActive = true);
public sealed record OpenOrderRequest(string? TableId, string OrderType = "dinein", int GuestCount = 1);
public sealed record AddItemRequest(string MenuItemId, decimal Quantity, string[]? ModifierIds, int CourseNo = 1, string? Note = null);
public sealed record SetQuantityRequest(decimal Quantity);
public sealed record VoidItemRequest(string? Reason);
public sealed record ApplyDiscountRequest(string Type, decimal Value, string? Reason);
public sealed record TransferRequest(string ToTableId);
public sealed record MergeRequest(string SourceOrderId);
public sealed record SplitRequest(string[] ItemIds, string? ToTableId);
public sealed record ItemStatusRequest(string Status);
public sealed record CancelOrderRequest(string? Reason);
public sealed record OrderItemModifierDto(string Id, string NameSnapshot, decimal PriceDelta);
public sealed record OrderItemDto(
    string Id,
    string MenuItemId,
    string Name,
    decimal UnitPrice,
    decimal Quantity,
    decimal ModifierTotal,
    decimal LineTotal,
    decimal VatRate,
    int CourseNo,
    string Status,
    bool IsComp,
    string? Note,
    IReadOnlyList<OrderItemModifierDto> Modifiers);

public sealed record OrderDto(
    string Id,
    long OrderNo,
    string? TableId,
    string OrderType,
    string Status,
    int GuestCount,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal Total,
    string? Note,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderSummaryDto(string Id, long OrderNo, string? TableId, string Status, decimal Total, DateTimeOffset OpenedAt);

public sealed record StationDto(string Id, string Name, string Code, int SortOrder, bool IsActive);
public sealed record UpsertStationRequest(string Name, string Code, int SortOrder, bool IsActive = true);
public sealed record KdsTicketDto(string OrderId, long OrderNo, string? TableName, DateTimeOffset OpenedAt, int ElapsedSeconds, IReadOnlyList<KdsItemDto> Items);
public sealed record KdsItemDto(string OrderItemId, string ItemName, decimal Quantity, int CourseNo, string Status, string? Station, string? Note, string? Modifiers, int ElapsedSeconds);
public sealed record SetItemStatusRequest(string Status);
public sealed record KitchenLine(string OrderId, string OrderItemId, long OrderNo, string? TableName, string ItemName, decimal Quantity, int CourseNo, string Status, string? Station, int ElapsedMinutes, string? Note);

public sealed record CategoryDto(string Id, string Name, string? Color, int SortOrder, bool IsActive);
public sealed record UpsertCategoryRequest(string Name, string? Color, int SortOrder, bool IsActive = true);
public sealed record MenuItemDto(
    string Id,
    string CategoryId,
    string Name,
    string? Description,
    decimal Price,
    decimal VatRate,
    string? Sku,
    string? ImageUrl,
    string? PrepStation,
    int SortOrder,
    bool IsActive);

public sealed record UpsertMenuItemRequest(
    string CategoryId,
    string Name,
    string? Description,
    decimal Price,
    decimal VatRate,
    string? Sku,
    string? ImageUrl,
    string? PrepStation,
    int SortOrder,
    bool IsActive = true);

public sealed record ModifierDto(string Id, string Name, decimal PriceDelta, int SortOrder, bool IsActive);
public sealed record ModifierGroupDto(string Id, string Name, int MinSelect, int MaxSelect, bool IsRequired, IReadOnlyList<ModifierDto> Modifiers);
public sealed record UpsertModifierGroupRequest(string Name, int MinSelect, int MaxSelect, bool IsRequired);
public sealed record UpsertModifierRequest(string Name, decimal PriceDelta, int SortOrder, bool IsActive = true);
public sealed record AssignModifierGroupsRequest(string[] GroupIds);
public sealed record AddBarcodeRequest(string Barcode);
public sealed record MenuTree(IReadOnlyList<MenuTreeCategory> Categories, IReadOnlyList<ModifierGroupDto> ModifierGroups);
public sealed record MenuTreeCategory(string Id, string Name, string? Color, int SortOrder, IReadOnlyList<MenuTreeItem> Items);
public sealed record MenuTreeItem(string Id, string Name, string? Description, decimal Price, decimal VatRate, string? PrepStation, int SortOrder, IReadOnlyList<string> ModifierGroupIds);
public sealed record ModifierFlat(string GroupId, string GroupName, string ModifierId, string Name, decimal PriceDelta, int SortOrder, bool IsRequired, bool IsActive);

public sealed record CustomerDto(
    string Id,
    string Phone,
    string? FullName,
    string? Email,
    DateTime? Birthday,
    string LoyaltyTier,
    int LoyaltyPoints,
    decimal TotalSpent,
    int VisitCount,
    bool SmsConsent,
    bool EmailConsent,
    bool IsBlocked,
    string? BlockReason,
    DateTimeOffset CreatedAt);

public sealed record CreateCustomerRequest(string Phone, string? FullName, string? Email, DateTime? Birthday, bool SmsConsent = true, bool EmailConsent = true);
public sealed record UpdateCustomerRequest(string? FullName, string? Email, DateTime? Birthday, string? Notes, string? Preferences, bool SmsConsent = true, bool EmailConsent = true);
public sealed record BlockCustomerRequest(string Reason);
public sealed record CreateCustomerAddressRequest(
    string Label,
    string AddressLine1,
    string? AddressLine2,
    string? District,
    string? City,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string? DeliveryNote,
    bool IsDefault = false);

public sealed record ReservationDto(
    string Id,
    long ReservationNo,
    string BranchId,
    string? CustomerId,
    string CustomerName,
    string CustomerPhone,
    DateTime ReservationDate,
    string ReservationTime,
    int GuestCount,
    string? TableId,
    string? Notes,
    string Status,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? SeatedAt,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt);

public sealed record CreateReservationRequest(string? CustomerId, string CustomerName, string CustomerPhone, DateTime ReservationDate, string ReservationTime, int GuestCount, string? TableId, string? Notes);
public sealed record SetReservationStatusRequest(string Status, string? Reason);
public sealed record CampaignDto(
    string Id,
    string? BranchId,
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinOrderAmount,
    int? UsageLimitPerCustomer,
    int? TotalUsageLimit,
    int UsageCount,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    bool IsActive,
    bool AutoApply,
    int Priority);

public sealed record CreateCampaignRequest(string? BranchId, string Code, string Name, string? Description, string DiscountType, decimal DiscountValue, decimal? MaxDiscountAmount, decimal? MinOrderAmount, int? UsageLimitPerCustomer, int? TotalUsageLimit, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool IsActive = true, bool AutoApply = false, int Priority = 10);
public sealed record ApplyCampaignRequest(string OrderId, string CampaignCode, string? CustomerId);
public sealed record DeliveryZoneDto(string Id, string BranchId, string Name, decimal CenterLat, decimal CenterLng, decimal RadiusKm, decimal DeliveryFee, decimal MinOrderAmount, decimal? FreeDeliveryOver, int EstimatedMinutes, bool IsActive);
public sealed record CreateDeliveryZoneRequest(string Name, decimal CenterLat, decimal CenterLng, decimal RadiusKm, decimal DeliveryFee, decimal MinOrderAmount, decimal? FreeDeliveryOver, int EstimatedMinutes, bool IsActive = true);
public sealed record CourierDto(string Id, string BranchId, string? UserId, string FullName, string Phone, string? LicensePlate, string VehicleType, string Status, string? CurrentOrderId, decimal? LastLat, decimal? LastLng, DateTimeOffset? LastLocationAt, int TotalDeliveries, decimal? Rating, bool IsActive);
public sealed record CreateCourierRequest(string? UserId, string FullName, string Phone, string? LicensePlate, string VehicleType = "motorbike", bool IsActive = true);
public sealed record SetCourierStatusRequest(string Status);
public sealed record DeliveryDto(string Id, string BranchId, string OrderId, string? CustomerId, string? CourierId, string? DeliveryZoneId, string DeliveryAddress, decimal? DeliveryLat, decimal? DeliveryLng, decimal DeliveryFee, int EstimatedMinutes, string Status, DateTimeOffset? AssignedAt, DateTimeOffset? PickedUpAt, DateTimeOffset? DeliveredAt, int? Rating, string? Feedback, DateTimeOffset CreatedAt);

public sealed record UnitDto(string Id, string Code, string Name);
public sealed record CreateUnitRequest(string Code, string Name);
public sealed record StockItemDto(string Id, string Name, string? Sku, string UnitId, string? UnitCode, decimal OnHand, decimal ReorderLevel, decimal UnitCost, bool IsActive);
public sealed record UpsertStockItemRequest(string Name, string? Sku, string UnitId, decimal ReorderLevel, decimal UnitCost, bool IsActive = true);
public sealed record AdjustStockRequest(decimal NewQuantity, string? Reason);
public sealed record SupplierDto(string Id, string Name, string? Phone, string? Email, string? TaxNo, bool IsActive);
public sealed record CreateSupplierRequest(string Name, string? Phone, string? Email, string? TaxNo);
public sealed record PurchaseLineInput(string StockItemId, decimal Quantity, decimal UnitCost);
public sealed record CreatePurchaseRequest(string? SupplierId, string? Note, PurchaseLineInput[] Lines);
public sealed record RecordWastageRequest(string StockItemId, decimal Quantity, string? Reason);
public sealed record StockMovementDto(string Id, string MoveType, decimal Quantity, decimal UnitCost, string? RefType, string? Note, DateTimeOffset CreatedAt);

public sealed record AddPaymentRequest(string Method, decimal Amount, decimal Tip = 0, string? Reference = null);
public sealed record RefundRequest(string? PaymentId, decimal Amount, string? Reason);
public sealed record PaymentLineDto(string Id, string Method, decimal Amount, decimal TipAmount, string? Reference, bool IsVoided, DateTimeOffset CreatedAt);
public sealed record PaymentResultDto(string OrderId, string PaymentId, bool Closed, decimal Change, decimal Balance, decimal OrderTotal, decimal PaidTotal, IReadOnlyList<PaymentLineDto> Payments);
public sealed record PaymentsViewDto(string OrderId, decimal OrderTotal, decimal PaidTotal, decimal Balance, string OrderStatus, IReadOnlyList<PaymentLineDto> Payments);

public sealed record RegisterDto(string Id, string Name, bool IsActive);
public sealed record CreateRegisterRequest(string Name);
public sealed record OpenSessionRequest(string RegisterId, decimal OpeningAmount);
public sealed record CashMoveRequest(decimal Amount, string? Note);
public sealed record CloseSessionRequest(decimal CountedAmount, string? Note);
public sealed record SessionDto(string Id, string RegisterId, string Status, decimal OpeningAmount, DateTimeOffset OpenedAt, decimal? ClosingCounted, decimal? ClosingExpected, decimal? Difference, DateTimeOffset? ClosedAt);
public sealed record CloseSessionResult(string SessionId, decimal Expected, decimal Counted, decimal Difference);
public sealed record PaymentBreakdownDto(string Method, decimal Amount, int Count);
public sealed record ZReportDto(string SessionId, string RegisterId, string Status, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt, decimal OpeningAmount, decimal ExpectedCash, decimal? CountedCash, decimal? Difference, decimal PayInTotal, decimal PayOutTotal, int OrderCount, decimal GrossSales, IReadOnlyList<PaymentBreakdownDto> PaymentBreakdown);

public sealed record FinanceAccountDto(string Id, string? BranchId, string Name, string AccountType, string Currency, decimal OpeningBalance, bool IsActive, DateTimeOffset CreatedAt);
public sealed record CreateFinanceAccountRequest(string Name, string AccountType, string? Currency, decimal OpeningBalance);
public sealed record CounterpartyDto(string Id, string CounterpartyType, string? RefId, string Name, string? Phone, string? Email, string? TaxNo, bool IsActive, DateTimeOffset CreatedAt);
public sealed record CreateCounterpartyRequest(string CounterpartyType, string? RefId, string Name, string? Phone, string? Email, string? TaxNo);
public sealed record FinanceTransactionDto(string Id, string BranchId, string? AccountId, string? CounterpartyId, string TransactionType, string Category, string Method, decimal Amount, decimal TaxAmount, DateTime BusinessDate, string? Description, string? SourceModule, string? SourceId, bool IsVoided, DateTimeOffset CreatedAt);
public sealed record CreateFinanceTransactionRequest(string? AccountId, string? CounterpartyId, string TransactionType, string Category, string Method, decimal Amount, decimal TaxAmount, DateTime? BusinessDate, string? Description);
public sealed record FinanceSummaryDto(string StartDate, string EndDate, decimal SalesRevenue, decimal OtherIncome, decimal Refunds, decimal PurchaseCosts, decimal Expenses, decimal CashIn, decimal CashOut, decimal Receivables, decimal Payables, decimal NetProfit);
public sealed record CashflowDayDto(string BusinessDate, decimal Income, decimal Expense, decimal Net);

public sealed record ReceiptLineDto(string Name, decimal Quantity, decimal UnitPrice, decimal LineTotal, string? Note);
public sealed record ReceiptPaymentDto(string Method, decimal Amount, decimal TipAmount, string? Reference);
public sealed record ReceiptDocumentDto(string OrderId, long OrderNo, string? TableName, string OrderType, string Status, long? InvoiceNo, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt, IReadOnlyList<ReceiptLineDto> Lines, IReadOnlyList<ReceiptPaymentDto> Payments, string PlainText, string Html);
public sealed record KitchenTicketLineDto(string Name, decimal Quantity, int CourseNo, string Status, string? Station, string? Note, string? Modifiers);
public sealed record KitchenTicketDocumentDto(string OrderId, long OrderNo, string? TableName, string OrderType, DateTimeOffset OpenedAt, IReadOnlyList<KitchenTicketLineDto> Lines, string PlainText, string Html);
public sealed record QueuePrintRequest(string? TerminalId, int Copies, string? PrinterName);
public sealed record PrintJobDto(string Id, string BranchId, string JobType, string OrderId, string? TerminalId, string Status, int Copies, string? ErrorMessage, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record IssueEInvoiceRequest(string? DocumentType, string? BuyerName, string? BuyerTaxNumber, string? BuyerTaxOffice, string? BuyerAddress, string? BuyerCity, string? BuyerEmail, string? Notes);
public sealed record EInvoiceDocumentDto(string Id, string? BranchId, string? OrderId, string? InvoiceId, string DocumentType, string Provider, string Scenario, string Status, string? ExternalId, string? Uuid, string? InvoiceNumber, string? BuyerName, string? BuyerTaxNo, string Currency, decimal Subtotal, decimal TaxTotal, decimal GrandTotal, string? PdfUrl, string? ErrorMessage, DateTimeOffset? IssuedAt, DateTimeOffset CreatedAt);

public sealed record ConnectorDto(string Id, string? BranchId, string Code, string Name, string ConnectorType, string ProviderCode, string? BaseUrl, string AuthType, string? SecretRef, string? Settings, string Status, bool IsActive, DateTimeOffset? LastSuccessAt, DateTimeOffset? LastFailureAt, string? FailureReason, DateTimeOffset CreatedAt, long RowVersion);
public sealed record CreateConnectorRequest(string Code, string Name, string ConnectorType, string ProviderCode, string? BranchId = null, string? BaseUrl = null, string AuthType = "none", string? SecretRef = null, string? Settings = null);
public sealed record SetConnectorStatusRequest(string Status, string? Reason = null);
public sealed record WebhookSubscriptionDto(string Id, string? BranchId, string? ConnectorId, string Name, string TargetUrl, string? SecretRef, string EventPattern, string? EventFilter, string? Headers, string Status, int MaxAttempts, int TimeoutSeconds, bool IsActive, DateTimeOffset CreatedAt, long RowVersion);
public sealed record CreateWebhookSubscriptionRequest(string Name, string TargetUrl, string? BranchId = null, string? ConnectorId = null, string? SecretRef = null, string EventPattern = "*", string? EventFilter = null, string? Headers = null, int MaxAttempts = 5, int TimeoutSeconds = 15);
public sealed record SetWebhookStatusRequest(string Status);
public sealed record IntegrationEventDto(string Id, string? BranchId, string SourceModule, string EventType, string AggregateType, string AggregateId, string Payload, string? CorrelationId, string Status, int Attempts, DateTimeOffset NextAttemptAt, DateTimeOffset CreatedAt, DateTimeOffset? ProcessedAt, long RowVersion);
public sealed record QueueIntegrationEventRequest(string SourceModule, string EventType, string AggregateType, string AggregateId, string Payload, string? BranchId = null, string? CorrelationId = null);
public sealed record TerminalDto(string Id, string BranchId, string? ConnectorId, string? DeviceId, string Name, string TerminalType, string? ProviderTerminalId, string ConnectionMode, string? IpAddress, int? Port, string? SerialPath, string? Settings, bool IsActive, DateTimeOffset? LastSeenAt, DateTimeOffset CreatedAt, long RowVersion);
public sealed record RegisterTerminalRequest(string Name, string TerminalType, string? BranchId = null, string? ConnectorId = null, string? DeviceId = null, string? ProviderTerminalId = null, string ConnectionMode = "cloud", string? IpAddress = null, int? Port = null, string? SerialPath = null, string? Settings = null);
public sealed record TerminalCommandDto(string Id, string BranchId, string? ConnectorId, string? TerminalId, string? OrderId, string? PaymentId, string CommandType, string? IdempotencyKey, string Payload, string Status, string? ProviderReference, string? ResultPayload, string? ErrorCode, string? ErrorMessage, string? RequestedBy, DateTimeOffset CreatedAt, DateTimeOffset? SentAt, DateTimeOffset? CompletedAt, long RowVersion);
public sealed record QueueTerminalCommandRequest(string CommandType, string Payload, string? BranchId = null, string? ConnectorId = null, string? TerminalId = null, string? OrderId = null, string? PaymentId = null, string? IdempotencyKey = null);
public sealed record MarkCommandSentRequest(string? ProviderReference = null);
public sealed record MarkCommandCompletedRequest(string? ProviderReference = null, string? ResultPayload = null);
public sealed record MarkCommandFailedRequest(string? ErrorCode = null, string? ErrorMessage = null, string? ResultPayload = null);

public sealed record SyncEntityDto(string EntityName, string TableName, bool IsBranchScoped, bool AllowClientPush, bool IsActive, int SortOrder);
public sealed record DeviceDto(string Id, string? BranchId, string Name, string DeviceType, string Fingerprint, bool IsApproved, DateTimeOffset? LastSeenAt);
public sealed record RegisterDeviceRequest(string Name, string DeviceType, string Fingerprint, string? BranchId = null);
public sealed record HeartbeatRequest(string? DeviceId, string? LocalStoreId, string? AppVersion);
public sealed record SyncChangeDto(long ChangeVersion, string Id, string? BranchId, string EntityName, string EntityId, string Operation, long? RowVersion, string? Payload, string? OriginDeviceId, string? OriginUserId, DateTimeOffset OccurredAt);
public sealed record PullChangesResponse(long HighWatermark, DateTimeOffset ServerTime, bool HasMore, IReadOnlyList<SyncChangeDto> Changes);
public sealed record AckPullRequest(string? DeviceId, long LastPullVersion);
public sealed record ClientMutationRequest(string ClientMutationId, string EntityName, string EntityId, string Operation, long? BaseChangeVersion, long? ExpectedRowVersion, string? Payload);
public sealed record PushChangesRequest(string? DeviceId, IReadOnlyList<ClientMutationRequest> Mutations);
public sealed record MutationResultDto(string ClientMutationId, string MutationId, string Status);
public sealed record PushChangesResponse(long HighWatermark, IReadOnlyList<MutationResultDto> Results);
public sealed record PendingMutationDto(string Id, string? BranchId, string DeviceId, string ClientMutationId, string EntityName, string EntityId, string Operation, long? BaseChangeVersion, long? ExpectedRowVersion, string? Payload, string Status, string? ErrorCode, string? ErrorMessage, DateTimeOffset CreatedAt);
public sealed record SyncConflictDto(string Id, string? BranchId, string DeviceId, string MutationId, string EntityName, string EntityId, long? ServerChangeVersion, string? ClientPayload, string? ServerPayload, string ResolutionStatus, DateTimeOffset CreatedAt);

public sealed record DashboardMetric(string Label, string Value, string Detail);
