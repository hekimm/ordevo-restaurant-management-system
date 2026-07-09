using Oracle.ManagedDataAccess.Client;
using Ordevo.BuildingBlocks.Results;
using Ordevo.Modules.M9Crm.Domain;

namespace Ordevo.Modules.M9Crm.Application;

public sealed class M9CrmService(IM9CrmRepository repo, IM9CrmProcedures procs)
{
    public async Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(
        string tenantId, string? search, int skip, int take, CancellationToken ct = default)
        => (await repo.SearchCustomersAsync(tenantId, search, Math.Max(0, skip), Math.Clamp(take, 1, 100), ct))
            .Select(ToDto).ToList();

    public async Task<Result<CustomerDetailDto>> GetCustomerAsync(string tenantId, string customerId, CancellationToken ct = default)
    {
        var customer = await repo.GetCustomerAsync(tenantId, customerId, ct);
        if (customer is null)
            return Error.NotFound("crm.customer.not_found", "Müşteri bulunamadı.");

        var addresses = await repo.ListCustomerAddressesAsync(tenantId, customerId, ct);
        var loyalty = await repo.ListLoyaltyAsync(tenantId, customerId, 30, ct);
        return new CustomerDetailDto(ToDto(customer), addresses.Select(ToDto).ToList(), loyalty.Select(ToDto).ToList());
    }

    public async Task<Result<CustomerDto>> GetCustomerByPhoneAsync(string tenantId, string phone, CancellationToken ct = default)
    {
        var customer = await repo.GetCustomerByPhoneAsync(tenantId, phone, ct);
        return customer is null ? Error.NotFound("crm.customer.not_found", "Müşteri bulunamadı.") : ToDto(customer);
    }

