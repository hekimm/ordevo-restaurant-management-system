using Ordevo.Modules.M9Crm.Domain;

namespace Ordevo.Modules.M9Crm.Application;

public interface IM9CrmRepository
{
    Task<Customer?> GetCustomerAsync(string tenantId, string customerId, CancellationToken ct = default);
    Task<Customer?> GetCustomerByPhoneAsync(string tenantId, string phone, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> SearchCustomersAsync(string tenantId, string? search, int skip, int take, CancellationToken ct = default);
    Task<int> DeleteCustomerAsync(string tenantId, string customerId, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerAddress>> ListCustomerAddressesAsync(string tenantId, string customerId, CancellationToken ct = default);
    Task<IReadOnlyList<LoyaltyTransaction>> ListLoyaltyAsync(string tenantId, string customerId, int take, CancellationToken ct = default);

    Task<Campaign?> GetCampaignAsync(string tenantId, string campaignId, CancellationToken ct = default);
    Task<Campaign?> GetCampaignByCodeAsync(string tenantId, string code, CancellationToken ct = default);
    Task<IReadOnlyList<Campaign>> ListCampaignsAsync(string tenantId, string? branchId, bool activeOnly, CancellationToken ct = default);
    Task<Campaign> InsertCampaignAsync(string id, string tenantId, CreateCampaignRequest request, CancellationToken ct = default);

    Task<Reservation?> GetReservationAsync(string tenantId, string reservationId, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> ListReservationsAsync(string tenantId, string branchId, DateTime date, CancellationToken ct = default);
    Task<Reservation?> UpdateReservationAsync(string tenantId, string reservationId, CreateReservationRequest request, string userId, CancellationToken ct = default);
    Task<int> DeleteReservationAsync(string tenantId, string reservationId, CancellationToken ct = default);

    Task<IReadOnlyList<DeliveryZone>> ListDeliveryZonesAsync(string tenantId, string branchId, CancellationToken ct = default);
    Task<DeliveryZone> InsertDeliveryZoneAsync(string id, string tenantId, string branchId, CreateDeliveryZoneRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<Courier>> ListCouriersAsync(string tenantId, string branchId, string? status, CancellationToken ct = default);
    Task<Courier?> GetCourierAsync(string tenantId, string courierId, CancellationToken ct = default);
    Task<Courier> InsertCourierAsync(string id, string tenantId, string branchId, CreateCourierRequest request, CancellationToken ct = default);

    Task<Delivery?> GetDeliveryAsync(string tenantId, string deliveryId, CancellationToken ct = default);
    Task<Delivery?> GetDeliveryByOrderAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Delivery>> ListActiveDeliveriesAsync(string tenantId, string branchId, CancellationToken ct = default);
}

public interface IM9CrmProcedures
{
    Task<string> CreateCustomerAsync(string tenantId, CreateCustomerRequest request, string userId, CancellationToken ct = default);
    Task UpdateCustomerAsync(string tenantId, string customerId, UpdateCustomerRequest request, string userId, CancellationToken ct = default);
    Task BlockCustomerAsync(string tenantId, string customerId, string reason, string userId, CancellationToken ct = default);
    Task UnblockCustomerAsync(string tenantId, string customerId, string userId, CancellationToken ct = default);
    Task<string> AddCustomerAddressAsync(string tenantId, string customerId, CreateCustomerAddressRequest request, string userId, CancellationToken ct = default);

    Task<string> AddLoyaltyPointsAsync(string tenantId, LoyaltyPointsRequest request, string userId, CancellationToken ct = default);
    Task<string> RedeemLoyaltyPointsAsync(string tenantId, RedeemLoyaltyRequest request, string userId, CancellationToken ct = default);
    Task<string> AdjustLoyaltyPointsAsync(string tenantId, AdjustLoyaltyRequest request, string userId, CancellationToken ct = default);

    Task<decimal> CalculateCampaignDiscountAsync(string tenantId, string orderId, string? customerId, string campaignCode, CancellationToken ct = default);
    Task<CampaignApplyResult> ApplyCampaignAsync(string tenantId, ApplyCampaignRequest request, string userId, CancellationToken ct = default);

    Task<(string ReservationId, long ReservationNo)> CreateReservationAsync(
        string tenantId, string branchId, CreateReservationRequest request, string userId, CancellationToken ct = default);
    Task SetReservationStatusAsync(string tenantId, string reservationId, string status, string? reason, string userId, CancellationToken ct = default);

    Task<string> CreateDeliveryAsync(string tenantId, string branchId, CreateDeliveryRequest request, string userId, CancellationToken ct = default);
    Task AssignCourierAsync(string tenantId, string deliveryId, string? courierId, string userId, CancellationToken ct = default);
    Task SetCourierStatusAsync(string tenantId, string courierId, string status, CancellationToken ct = default);
    Task SetDeliveryStatusAsync(string tenantId, string deliveryId, string status, string userId, CancellationToken ct = default);
    Task RateDeliveryAsync(string tenantId, string deliveryId, int rating, string? feedback, CancellationToken ct = default);
    Task UpdateCourierLocationAsync(string tenantId, string courierId, decimal latitude, decimal longitude, CancellationToken ct = default);
}
