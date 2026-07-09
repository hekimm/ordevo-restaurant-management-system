using Dapper;
using Ordevo.BuildingBlocks.Data;
using Ordevo.Modules.M9Crm.Application;
using Ordevo.Modules.M9Crm.Domain;

namespace Ordevo.Modules.M9Crm.Infrastructure;

public sealed class M9CrmRepository(IDbConnectionFactory factory) : IM9CrmRepository
{
    public async Task<Customer?> GetCustomerAsync(string tenantId, string customerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Customer>(
            "SELECT * FROM CRM_CUSTOMERS WHERE TENANT_ID = :tenantId AND ID = :customerId",
            new OracleParams(new { tenantId, customerId }));
    }

    public async Task<Customer?> GetCustomerByPhoneAsync(string tenantId, string phone, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Customer>(
            "SELECT * FROM CRM_CUSTOMERS WHERE TENANT_ID = :tenantId AND PHONE = :phone",
            new OracleParams(new { tenantId, phone }));
    }

    public async Task<IReadOnlyList<Customer>> SearchCustomersAsync(
        string tenantId, string? search, int skip, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Customer>(
            """
            SELECT * FROM CRM_CUSTOMERS
            WHERE TENANT_ID = :tenantId
              AND (:search IS NULL
                   OR UPPER(FULL_NAME) LIKE '%' || UPPER(:search) || '%'
                   OR PHONE LIKE '%' || :search || '%'
                   OR UPPER(EMAIL) LIKE '%' || UPPER(:search) || '%')
            ORDER BY UPDATED_AT DESC
            OFFSET :skip ROWS FETCH NEXT :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, search, skip, take }));
        return rows.AsList();
    }

    public async Task<int> DeleteCustomerAsync(string tenantId, string customerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM CRM_CUSTOMERS WHERE TENANT_ID = :tenantId AND ID = :customerId",
            new OracleParams(new { tenantId, customerId }));
    }

    public async Task<IReadOnlyList<CustomerAddress>> ListCustomerAddressesAsync(
        string tenantId, string customerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<CustomerAddress>(
            """
            SELECT * FROM CRM_CUSTOMER_ADDRESSES
            WHERE TENANT_ID = :tenantId AND CUSTOMER_ID = :customerId
            ORDER BY IS_DEFAULT DESC, CREATED_AT DESC
            """,
            new OracleParams(new { tenantId, customerId }));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<LoyaltyTransaction>> ListLoyaltyAsync(
        string tenantId, string customerId, int take, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<LoyaltyTransaction>(
            """
            SELECT * FROM CRM_LOYALTY_TRANSACTIONS
            WHERE TENANT_ID = :tenantId AND CUSTOMER_ID = :customerId
            ORDER BY CREATED_AT DESC FETCH FIRST :take ROWS ONLY
            """,
            new OracleParams(new { tenantId, customerId, take }));
        return rows.AsList();
    }

    public async Task<Campaign?> GetCampaignAsync(string tenantId, string campaignId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Campaign>(
            "SELECT * FROM CRM_CAMPAIGNS WHERE TENANT_ID = :tenantId AND ID = :campaignId",
            new OracleParams(new { tenantId, campaignId }));
    }

    public async Task<Campaign?> GetCampaignByCodeAsync(string tenantId, string code, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Campaign>(
            "SELECT * FROM CRM_CAMPAIGNS WHERE TENANT_ID = :tenantId AND UPPER(CODE) = UPPER(:code)",
            new OracleParams(new { tenantId, code }));
    }

    public async Task<IReadOnlyList<Campaign>> ListCampaignsAsync(
        string tenantId, string? branchId, bool activeOnly, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Campaign>(
            """
            SELECT * FROM CRM_CAMPAIGNS
            WHERE TENANT_ID = :tenantId
              AND (:branchId IS NULL OR BRANCH_ID IS NULL OR BRANCH_ID = :branchId)
              AND (:activeOnly = 0 OR IS_ACTIVE = 1)
            ORDER BY PRIORITY DESC, STARTS_AT DESC
            """,
            new OracleParams(new { tenantId, branchId, activeOnly = activeOnly ? 1 : 0 }));
        return rows.AsList();
    }

    public async Task<Campaign> InsertCampaignAsync(
        string id, string tenantId, CreateCampaignRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO CRM_CAMPAIGNS (
                ID, TENANT_ID, BRANCH_ID, CODE, NAME, DESCRIPTION, DISCOUNT_TYPE, DISCOUNT_VALUE,
                MAX_DISCOUNT_AMOUNT, MIN_ORDER_AMOUNT, USAGE_LIMIT_PER_CUSTOMER, TOTAL_USAGE_LIMIT,
                STARTS_AT, ENDS_AT, IS_ACTIVE, AUTO_APPLY, PRIORITY)
            VALUES (
                :id, :tenantId, :branchId, :code, :name, :description, :discountType, :discountValue,
                :maxDiscountAmount, :minOrderAmount, :usageLimitPerCustomer, :totalUsageLimit,
                :startsAt, :endsAt, :isActive, :autoApply, :priority)
            """,
            new OracleParams(new
            {
                id,
                tenantId,
                branchId = request.BranchId,
                code = request.Code.Trim().ToUpperInvariant(),
                name = request.Name.Trim(),
                description = request.Description,
                discountType = request.DiscountType,
                discountValue = request.DiscountValue,
                maxDiscountAmount = request.MaxDiscountAmount,
                minOrderAmount = request.MinOrderAmount,
                usageLimitPerCustomer = request.UsageLimitPerCustomer,
                totalUsageLimit = request.TotalUsageLimit,
                startsAt = request.StartsAt,
                endsAt = request.EndsAt,
                isActive = request.IsActive ? 1 : 0,
                autoApply = request.AutoApply ? 1 : 0,
                priority = request.Priority
            }));

        return (await GetCampaignAsync(tenantId, id, ct))!;
    }

