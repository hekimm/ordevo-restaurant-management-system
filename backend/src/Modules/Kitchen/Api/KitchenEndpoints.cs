using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Kitchen.Application;
using Ordevo.Modules.Kitchen.Realtime;

namespace Ordevo.Modules.Kitchen.Api;

public static class KitchenEndpoints
{
    private const string View = "kitchen.view";
    private const string Manage = "kitchen.manage";

    private static IResult NoBranch() => Results.Problem(title: "branch.required", detail: "İşlem için şube bağlamı gerekli.", statusCode: 400);

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/kitchen").WithTags("Kitchen");

        g.MapGet("/stations", async (ITenantContext t, KitchenService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ListAsync(t.RequireTenantId(), t.BranchId, ct)))
            .RequireAuthorization(View);

        g.MapPost("/stations", async (UpsertStationRequest r, ITenantContext t, KitchenService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.CreateAsync(t.RequireTenantId(), t.BranchId, r, ct)))
            .AddEndpointFilter<ValidationFilter<UpsertStationRequest>>().RequireAuthorization(Manage);

        g.MapPut("/stations/{id}", async (string id, UpsertStationRequest r, ITenantContext t, KitchenService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpsertStationRequest>>().RequireAuthorization(Manage);

        g.MapDelete("/stations/{id}", async (string id, ITenantContext t, KitchenService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapGet("/board", async (string? station, ITenantContext t, KdsService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.GetBoardAsync(t.RequireTenantId(), t.BranchId, station, ct)))
            .RequireAuthorization(View);

        g.MapPost("/items/{itemId}/status", async (string itemId, SetItemStatusRequest r, ITenantContext t, KdsService svc, CancellationToken ct) =>
            (await svc.SetStatusAsync(t.RequireTenantId(), itemId, r.Status, t.UserId!, ct)).Match(Results.NoContent))
            .AddEndpointFilter<ValidationFilter<SetItemStatusRequest>>().RequireAuthorization(Manage);

        g.MapPost("/orders/{orderId}/bump", async (string orderId, ITenantContext t, KdsService svc, CancellationToken ct) =>
            (await svc.BumpOrderAsync(t.RequireTenantId(), orderId, t.UserId!, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        root.MapHub<KdsHub>("/hubs/kds");
    }
}
