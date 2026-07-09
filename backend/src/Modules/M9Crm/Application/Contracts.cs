namespace Ordevo.Modules.M9Crm.Application;

public sealed record CustomerDto(
    string Id, string Phone, string? FullName, string? Email, DateTime? Birthday,
    string LoyaltyTier, int LoyaltyPoints, decimal TotalSpent, int VisitCount,
    bool SmsConsent, bool EmailConsent, bool IsBlocked, string? BlockReason,
    DateTimeOffset CreatedAt);

public sealed record CustomerDetailDto(
    CustomerDto Customer,
    IReadOnlyList<CustomerAddressDto> Addresses,
    IReadOnlyList<LoyaltyTransactionDto> LoyaltyHistory);

public sealed record CreateCustomerRequest(
    string Phone, string? FullName, string? Email, DateTime? Birthday,
    bool SmsConsent = true, bool EmailConsent = true);

public sealed record UpdateCustomerRequest(
    string? FullName, string? Email, DateTime? Birthday, string? Notes, string? Preferences,
    bool SmsConsent = true, bool EmailConsent = true);

public sealed record BlockCustomerRequest(string Reason);

public sealed record CustomerAddressDto(
    string Id, string CustomerId, string Label, string AddressLine1, string? AddressLine2,
    string? District, string? City, string? PostalCode, decimal? Latitude, decimal? Longitude,
    string? DeliveryNote, bool IsDefault);

public sealed record CreateCustomerAddressRequest(
    string Label, string AddressLine1, string? AddressLine2, string? District, string? City,
    string? PostalCode, decimal? Latitude, decimal? Longitude, string? DeliveryNote,
    bool IsDefault = false);

public sealed record LoyaltyTransactionDto(
    string Id, string CustomerId, string TransactionType, int Points, int BalanceAfter,
    string? OrderId, string? Reason, DateTimeOffset CreatedAt);

public sealed record LoyaltyPointsRequest(string CustomerId, string? OrderId, int Points, string? Reason);
public sealed record RedeemLoyaltyRequest(string CustomerId, string OrderId, int Points);
public sealed record AdjustLoyaltyRequest(string CustomerId, int Points, string Reason);

public sealed record CampaignDto(
    string Id, string? BranchId, string Code, string Name, string? Description,
    string DiscountType, decimal DiscountValue, decimal? MaxDiscountAmount,
    decimal? MinOrderAmount, int? UsageLimitPerCustomer, int? TotalUsageLimit,
    int UsageCount, DateTimeOffset StartsAt, DateTimeOffset? EndsAt,
    bool IsActive, bool AutoApply, int Priority);

public sealed record CreateCampaignRequest(
    string? BranchId, string Code, string Name, string? Description,
    string DiscountType, decimal DiscountValue, decimal? MaxDiscountAmount,
    decimal? MinOrderAmount, int? UsageLimitPerCustomer, int? TotalUsageLimit,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool IsActive = true,
    bool AutoApply = false, int Priority = 10);

public sealed record ApplyCampaignRequest(string OrderId, string CampaignCode, string? CustomerId);
public sealed record CampaignApplyResult(string UsageId, decimal DiscountAmount);

public sealed record ReservationDto(
    string Id, long ReservationNo, string BranchId, string? CustomerId, string CustomerName,
    string CustomerPhone, DateTime ReservationDate, string ReservationTime, int GuestCount,
    string? TableId, string? Notes, string Status, DateTimeOffset? ConfirmedAt,
    DateTimeOffset? SeatedAt, DateTimeOffset? CancelledAt, string? CancelReason,
    DateTimeOffset CreatedAt);

public sealed record CreateReservationRequest(
    string? CustomerId, string CustomerName, string CustomerPhone,
    DateTime ReservationDate, string ReservationTime, int GuestCount,
    string? TableId, string? Notes);

public sealed record SetReservationStatusRequest(string Status, string? Reason);

public sealed record DeliveryZoneDto(
    string Id, string BranchId, string Name, decimal CenterLat, decimal CenterLng,
    decimal RadiusKm, decimal DeliveryFee, decimal MinOrderAmount,
    decimal? FreeDeliveryOver, int EstimatedMinutes, bool IsActive);

public sealed record CreateDeliveryZoneRequest(
    string Name, decimal CenterLat, decimal CenterLng, decimal RadiusKm,
    decimal DeliveryFee, decimal MinOrderAmount, decimal? FreeDeliveryOver,
    int EstimatedMinutes, bool IsActive = true);

public sealed record CourierDto(
    string Id, string BranchId, string? UserId, string FullName, string Phone,
    string? LicensePlate, string VehicleType, string Status, string? CurrentOrderId,
    decimal? LastLat, decimal? LastLng, DateTimeOffset? LastLocationAt,
    int TotalDeliveries, decimal? Rating, bool IsActive);

public sealed record CreateCourierRequest(
    string? UserId, string FullName, string Phone, string? LicensePlate,
    string VehicleType = "motorbike", bool IsActive = true);

public sealed record SetCourierStatusRequest(string Status);
public sealed record UpdateCourierLocationRequest(decimal Latitude, decimal Longitude);

public sealed record DeliveryDto(
    string Id, string BranchId, string OrderId, string? CustomerId, string? CourierId,
    string? DeliveryZoneId, string DeliveryAddress, decimal? DeliveryLat, decimal? DeliveryLng,
    decimal DeliveryFee, int EstimatedMinutes, string Status, DateTimeOffset? AssignedAt,
    DateTimeOffset? PickedUpAt, DateTimeOffset? DeliveredAt, int? Rating,
    string? Feedback, DateTimeOffset CreatedAt);

public sealed record CreateDeliveryRequest(
    string OrderId, string? CustomerId, string? DeliveryZoneId, string DeliveryAddress,
    decimal? DeliveryLat, decimal? DeliveryLng, decimal DeliveryFee, int EstimatedMinutes);

public sealed record AssignCourierRequest(string? CourierId);
public sealed record SetDeliveryStatusRequest(string Status);
public sealed record RateDeliveryRequest(int Rating, string? Feedback);