    public async Task<Result<CustomerDto>> CreateCustomerAsync(
        string tenantId, CreateCustomerRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            var id = await procs.CreateCustomerAsync(tenantId, request, userId, ct);
            return ToDto((await repo.GetCustomerAsync(tenantId, id, ct))!);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<CustomerDto>> UpdateCustomerAsync(
        string tenantId, string customerId, UpdateCustomerRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.UpdateCustomerAsync(tenantId, customerId, request, userId, ct);
            return ToDto((await repo.GetCustomerAsync(tenantId, customerId, ct))!);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> DeleteCustomerAsync(string tenantId, string customerId, CancellationToken ct = default)
    {
        var affected = await repo.DeleteCustomerAsync(tenantId, customerId, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("crm.customer.not_found", "Müşteri bulunamadı.");
    }

    public async Task<Result> BlockCustomerAsync(string tenantId, string customerId, string reason, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.BlockCustomerAsync(tenantId, customerId, reason, userId, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> UnblockCustomerAsync(string tenantId, string customerId, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.UnblockCustomerAsync(tenantId, customerId, userId, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<CustomerAddressDto>> AddCustomerAddressAsync(
        string tenantId, string customerId, CreateCustomerAddressRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            var id = await procs.AddCustomerAddressAsync(tenantId, customerId, request, userId, ct);
            var addresses = await repo.ListCustomerAddressesAsync(tenantId, customerId, ct);
            return ToDto(addresses.Single(a => a.Id == id));
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<string>> AddLoyaltyPointsAsync(string tenantId, LoyaltyPointsRequest request, string userId, CancellationToken ct = default)
    {
        try { return await procs.AddLoyaltyPointsAsync(tenantId, request, userId, ct); }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<string>> RedeemLoyaltyPointsAsync(string tenantId, RedeemLoyaltyRequest request, string userId, CancellationToken ct = default)
    {
        try { return await procs.RedeemLoyaltyPointsAsync(tenantId, request, userId, ct); }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<string>> AdjustLoyaltyPointsAsync(string tenantId, AdjustLoyaltyRequest request, string userId, CancellationToken ct = default)
    {
        try { return await procs.AdjustLoyaltyPointsAsync(tenantId, request, userId, ct); }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<CampaignDto>> ListCampaignsAsync(
        string tenantId, string? branchId, bool activeOnly, CancellationToken ct = default)
        => ListMapAsync(repo.ListCampaignsAsync(tenantId, branchId, activeOnly, ct), ToDto);

    public async Task<CampaignDto> CreateCampaignAsync(string tenantId, CreateCampaignRequest request, CancellationToken ct = default)
        => ToDto(await repo.InsertCampaignAsync(Guid.NewGuid().ToString(), tenantId, request, ct));

    public async Task<Result<decimal>> CalculateCampaignDiscountAsync(
        string tenantId, string orderId, string? customerId, string campaignCode, CancellationToken ct = default)
    {
        try { return await procs.CalculateCampaignDiscountAsync(tenantId, orderId, customerId, campaignCode, ct); }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<CampaignApplyResult>> ApplyCampaignAsync(
        string tenantId, ApplyCampaignRequest request, string userId, CancellationToken ct = default)
    {
        try { return await procs.ApplyCampaignAsync(tenantId, request, userId, ct); }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<ReservationDto>> ListReservationsAsync(
        string tenantId, string branchId, DateTime date, CancellationToken ct = default)
        => ListMapAsync(repo.ListReservationsAsync(tenantId, branchId, date.Date, ct), ToDto);

    public async Task<Result<ReservationDto>> CreateReservationAsync(
        string tenantId, string branchId, CreateReservationRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            var (id, _) = await procs.CreateReservationAsync(tenantId, branchId, request, userId, ct);
            return ToDto((await repo.GetReservationAsync(tenantId, id, ct))!);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<ReservationDto>> UpdateReservationAsync(
        string tenantId, string reservationId, CreateReservationRequest request, string userId, CancellationToken ct = default)
    {
        var reservation = await repo.UpdateReservationAsync(tenantId, reservationId, request, userId, ct);
        return reservation is null ? Error.NotFound("crm.reservation.not_found", "Rezervasyon bulunamadı.") : ToDto(reservation);
    }

    public async Task<Result> DeleteReservationAsync(string tenantId, string reservationId, CancellationToken ct = default)
    {
        var affected = await repo.DeleteReservationAsync(tenantId, reservationId, ct);
        return affected > 0 ? Result.Success() : Error.NotFound("crm.reservation.not_found", "Rezervasyon bulunamadı.");
    }

    public async Task<Result<ReservationDto>> SetReservationStatusAsync(
        string tenantId, string reservationId, SetReservationStatusRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.SetReservationStatusAsync(tenantId, reservationId, request.Status, request.Reason, userId, ct);
            return ToDto((await repo.GetReservationAsync(tenantId, reservationId, ct))!);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<DeliveryZoneDto>> ListDeliveryZonesAsync(
        string tenantId, string branchId, CancellationToken ct = default)
        => ListMapAsync(repo.ListDeliveryZonesAsync(tenantId, branchId, ct), ToDto);

    public async Task<DeliveryZoneDto> CreateDeliveryZoneAsync(
        string tenantId, string branchId, CreateDeliveryZoneRequest request, CancellationToken ct = default)
        => ToDto(await repo.InsertDeliveryZoneAsync(Guid.NewGuid().ToString(), tenantId, branchId, request, ct));

    public Task<IReadOnlyList<CourierDto>> ListCouriersAsync(
        string tenantId, string branchId, string? status, CancellationToken ct = default)
        => ListMapAsync(repo.ListCouriersAsync(tenantId, branchId, status, ct), ToDto);

    public async Task<CourierDto> CreateCourierAsync(
        string tenantId, string branchId, CreateCourierRequest request, CancellationToken ct = default)
        => ToDto(await repo.InsertCourierAsync(Guid.NewGuid().ToString(), tenantId, branchId, request, ct));

    public async Task<Result<CourierDto>> SetCourierStatusAsync(
        string tenantId, string courierId, SetCourierStatusRequest request, CancellationToken ct = default)
    {
        try
        {
            await procs.SetCourierStatusAsync(tenantId, courierId, request.Status, ct);
            var courier = await repo.GetCourierAsync(tenantId, courierId, ct);
            return courier is null ? Error.NotFound("crm.courier.not_found", "Kurye bulunamadı.") : ToDto(courier);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> UpdateCourierLocationAsync(
        string tenantId, string courierId, UpdateCourierLocationRequest request, CancellationToken ct = default)
    {
        try
        {
            await procs.UpdateCourierLocationAsync(tenantId, courierId, request.Latitude, request.Longitude, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public Task<IReadOnlyList<DeliveryDto>> ListActiveDeliveriesAsync(
        string tenantId, string branchId, CancellationToken ct = default)
        => ListMapAsync(repo.ListActiveDeliveriesAsync(tenantId, branchId, ct), ToDto);

    public async Task<Result<DeliveryDto>> GetDeliveryAsync(string tenantId, string deliveryId, CancellationToken ct = default)
    {
        var delivery = await repo.GetDeliveryAsync(tenantId, deliveryId, ct);
        return delivery is null ? Error.NotFound("crm.delivery.not_found", "Teslimat bulunamadı.") : ToDto(delivery);
    }

    public async Task<Result<DeliveryDto>> GetDeliveryByOrderAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        var delivery = await repo.GetDeliveryByOrderAsync(tenantId, orderId, ct);
        return delivery is null ? Error.NotFound("crm.delivery.not_found", "Sipariş için teslimat bulunamadı.") : ToDto(delivery);
    }

    public async Task<Result<DeliveryDto>> CreateDeliveryAsync(
        string tenantId, string branchId, CreateDeliveryRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            var id = await procs.CreateDeliveryAsync(tenantId, branchId, request, userId, ct);
            return ToDto((await repo.GetDeliveryAsync(tenantId, id, ct))!);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<DeliveryDto>> AssignCourierAsync(
        string tenantId, string deliveryId, AssignCourierRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.AssignCourierAsync(tenantId, deliveryId, request.CourierId, userId, ct);
            return ToDto((await repo.GetDeliveryAsync(tenantId, deliveryId, ct))!);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result<DeliveryDto>> SetDeliveryStatusAsync(
        string tenantId, string deliveryId, SetDeliveryStatusRequest request, string userId, CancellationToken ct = default)
    {
        try
        {
            await procs.SetDeliveryStatusAsync(tenantId, deliveryId, request.Status, userId, ct);
            return ToDto((await repo.GetDeliveryAsync(tenantId, deliveryId, ct))!);
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    public async Task<Result> RateDeliveryAsync(string tenantId, string deliveryId, RateDeliveryRequest request, CancellationToken ct = default)
    {
        try
        {
            await procs.RateDeliveryAsync(tenantId, deliveryId, request.Rating, request.Feedback, ct);
            return Result.Success();
        }
        catch (OracleException ex) when (TryMapOracle(ex, out var error)) { return error; }
    }

    private static async Task<IReadOnlyList<TOut>> ListMapAsync<TIn, TOut>(Task<IReadOnlyList<TIn>> source, Func<TIn, TOut> map)
        => (await source).Select(map).ToList();

    private static CustomerDto ToDto(Customer c) => new(
        c.Id, c.Phone, c.FullName, c.Email, c.Birthday, c.LoyaltyTier, c.LoyaltyPoints,
        c.TotalSpent, c.VisitCount, c.SmsConsent, c.EmailConsent, c.IsBlocked, c.BlockReason, c.CreatedAt);

    private static CustomerAddressDto ToDto(CustomerAddress a) => new(
        a.Id, a.CustomerId, a.Label, a.AddressLine1, a.AddressLine2, a.District, a.City,
        a.PostalCode, a.Latitude, a.Longitude, a.DeliveryNote, a.IsDefault);

    private static LoyaltyTransactionDto ToDto(LoyaltyTransaction t) => new(
        t.Id, t.CustomerId, t.TransactionType, t.Points, t.BalanceAfter, t.OrderId, t.Reason, t.CreatedAt);

    private static CampaignDto ToDto(Campaign c) => new(
        c.Id, c.BranchId, c.Code, c.Name, c.Description, c.DiscountType, c.DiscountValue,
        c.MaxDiscountAmount, c.MinOrderAmount, c.UsageLimitPerCustomer, c.TotalUsageLimit,
        c.UsageCount, c.StartsAt, c.EndsAt, c.IsActive, c.AutoApply, c.Priority);

    private static ReservationDto ToDto(Reservation r) => new(
        r.Id, r.ReservationNo, r.BranchId, r.CustomerId, r.CustomerName, r.CustomerPhone,
        r.ReservationDate, r.ReservationTime, r.GuestCount, r.TableId, r.Notes, r.Status,
        r.ConfirmedAt, r.SeatedAt, r.CancelledAt, r.CancelReason, r.CreatedAt);

    private static DeliveryZoneDto ToDto(DeliveryZone z) => new(
        z.Id, z.BranchId, z.Name, z.CenterLat, z.CenterLng, z.RadiusKm, z.DeliveryFee,
        z.MinOrderAmount, z.FreeDeliveryOver, z.EstimatedMinutes, z.IsActive);

    private static CourierDto ToDto(Courier c) => new(
        c.Id, c.BranchId, c.UserId, c.FullName, c.Phone, c.LicensePlate, c.VehicleType,
        c.Status, c.CurrentOrderId, c.LastLat, c.LastLng, c.LastLocationAt,
        c.TotalDeliveries, c.Rating, c.IsActive);

    private static DeliveryDto ToDto(Delivery d) => new(
        d.Id, d.BranchId, d.OrderId, d.CustomerId, d.CourierId, d.DeliveryZoneId,
        d.DeliveryAddress, d.DeliveryLat, d.DeliveryLng, d.DeliveryFee, d.EstimatedMinutes,
        d.Status, d.AssignedAt, d.PickedUpAt, d.DeliveredAt, d.Rating, d.Feedback, d.CreatedAt);

    private static bool TryMapOracle(OracleException ex, out Error error)
    {
        if (ex.Number is >= 20321 and <= 20360)
        {
            error = ex.Number switch
            {
                20321 => Error.Conflict("crm.customer.phone_exists", "Telefon numarası zaten kayıtlı."),
                20322 => Error.NotFound("crm.customer.not_found", "Müşteri bulunamadı."),
                20323 => Error.Validation("crm.customer.blocked", "Müşteri engellenmiş."),
                20324 => Error.Validation("crm.loyalty.invalid_points", "Puan miktarı geçersiz."),
                20325 => Error.Validation("crm.loyalty.insufficient", "Yetersiz puan bakiyesi."),
                20331 => Error.NotFound("crm.campaign.not_found", "Kampanya bulunamadı."),
                20332 => Error.Validation("crm.campaign.not_eligible", "Kampanya şartları sağlanmıyor."),
                20333 => Error.Conflict("crm.campaign.already_used", "Kampanya bu siparişte kullanılmış."),
                20341 => Error.NotFound("crm.reservation.not_found", "Rezervasyon bulunamadı."),
                20342 => Error.Validation("crm.reservation.invalid_status", "Rezervasyon durumu geçersiz."),
                20343 => Error.Conflict("crm.reservation.table_conflict", "Masa bu zaman için rezerve edilmiş."),
                20351 => Error.NotFound("crm.delivery.not_found", "Teslimat bulunamadı."),
                20352 => Error.Conflict("crm.delivery.already_exists", "Sipariş için teslimat zaten var."),
                20353 => Error.Validation("crm.delivery.no_courier", "Müsait kurye bulunamadı."),
                20354 => Error.Validation("crm.courier.unavailable", "Kurye müsait değil."),
                20355 => Error.Validation("crm.delivery.invalid_status", "Teslimat durumu geçersiz."),
                20356 => Error.Validation("crm.delivery.invalid_rating", "Puan 1-5 arasında olmalı."),
                _ => Error.Validation("crm.rule", CleanMessage(ex))
            };
            return true;
        }

        error = Error.Failure("crm.db", CleanMessage(ex));
        return false;
    }

    private static string CleanMessage(OracleException ex)
    {
        var first = ex.Message.Split('\n')[0];
        return first.Replace($"ORA-{ex.Number}:", "", StringComparison.OrdinalIgnoreCase).Trim();
    }
}
