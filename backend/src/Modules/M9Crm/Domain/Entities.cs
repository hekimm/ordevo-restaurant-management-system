namespace Ordevo.Modules.M9Crm.Domain;

public sealed class Customer
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Notes { get; set; }
    public string? Preferences { get; set; }
    public string LoyaltyTier { get; set; } = "bronze";
    public int LoyaltyPoints { get; set; }
    public decimal TotalSpent { get; set; }
    public int VisitCount { get; set; }
    public bool SmsConsent { get; set; }
    public bool EmailConsent { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed class CustomerAddress
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string AddressLine1 { get; set; } = default!;
    public string? AddressLine2 { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? DeliveryNote { get; set; }
    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class LoyaltyTransaction
{
    public string Id { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string TransactionType { get; set; } = default!;
    public int Points { get; set; }
    public int BalanceAfter { get; set; }
    public string? OrderId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Campaign
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string? BranchId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = default!;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimitPerCustomer { get; set; }
    public int? TotalUsageLimit { get; set; }
    public int UsageCount { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public bool IsActive { get; set; }
    public bool AutoApply { get; set; }
    public int Priority { get; set; }
}

public sealed class Reservation
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public long ReservationNo { get; set; }
    public string? CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public string CustomerPhone { get; set; } = default!;
    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = default!;
    public int GuestCount { get; set; }
    public string? TableId { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? SeatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DeliveryZone
{
    public string Id { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    public decimal RadiusKm { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal? FreeDeliveryOver { get; set; }
    public int EstimatedMinutes { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Courier
{
    public string Id { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string? UserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? LicensePlate { get; set; }
    public string VehicleType { get; set; } = default!;
    public string Status { get; set; } = "off_duty";
    public string? CurrentOrderId { get; set; }
    public decimal? LastLat { get; set; }
    public decimal? LastLng { get; set; }
    public DateTimeOffset? LastLocationAt { get; set; }
    public int TotalDeliveries { get; set; }
    public decimal? Rating { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Delivery
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string BranchId { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string? CustomerId { get; set; }
    public string? CourierId { get; set; }
    public string? DeliveryZoneId { get; set; }
    public string DeliveryAddress { get; set; } = default!;
    public decimal? DeliveryLat { get; set; }
    public decimal? DeliveryLng { get; set; }
    public decimal DeliveryFee { get; set; }
    public int EstimatedMinutes { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public int? Rating { get; set; }
    public string? Feedback { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
