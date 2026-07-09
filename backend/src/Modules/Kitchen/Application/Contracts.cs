namespace Ordevo.Modules.Kitchen.Application;

public sealed class KdsStation
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record StationDto(string Id, string Name, string Code, int SortOrder, bool IsActive);
public sealed record UpsertStationRequest(string Name, string Code, int SortOrder, bool IsActive = true);

public sealed class KdsItemRow
{
    public string OrderItemId { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public long OrderNo { get; set; }
    public string? TableName { get; set; }
    public string ItemName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public int CourseNo { get; set; }
    public string Status { get; set; } = default!;
    public string? Station { get; set; }
    public string? Note { get; set; }
    public string? Modifiers { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsAdditional { get; set; }
}

public sealed record KdsItemDto(
    string OrderItemId, string ItemName, decimal Quantity, int CourseNo, string Status,
    string? Station, string? Note, string? Modifiers, int ElapsedSeconds,
    DateTimeOffset CreatedAt, bool IsAdditional);

public sealed record KdsTicketDto(
    string OrderId, long OrderNo, string? TableName, DateTimeOffset OpenedAt,
    int ElapsedSeconds, IReadOnlyList<KdsItemDto> Items);

public sealed record SetItemStatusRequest(string Status);
