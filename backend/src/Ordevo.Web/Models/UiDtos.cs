namespace Ordevo.Web.Models;

public sealed record DailyStatsDto(string Date, int OrderCount, decimal Revenue, decimal ItemCount, decimal AvgTicket);
public sealed record HourlyPointDto(int Hour, int OrderCount, decimal Revenue);
public sealed record PaymentMethodDto(string Method, decimal Amount, int Count);
public sealed record TopItemDto(string Name, string? Category, decimal Quantity, decimal Revenue);
public sealed record CategorySalesDto(string Category, decimal Quantity, decimal Revenue);
public sealed record DailySummaryDto(string BusinessDate, int OrderCount, decimal Revenue, decimal TaxTotal, decimal DiscountTotal);

public sealed record TableDto(string Id, string? SectionId, string Name, int Capacity, string Status, int SortOrder, bool IsActive);
public sealed record SectionDto(string Id, string Name, int SortOrder);
public sealed record UserSummaryDto(string Id, string Email, string FullName, bool IsActive, string[] Roles);

public sealed record OrderSummaryDto(string Id, long OrderNo, string? TableId, string OrderType, string Status, decimal Total, DateTimeOffset OpenedAt, int ItemCount);
public sealed record OrderItemDto(string Id, string MenuItemId, string Name, decimal UnitPrice, decimal Quantity, decimal LineTotal, string Status, bool IsComp, string? Note);
public sealed record OrderDto(
    string Id, long OrderNo, string? TableId, string OrderType, string Status, int GuestCount,
    decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total,
    string? Note, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record MenuTree(IReadOnlyList<MenuTreeCategory> Categories, IReadOnlyList<ModifierGroupDto> ModifierGroups);
public sealed record MenuTreeCategory(string Id, string Name, string? Color, int SortOrder, IReadOnlyList<MenuTreeItem> Items);
public sealed record MenuTreeItem(string Id, string Name, string? Description, decimal Price, decimal VatRate, string? PrepStation, int SortOrder, IReadOnlyList<string> ModifierGroupIds);
public sealed record CategoryDto(string Id, string Name, string? Color, int SortOrder, bool IsActive);
public sealed record MenuItemDto(string Id, string CategoryId, string Name, string? Description, decimal Price, decimal VatRate, string? Sku, string? ImageUrl, string? PrepStation, int SortOrder, bool IsActive);
public sealed record ModifierGroupDto(string Id, string Name, int MinSelect, int MaxSelect, bool IsRequired, IReadOnlyList<ModifierDto> Modifiers);
public sealed record ModifierDto(string Id, string Name, decimal PriceDelta, int SortOrder, bool IsActive);

public sealed record KdsTicketDto(string OrderId, long OrderNo, string? TableName, DateTimeOffset OpenedAt, int ElapsedSeconds, IReadOnlyList<KdsItemDto> Items);
public sealed record KdsItemDto(
    string OrderItemId, string ItemName, decimal Quantity, int CourseNo, string Status,
    string? Station, string? Note, string? Modifiers, int ElapsedSeconds,
    DateTimeOffset CreatedAt, bool IsAdditional);

public sealed record CustomerDto(
    string Id, string Phone, string? FullName, string? Email, DateTime? Birthday,
    string LoyaltyTier, int LoyaltyPoints, decimal TotalSpent, int VisitCount,
    bool SmsConsent, bool EmailConsent, bool IsBlocked, string? BlockReason,
    DateTimeOffset CreatedAt);

public sealed record ReservationDto(
    string Id, long ReservationNo, string BranchId, string? CustomerId, string CustomerName,
    string CustomerPhone, DateTime ReservationDate, string ReservationTime, int GuestCount,
    string? TableId, string? Notes, string Status, DateTimeOffset? ConfirmedAt,
    DateTimeOffset? SeatedAt, DateTimeOffset? CancelledAt, string? CancelReason,
    DateTimeOffset CreatedAt);

public sealed record StockItemDto(string Id, string Name, string? Sku, string UnitId, string? UnitCode, decimal OnHand, decimal ReorderLevel, decimal UnitCost, bool IsActive);
public sealed record UnitDto(string Id, string Code, string Name);
public sealed record SupplierDto(string Id, string Name, string? Phone, string? Email, string? TaxNo, bool IsActive);
public sealed record PurchaseLineDto(string StockItemId, string? StockItemName, decimal Quantity, decimal UnitCost, decimal LineTotal);
public sealed record PurchaseDto(string Id, string? SupplierId, string Status, decimal Total, string? Note, IReadOnlyList<PurchaseLineDto> Lines);

public sealed record RegisterDto(string Id, string Name, bool IsActive);

public sealed record FinanceAccountDto(string Id, string? BranchId, string Name, string AccountType, string Currency, decimal OpeningBalance, bool IsActive, DateTimeOffset CreatedAt);

public sealed record CounterpartyDto(string Id, string CounterpartyType, string? RefId, string Name, string? Phone, string? Email, string? TaxNo, bool IsActive, DateTimeOffset CreatedAt);

public sealed record FinanceTransactionDto(
    string Id, string BranchId, string? AccountId, string? CounterpartyId,
    string TransactionType, string Category, string Method, decimal Amount,
    decimal TaxAmount, DateTime BusinessDate, string? Description,
    string? SourceModule, string? SourceId, bool IsVoided, DateTimeOffset CreatedAt);

public sealed record FinanceSummaryDto(
    string StartDate, string EndDate, decimal SalesRevenue, decimal OtherIncome,
    decimal Refunds, decimal PurchaseCosts, decimal Expenses, decimal CashIn,
    decimal CashOut, decimal Receivables, decimal Payables, decimal NetProfit);

public sealed record CashflowDayDto(string BusinessDate, decimal Income, decimal Expense, decimal Net);

public sealed record ReceiptLineDto(string Name, decimal Quantity, decimal UnitPrice, decimal LineTotal, string? Note);
public sealed record ReceiptPaymentDto(string Method, decimal Amount, decimal TipAmount, string? Reference);
public sealed record ReceiptDocumentDto(
    string OrderId, long OrderNo, string? TableName, string OrderType, string Status,
    long? InvoiceNo, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal,
    decimal Total, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt,
    IReadOnlyList<ReceiptLineDto> Lines, IReadOnlyList<ReceiptPaymentDto> Payments,
    string PlainText, string Html);

public sealed record KitchenTicketLineDto(string Name, decimal Quantity, int CourseNo, string Status, string? Station, string? Note, string? Modifiers);
public sealed record KitchenTicketDocumentDto(
    string OrderId, long OrderNo, string? TableName, string OrderType,
    DateTimeOffset OpenedAt, IReadOnlyList<KitchenTicketLineDto> Lines,
    string PlainText, string Html);

public sealed record PrintJobDto(
    string Id, string BranchId, string JobType, string OrderId, string? TerminalId,
    string Status, int Copies, string? ErrorMessage, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SyncEntityDto(string EntityName, string TableName, bool IsBranchScoped, bool AllowClientPush, bool IsActive, int SortOrder);

public sealed record PendingMutationDto(
    string Id, string? BranchId, string DeviceId, string ClientMutationId, string EntityName,
    string EntityId, string Operation, long? BaseChangeVersion, long? ExpectedRowVersion,
    string? Payload, string Status, string? ErrorCode, string? ErrorMessage, DateTimeOffset CreatedAt);

public sealed record ConnectorDto(
    string Id, string? BranchId, string Code, string Name, string ConnectorType,
    string ProviderCode, string? BaseUrl, string AuthType, string? SecretRef,
    string? Settings, string Status, bool IsActive, DateTimeOffset? LastSuccessAt,
    DateTimeOffset? LastFailureAt, string? FailureReason, DateTimeOffset CreatedAt,
    long RowVersion);

public sealed record IntegrationEventDto(
    string Id, string? BranchId, string SourceModule, string EventType,
    string AggregateType, string AggregateId, string Payload, string? CorrelationId,
    string Status, int Attempts, DateTimeOffset NextAttemptAt, DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt, long RowVersion);

public sealed record TerminalCommandDto(
    string Id, string BranchId, string? ConnectorId, string? TerminalId,
    string? OrderId, string? PaymentId, string CommandType, string? IdempotencyKey,
    string Payload, string Status, string? ProviderReference, string? ResultPayload,
    string? ErrorCode, string? ErrorMessage, string? RequestedBy,
    DateTimeOffset CreatedAt, DateTimeOffset? SentAt, DateTimeOffset? CompletedAt,
    long RowVersion);

public sealed record TerminalDto(
    string Id, string BranchId, string? ConnectorId, string? DeviceId,
    string Name, string TerminalType, string? ProviderTerminalId,
    string ConnectionMode, string? IpAddress, int? Port, string? SerialPath,
    string? Settings, int IsActive, DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedAt, long RowVersion);

public sealed record FiscalOverviewDto(
    string PaymentTerminalProvider,
    string EAdisyonProvider,
    bool EAdisyonEnabled,
    int ActiveTerminalCount,
    int OpenCommandCount,
    IReadOnlyList<FiscalTerminalDto> Terminals,
    IReadOnlyList<FiscalTransactionDto> RecentTransactions);

public sealed record FiscalTerminalDto(
    string Id, string Name, string TerminalType, string? ProviderTerminalId,
    string ConnectionMode, bool IsActive, DateTimeOffset? LastSeenAt);

public sealed record FiscalTransactionDto(
    string Id, string? BranchId, string? OrderId, string? PaymentId, string? CommandId,
    string? TerminalId, string Provider, string Method, decimal Amount, decimal TipAmount,
    string Currency, string Status, string? AuthorizationCode, string? BatchNo,
    string? Stan, string? Rrn, string? FiscalReceiptNo, string? ZNo, string? DeviceSerial,
    string? DocumentUuid, string? ErrorCode, string? ErrorMessage,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

public sealed record FiscalPaymentResultDto(
    string FiscalTransactionId, string Status, string UserMessage, string? CommandId,
    string? PaymentId, bool OrderClosed, decimal Change, decimal Balance,
    string? ProviderReference, string? AuthorizationCode, string? Rrn,
    string? FiscalReceiptNo, string? EInvoiceDocumentId, string? EInvoiceStatus);

public sealed record DeveloperToggleDto(
    string Code, string Name, string Description, string Route, bool IsEnabled);

public sealed record DeveloperSettingsDto(
    IReadOnlyList<DeveloperToggleDto> Modules,
    IReadOnlyList<DeveloperToggleDto> Integrations);

public sealed record SessionDto(
    string Id, string RegisterId, string Status, decimal OpeningAmount,
    DateTimeOffset OpenedAt, decimal? ClosingCounted, decimal? ClosingExpected,
    decimal? Difference, DateTimeOffset? ClosedAt);

public sealed record ZReportDto(
    string SessionId, string RegisterId, string Status,
    DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt,
    decimal OpeningAmount, decimal ExpectedCash, decimal? CountedCash,
    decimal? Difference, decimal PayInTotal, decimal PayOutTotal,
    int OrderCount, decimal GrossSales,
    IReadOnlyList<PaymentBreakdownDto> PaymentBreakdown);

public sealed record PaymentBreakdownDto(string Method, decimal Amount, int Count);
