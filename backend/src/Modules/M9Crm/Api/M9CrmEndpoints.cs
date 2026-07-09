using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.M9Crm.Application;

namespace Ordevo.Modules.M9Crm.Api;

public static class M9CrmEndpoints
{
    private const string CustomerRead = "crm.customers.read";
    private const string CustomerManage = "crm.customers.manage";
    private const string LoyaltyManage = "crm.loyalty.manage";
    private const string CampaignRead = "crm.campaigns.read";
    private const string CampaignManage = "crm.campaigns.manage";
    private const string CampaignApply = "crm.campaigns.apply";
    private const string ReservationRead = "crm.reservations.read";
    private const string ReservationManage = "crm.reservations.manage";
    private const string DeliveryRead = "crm.delivery.read";
    private const string DeliveryManage = "crm.delivery.manage";

    public static void Map(IEndpointRouteBuilder root)
    {
        MapCustomers(root);
        MapLoyalty(root);
        MapCampaigns(root);
        MapReservations(root);
        MapDelivery(root);
    }

    private static IResult NoBranch() =>
        Results.Problem(title: "branch.required", detail: "İşlem için şube bağlamı gerekli.", statusCode: 400);

    private static DateTime ParseDate(string? value, DateTime fallback) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : fallback.Date;

    private static void MapCustomers(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/m9-crm/customers").WithTags("M9 CRM.Customers");

        g.MapGet("/", async (string? search, int? skip, int? take, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            Results.Ok(await svc.SearchCustomersAsync(t.RequireTenantId(), search, skip ?? 0, take ?? 30, ct)))
            .RequireAuthorization(CustomerRead);

        g.MapGet("/by-phone", async (string phone, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.GetCustomerByPhoneAsync(t.RequireTenantId(), phone, ct)).Match(Results.Ok))
            .RequireAuthorization(CustomerRead);

        g.MapGet("/{id}", async (string id, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.GetCustomerAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok))
            .RequireAuthorization(CustomerRead);

        g.MapPost("/", async (CreateCustomerRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.CreateCustomerAsync(t.RequireTenantId(), r, t.UserId!, ct))
                .Match(c => Results.Created($"/api/m9-crm/customers/{c.Id}", c)))
            .AddEndpointFilter<ValidationFilter<CreateCustomerRequest>>()
            .RequireAuthorization(CustomerManage);

        g.MapPut("/{id}", async (string id, UpdateCustomerRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.UpdateCustomerAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpdateCustomerRequest>>()
            .RequireAuthorization(CustomerManage);

        g.MapPost("/{id}/block", async (string id, BlockCustomerRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.BlockCustomerAsync(t.RequireTenantId(), id, r.Reason, t.UserId!, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<BlockCustomerRequest>>()
            .RequireAuthorization(CustomerManage);

        g.MapPost("/{id}/unblock", async (string id, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.UnblockCustomerAsync(t.RequireTenantId(), id, t.UserId!, ct)).Match(Results.NoContent))
            .RequireAuthorization(CustomerManage);

        g.MapDelete("/{id}", async (string id, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.DeleteCustomerAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(CustomerManage);

        g.MapPost("/{id}/addresses", async (string id, CreateCustomerAddressRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.AddCustomerAddressAsync(t.RequireTenantId(), id, r, t.UserId!, ct))
                .Match(a => Results.Created($"/api/m9-crm/customers/{id}/addresses/{a.Id}", a)))
            .AddEndpointFilter<ValidationFilter<CreateCustomerAddressRequest>>()
            .RequireAuthorization(CustomerManage);
    }

    private static void MapLoyalty(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/m9-crm/loyalty").WithTags("M9 CRM.Loyalty")
            .RequireAuthorization(LoyaltyManage);

        g.MapPost("/earn", async (LoyaltyPointsRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.AddLoyaltyPointsAsync(t.RequireTenantId(), r, t.UserId!, ct))
                .Match(id => Results.Ok(new { transactionId = id })))
            .AddEndpointFilter<ValidationFilter<LoyaltyPointsRequest>>();

        g.MapPost("/redeem", async (RedeemLoyaltyRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.RedeemLoyaltyPointsAsync(t.RequireTenantId(), r, t.UserId!, ct))
                .Match(id => Results.Ok(new { transactionId = id })))
            .AddEndpointFilter<ValidationFilter<RedeemLoyaltyRequest>>();

        g.MapPost("/adjust", async (AdjustLoyaltyRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.AdjustLoyaltyPointsAsync(t.RequireTenantId(), r, t.UserId!, ct))
                .Match(id => Results.Ok(new { transactionId = id })))
            .AddEndpointFilter<ValidationFilter<AdjustLoyaltyRequest>>();
    }

    private static void MapCampaigns(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/m9-crm/campaigns").WithTags("M9 CRM.Campaigns");

        g.MapGet("/", async (bool? activeOnly, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListCampaignsAsync(t.RequireTenantId(), t.BranchId, activeOnly ?? false, ct)))
            .RequireAuthorization(CampaignRead);

        g.MapPost("/", async (CreateCampaignRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            Results.Created("/api/m9-crm/campaigns", await svc.CreateCampaignAsync(t.RequireTenantId(), r, ct)))
            .AddEndpointFilter<ValidationFilter<CreateCampaignRequest>>()
            .RequireAuthorization(CampaignManage);

        g.MapGet("/discount", async (string orderId, string campaignCode, string? customerId, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.CalculateCampaignDiscountAsync(t.RequireTenantId(), orderId, customerId, campaignCode, ct))
                .Match(discount => Results.Ok(new { discount })))
            .RequireAuthorization(CampaignApply);

        g.MapPost("/apply", async (ApplyCampaignRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.ApplyCampaignAsync(t.RequireTenantId(), r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<ApplyCampaignRequest>>()
            .RequireAuthorization(CampaignApply);
    }

    private static void MapReservations(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/m9-crm/reservations").WithTags("M9 CRM.Reservations");

        g.MapGet("/", async (string? date, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : Results.Ok(await svc.ListReservationsAsync(t.RequireTenantId(), t.BranchId, ParseDate(date, DateTime.UtcNow), ct)))
            .RequireAuthorization(ReservationRead);

        g.MapPost("/", async (CreateReservationRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.CreateReservationAsync(t.RequireTenantId(), t.BranchId, r, t.UserId!, ct))
                    .Match(x => Results.Created($"/api/m9-crm/reservations/{x.Id}", x)))
            .AddEndpointFilter<ValidationFilter<CreateReservationRequest>>()
            .RequireAuthorization(ReservationManage);

        g.MapPut("/{id}", async (string id, CreateReservationRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.UpdateReservationAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<CreateReservationRequest>>()
            .RequireAuthorization(ReservationManage);

        g.MapPut("/{id}/status", async (string id, SetReservationStatusRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.SetReservationStatusAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<SetReservationStatusRequest>>()
            .RequireAuthorization(ReservationManage);

        g.MapDelete("/{id}", async (string id, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.DeleteReservationAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(ReservationManage);
    }

    private static void MapDelivery(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/m9-crm/delivery").WithTags("M9 CRM.Delivery");

        g.MapGet("/zones", async (ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : Results.Ok(await svc.ListDeliveryZonesAsync(t.RequireTenantId(), t.BranchId, ct)))
            .RequireAuthorization(DeliveryRead);

        g.MapPost("/zones", async (CreateDeliveryZoneRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : Results.Created("/api/m9-crm/delivery/zones", await svc.CreateDeliveryZoneAsync(t.RequireTenantId(), t.BranchId, r, ct)))
            .AddEndpointFilter<ValidationFilter<CreateDeliveryZoneRequest>>()
            .RequireAuthorization(DeliveryManage);

        g.MapGet("/couriers", async (string? status, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : Results.Ok(await svc.ListCouriersAsync(t.RequireTenantId(), t.BranchId, status, ct)))
            .RequireAuthorization(DeliveryRead);

        g.MapPost("/couriers", async (CreateCourierRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : Results.Created("/api/m9-crm/delivery/couriers", await svc.CreateCourierAsync(t.RequireTenantId(), t.BranchId, r, ct)))
            .AddEndpointFilter<ValidationFilter<CreateCourierRequest>>()
            .RequireAuthorization(DeliveryManage);

        g.MapPost("/couriers/{id}/location", async (string id, UpdateCourierLocationRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.UpdateCourierLocationAsync(t.RequireTenantId(), id, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<UpdateCourierLocationRequest>>()
            .RequireAuthorization(DeliveryManage);

        g.MapPut("/couriers/{id}/status", async (string id, SetCourierStatusRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.SetCourierStatusAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<SetCourierStatusRequest>>()
            .RequireAuthorization(DeliveryManage);

        g.MapGet("/active", async (ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : Results.Ok(await svc.ListActiveDeliveriesAsync(t.RequireTenantId(), t.BranchId, ct)))
            .RequireAuthorization(DeliveryRead);

        g.MapGet("/by-order/{orderId}", async (string orderId, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.GetDeliveryByOrderAsync(t.RequireTenantId(), orderId, ct)).Match(Results.Ok))
            .RequireAuthorization(DeliveryRead);

        g.MapGet("/{id}", async (string id, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.GetDeliveryAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok))
            .RequireAuthorization(DeliveryRead);

        g.MapPost("/", async (CreateDeliveryRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.CreateDeliveryAsync(t.RequireTenantId(), t.BranchId, r, t.UserId!, ct))
                    .Match(x => Results.Created($"/api/m9-crm/delivery/{x.Id}", x)))
            .AddEndpointFilter<ValidationFilter<CreateDeliveryRequest>>()
            .RequireAuthorization(DeliveryManage);

        g.MapPost("/{id}/assign", async (string id, AssignCourierRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.AssignCourierAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(DeliveryManage);

        g.MapPut("/{id}/status", async (string id, SetDeliveryStatusRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.SetDeliveryStatusAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<SetDeliveryStatusRequest>>()
            .RequireAuthorization(DeliveryManage);

        g.MapPost("/{id}/rate", async (string id, RateDeliveryRequest r, ITenantContext t, M9CrmService svc, CancellationToken ct) =>
            (await svc.RateDeliveryAsync(t.RequireTenantId(), id, r, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<RateDeliveryRequest>>()
            .RequireAuthorization(DeliveryManage);
    }
}