    public async Task<Reservation?> GetReservationAsync(string tenantId, string reservationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Reservation>(
            "SELECT * FROM CRM_RESERVATIONS WHERE TENANT_ID = :tenantId AND ID = :reservationId",
            new OracleParams(new { tenantId, reservationId }));
    }

    public async Task<IReadOnlyList<Reservation>> ListReservationsAsync(
        string tenantId, string branchId, DateTime date, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Reservation>(
            """
            SELECT * FROM CRM_RESERVATIONS
            WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId AND RESERVATION_DATE = :dateValue
            ORDER BY RESERVATION_TIME, CREATED_AT
            """,
            new OracleParams(new { tenantId, branchId, dateValue = date.Date }));
        return rows.AsList();
    }

    public async Task<Reservation?> UpdateReservationAsync(string tenantId, string reservationId, CreateReservationRequest request, string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.ExecuteAsync(
            """
            UPDATE CRM_RESERVATIONS
               SET CUSTOMER_ID = :customerId,
                   CUSTOMER_NAME = :customerName,
                   CUSTOMER_PHONE = :customerPhone,
                   RESERVATION_DATE = :reservationDate,
                   RESERVATION_TIME = :reservationTime,
                   GUEST_COUNT = :guestCount,
                   TABLE_ID = :tableId,
                   NOTES = :notes,
                   UPDATED_BY = :userId,
                   UPDATED_AT = SYSTIMESTAMP,
                   ROW_VERSION = ROW_VERSION + 1
             WHERE TENANT_ID = :tenantId AND ID = :reservationId
            """,
            new OracleParams(new
            {
                tenantId,
                reservationId,
                customerId = request.CustomerId,
                customerName = request.CustomerName,
                customerPhone = request.CustomerPhone,
                reservationDate = request.ReservationDate.Date,
                reservationTime = request.ReservationTime,
                guestCount = request.GuestCount,
                tableId = request.TableId,
                notes = request.Notes,
                userId
            }));

        return rows == 0 ? null : await GetReservationAsync(tenantId, reservationId, ct);
    }

    public async Task<int> DeleteReservationAsync(string tenantId, string reservationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.ExecuteAsync(
            "DELETE FROM CRM_RESERVATIONS WHERE TENANT_ID = :tenantId AND ID = :reservationId",
            new OracleParams(new { tenantId, reservationId }));
    }

