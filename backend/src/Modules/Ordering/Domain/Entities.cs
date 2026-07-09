namespace Ordevo.Modules.Ordering.Domain;

public sealed class TableSection
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int SortOrder { get; set; }
}

public sealed class DiningTable
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string? SectionId { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public string Status { get; set; } = "idle";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Order
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public long OrderNo { get; set; }
    public string? TableId { get; set; }
    public string OrderType { get; set; } = "dinein";
    public string Status { get; set; } = "open";
    public int GuestCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class OrderItem
{
    public string Id { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string MenuItemId { get; set; } = default!;
    public string NameSnapshot { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal ModifierTotal { get; set; }
    public decimal LineTotal { get; set; }
    public decimal VatRate { get; set; }
    public int CourseNo { get; set; }
    public string Status { get; set; } = "pending";
    public bool IsComp { get; set; }
    public string? Note { get; set; }
}

public sealed class OrderItemModifier
{
    public string Id { get; set; } = default!;
    public string OrderItemId { get; set; } = default!;
    public string? ModifierId { get; set; }
    public string NameSnapshot { get; set; } = default!;
    public decimal PriceDelta { get; set; }
}
