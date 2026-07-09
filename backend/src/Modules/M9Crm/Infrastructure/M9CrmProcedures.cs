using System.Data;
using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.M9Crm.Application;

namespace Ordevo.Modules.M9Crm.Infrastructure;

public sealed class M9CrmProcedures(IDbConnectionFactory factory) : IM9CrmProcedures
{
    public async Task<string> CreateCustomerAsync(
        string tenantId, CreateCustomerRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_phone", request.Phone);
        p.Add("p_full_name", request.FullName);
        p.Add("p_email", request.Email);
        p.Add("p_birthday", request.Birthday, DbType.Date);
        p.Add("p_sms_consent", request.SmsConsent ? 1 : 0);
        p.Add("p_email_consent", request.EmailConsent ? 1 : 0);
        p.Add("p_user_id", userId);
        p.Add("p_customer_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_M9_CRM.CREATE_CUSTOMER", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_customer_id");
    }

    public async Task UpdateCustomerAsync(
        string tenantId, string customerId, UpdateCustomerRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_customer_id", customerId);
        p.Add("p_full_name", request.FullName);
        p.Add("p_email", request.Email);
        p.Add("p_birthday", request.Birthday, DbType.Date);
        p.Add("p_notes", request.Notes);
        p.Add("p_preferences", request.Preferences);
        p.Add("p_sms_consent", request.SmsConsent ? 1 : 0);
        p.Add("p_email_consent", request.EmailConsent ? 1 : 0);
        p.Add("p_user_id", userId);
        await db.ExecuteAsync("PKG_M9_CRM.UPDATE_CUSTOMER", p, commandType: CommandType.StoredProcedure);
    }

    public Task BlockCustomerAsync(string tenantId, string customerId, string reason, string userId, CancellationToken ct = default)
        => ExecAsync("PKG_M9_CRM.BLOCK_CUSTOMER", ct,
            ("p_tenant_id", tenantId), ("p_customer_id", customerId), ("p_reason", reason), ("p_user_id", userId));

    public Task UnblockCustomerAsync(string tenantId, string customerId, string userId, CancellationToken ct = default)
        => ExecAsync("PKG_M9_CRM.UNBLOCK_CUSTOMER", ct,
            ("p_tenant_id", tenantId), ("p_customer_id", customerId), ("p_user_id", userId));

    public async Task<string> AddCustomerAddressAsync(
        string tenantId, string customerId, CreateCustomerAddressRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_customer_id", customerId);
        p.Add("p_label", request.Label);
        p.Add("p_address_line1", request.AddressLine1);
        p.Add("p_address_line2", request.AddressLine2);
        p.Add("p_district", request.District);
        p.Add("p_city", request.City);
        p.Add("p_postal_code", request.PostalCode);
        p.Add("p_latitude", request.Latitude);
        p.Add("p_longitude", request.Longitude);
        p.Add("p_delivery_note", request.DeliveryNote);
        p.Add("p_is_default", request.IsDefault ? 1 : 0);
        p.Add("p_user_id", userId);
        p.Add("p_address_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_M9_CRM.ADD_CUSTOMER_ADDRESS", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_address_id");
    }

    public async Task<string> AddLoyaltyPointsAsync(
        string tenantId, LoyaltyPointsRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_customer_id", request.CustomerId);
        p.Add("p_order_id", request.OrderId);
        p.Add("p_points", request.Points);
        p.Add("p_reason", request.Reason);
        p.Add("p_user_id", userId);
        p.Add("p_txn_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_M9_CRM.ADD_LOYALTY_POINTS", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_txn_id");
    }

    public async Task<string> RedeemLoyaltyPointsAsync(
        string tenantId, RedeemLoyaltyRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_customer_id", request.CustomerId);
        p.Add("p_order_id", request.OrderId);
        p.Add("p_points", request.Points);
        p.Add("p_user_id", userId);
        p.Add("p_txn_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_M9_CRM.REDEEM_LOYALTY_POINTS", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_txn_id");
    }

    public async Task<string> AdjustLoyaltyPointsAsync(
        string tenantId, AdjustLoyaltyRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_customer_id", request.CustomerId);
        p.Add("p_points", request.Points);
        p.Add("p_reason", request.Reason);
        p.Add("p_user_id", userId);
        p.Add("p_txn_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_M9_CRM.ADJUST_LOYALTY_POINTS", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_txn_id");
    }

    public async Task<decimal> CalculateCampaignDiscountAsync(
        string tenantId, string orderId, string? customerId, string campaignCode, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<decimal>(
            """
            SELECT PKG_M9_CRM.CALCULATE_CAMPAIGN_DISCOUNT(:tenantId, :orderId, :customerId, :campaignCode)
            FROM DUAL
            """,
            new OracleParams(new { tenantId, orderId, customerId, campaignCode }));
    }

    public async Task<CampaignApplyResult> ApplyCampaignAsync(
        string tenantId, ApplyCampaignRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_order_id", request.OrderId);
        p.Add("p_customer_id", request.CustomerId);
        p.Add("p_campaign_code", request.CampaignCode);
        p.Add("p_user_id", userId);
        p.Add("p_usage_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        p.Add("p_discount_amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
        await db.ExecuteAsync("PKG_M9_CRM.APPLY_CAMPAIGN", p, commandType: CommandType.StoredProcedure);
        return new CampaignApplyResult(p.Get<string>("p_usage_id"), p.Get<decimal>("p_discount_amount"));
    }

    public async Task<(string ReservationId, long ReservationNo)> CreateReservationAsync(
        string tenantId, string branchId, CreateReservationRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_customer_id", request.CustomerId);
        p.Add("p_customer_name", request.CustomerName);
        p.Add("p_customer_phone", request.CustomerPhone);
        p.Add("p_reservation_date", request.ReservationDate.Date, DbType.Date);
        p.Add("p_reservation_time", request.ReservationTime);
        p.Add("p_guest_count", request.GuestCount);
        p.Add("p_table_id", request.TableId);
        p.Add("p_notes", request.Notes);
        p.Add("p_user_id", userId);
        p.Add("p_reservation_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        p.Add("p_reservation_no", dbType: DbType.Int64, direction: ParameterDirection.Output);
        await db.ExecuteAsync("PKG_M9_CRM.CREATE_RESERVATION", p, commandType: CommandType.StoredProcedure);
        return (p.Get<string>("p_reservation_id"), p.Get<long>("p_reservation_no"));
    }

    public Task SetReservationStatusAsync(
        string tenantId, string reservationId, string status, string? reason, string userId, CancellationToken ct = default)
        => ExecAsync("PKG_M9_CRM.SET_RESERVATION_STATUS", ct,
            ("p_tenant_id", tenantId), ("p_reservation_id", reservationId),
            ("p_status", status), ("p_reason", reason), ("p_user_id", userId));

    public async Task<string> CreateDeliveryAsync(
        string tenantId, string branchId, CreateDeliveryRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        p.Add("p_tenant_id", tenantId);
        p.Add("p_branch_id", branchId);
        p.Add("p_order_id", request.OrderId);
        p.Add("p_customer_id", request.CustomerId);
        p.Add("p_zone_id", request.DeliveryZoneId);
        p.Add("p_delivery_address", request.DeliveryAddress);
        p.Add("p_delivery_lat", request.DeliveryLat);
        p.Add("p_delivery_lng", request.DeliveryLng);
        p.Add("p_delivery_fee", request.DeliveryFee);
        p.Add("p_estimated_minutes", request.EstimatedMinutes);
        p.Add("p_user_id", userId);
        p.Add("p_delivery_id", dbType: DbType.String, direction: ParameterDirection.Output, size: 40);
        await db.ExecuteAsync("PKG_M9_CRM.CREATE_DELIVERY", p, commandType: CommandType.StoredProcedure);
        return p.Get<string>("p_delivery_id");
    }

    public Task AssignCourierAsync(
        string tenantId, string deliveryId, string? courierId, string userId, CancellationToken ct = default)
        => ExecAsync("PKG_M9_CRM.ASSIGN_COURIER", ct,
            ("p_tenant_id", tenantId), ("p_delivery_id", deliveryId),
            ("p_courier_id", courierId), ("p_user_id", userId));

    public Task SetCourierStatusAsync(string tenantId, string courierId, string status, CancellationToken ct = default)
        => ExecAsync("PKG_M9_CRM.SET_COURIER_STATUS", ct,
            ("p_tenant_id", tenantId), ("p_courier_id", courierId), ("p_status", status));

    public Task SetDeliveryStatusAsync(
        string tenantId, string deliveryId, string status, string userId, CancellationToken ct = default)
        => ExecAsync("PKG_M9_CRM.SET_DELIVERY_STATUS", ct,
            ("p_tenant_id", tenantId), ("p_delivery_id", deliveryId),
            ("p_status", status), ("p_user_id", userId));

    public Task RateDeliveryAsync(
        string tenantId, string deliveryId, int rating, string? feedback, CancellationToken ct = default)
        => ExecAsync("PKG_M9_CRM.RATE_DELIVERY", ct,
            ("p_tenant_id", tenantId), ("p_delivery_id", deliveryId),
            ("p_rating", rating), ("p_feedback", feedback));

    public Task UpdateCourierLocationAsync(
        string tenantId, string courierId, decimal latitude, decimal longitude, CancellationToken ct = default)
        => ExecAsync("PKG_M9_CRM.UPDATE_COURIER_LOCATION", ct,
            ("p_tenant_id", tenantId), ("p_courier_id", courierId),
            ("p_latitude", latitude), ("p_longitude", longitude));

    private async Task ExecAsync(string procName, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters();
        foreach (var (name, value) in args)
            p.Add(name, value);
        await db.ExecuteAsync(procName, p, commandType: CommandType.StoredProcedure);
    }
}
