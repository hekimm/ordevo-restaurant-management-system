namespace Ordevo.Modules.Ordering.Application;

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
    string Id, string MenuItemId, string Name, decimal UnitPrice, decimal Quantity,
    decimal ModifierTotal, decimal LineTotal, decimal VatRate, int CourseNo,
    string Status, bool IsComp, string? Note, IReadOnlyList<OrderItemModifierDto> Modifiers);

public sealed record OrderDto(
    string Id, long OrderNo, string? TableId, string OrderType, string Status, int GuestCount,
    decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal Total,
    string? Note, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderSummaryDto(
    string Id, long OrderNo, string? TableId, string OrderType, string Status,
    decimal Total, DateTimeOffset OpenedAt, int ItemCount);