    public async Task<IReadOnlyList<DeliveryZone>> ListDeliveryZonesAsync(
        string tenantId, string branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<DeliveryZone>(
            """
            SELECT * FROM CRM_DELIVERY_ZONES
            WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId
            ORDER BY IS_ACTIVE DESC, NAME
            """,
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }

    public async Task<DeliveryZone> InsertDeliveryZoneAsync(
        string id, string tenantId, string branchId, CreateDeliveryZoneRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO CRM_DELIVERY_ZONES (
                ID, TENANT_ID, BRANCH_ID, NAME, CENTER_LAT, CENTER_LNG, RADIUS_KM,
                DELIVERY_FEE, MIN_ORDER_AMOUNT, FREE_DELIVERY_OVER, ESTIMATED_MINUTES, IS_ACTIVE)
            VALUES (
                :id, :tenantId, :branchId, :name, :centerLat, :centerLng, :radiusKm,
                :deliveryFee, :minOrderAmount, :freeDeliveryOver, :estimatedMinutes, :isActive)
            """,
            new OracleParams(new
            {
                id,
                tenantId,
                branchId,
                name = request.Name.Trim(),
                request.CenterLat,
                request.CenterLng,
                request.RadiusKm,
                request.DeliveryFee,
                request.MinOrderAmount,
                request.FreeDeliveryOver,
                request.EstimatedMinutes,
                isActive = request.IsActive ? 1 : 0
            }));

        var zones = await ListDeliveryZonesAsync(tenantId, branchId, ct);
        return zones.Single(z => z.Id == id);
    }

    public async Task<IReadOnlyList<Courier>> ListCouriersAsync(
        string tenantId, string branchId, string? status, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Courier>(
            """
            SELECT * FROM CRM_COURIERS
            WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId
              AND (:status IS NULL OR STATUS = :status)
            ORDER BY IS_ACTIVE DESC, STATUS, FULL_NAME
            """,
            new OracleParams(new { tenantId, branchId, status }));
        return rows.AsList();
    }

    public async Task<Courier?> GetCourierAsync(string tenantId, string courierId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Courier>(
            "SELECT * FROM CRM_COURIERS WHERE TENANT_ID = :tenantId AND ID = :courierId",
            new OracleParams(new { tenantId, courierId }));
    }

    public async Task<Courier> InsertCourierAsync(
        string id, string tenantId, string branchId, CreateCourierRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        await db.ExecuteAsync(
            """
            INSERT INTO CRM_COURIERS (
                ID, TENANT_ID, BRANCH_ID, USER_ID, FULL_NAME, PHONE, LICENSE_PLATE,
                VEHICLE_TYPE, STATUS, IS_ACTIVE)
            VALUES (
                :id, :tenantId, :branchId, :userId, :fullName, :phone, :licensePlate,
                :vehicleType, 'off_duty', :isActive)
            """,
            new OracleParams(new
            {
                id,
                tenantId,
                branchId,
                userId = request.UserId,
                fullName = request.FullName.Trim(),
                phone = request.Phone.Trim(),
                licensePlate = request.LicensePlate,
                request.VehicleType,
                isActive = request.IsActive ? 1 : 0
            }));

        return (await GetCourierAsync(tenantId, id, ct))!;
    }

    public async Task<Delivery?> GetDeliveryAsync(string tenantId, string deliveryId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Delivery>(
            "SELECT * FROM CRM_DELIVERIES WHERE TENANT_ID = :tenantId AND ID = :deliveryId",
            new OracleParams(new { tenantId, deliveryId }));
    }

    public async Task<Delivery?> GetDeliveryByOrderAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Delivery>(
            "SELECT * FROM CRM_DELIVERIES WHERE TENANT_ID = :tenantId AND ORDER_ID = :orderId",
            new OracleParams(new { tenantId, orderId }));
    }

    public async Task<IReadOnlyList<Delivery>> ListActiveDeliveriesAsync(
        string tenantId, string branchId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateOpenConnectionAsync(ct);
        var rows = await db.QueryAsync<Delivery>(
            """
            SELECT * FROM CRM_DELIVERIES
            WHERE TENANT_ID = :tenantId AND BRANCH_ID = :branchId
              AND STATUS IN ('pending','assigned','picked_up','on_way')
            ORDER BY CREATED_AT
            """,
            new OracleParams(new { tenantId, branchId }));
        return rows.AsList();
    }
}
