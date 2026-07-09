using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Ordering.Application;
using Ordevo.Modules.Ordering.Realtime;

namespace Ordevo.Modules.Ordering.Api;

public static class OrderingEndpoints
{
    private const string Read = "order.read";
    private const string Create = "order.create";
    private const string Manage = "order.manage";

    public static void Map(IEndpointRouteBuilder root)
    {
        MapTables(root);
        MapOrders(root);

        root.MapHub<OrdersHub>("/hubs/orders");
        root.MapHub<TablesHub>("/hubs/tables");
    }

    private static IResult NoBranch() => Results.Problem(title: "branch.required", detail: "İşlem için şube bağlamı gerekli.", statusCode: 400);

    private static void MapTables(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/ordering").WithTags("Ordering.Tables");

        g.MapGet("/sections", async (ITenantContext t, TableService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ListSectionsAsync(t.RequireTenantId(), t.BranchId, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/sections", async (UpsertSectionRequest r, ITenantContext t, TableService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.CreateSectionAsync(t.RequireTenantId(), t.BranchId, r, ct)))
            .AddEndpointFilter<ValidationFilter<UpsertSectionRequest>>().RequireAuthorization(Manage);

        g.MapPut("/sections/{id}", async (string id, UpsertSectionRequest r, ITenantContext t, TableService svc, CancellationToken ct) =>
            (await svc.UpdateSectionAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpsertSectionRequest>>().RequireAuthorization(Manage);

        g.MapDelete("/sections/{id}", async (string id, ITenantContext t, TableService svc, CancellationToken ct) =>
            (await svc.DeleteSectionAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapGet("/tables", async (ITenantContext t, TableService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ListTablesAsync(t.RequireTenantId(), t.BranchId, ct)))
            .RequireAuthorization(Read);

        g.MapPost("/tables", async (UpsertTableRequest r, ITenantContext t, TableService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.CreateTableAsync(t.RequireTenantId(), t.BranchId, r, ct)))
            .AddEndpointFilter<ValidationFilter<UpsertTableRequest>>().RequireAuthorization(Manage);

        g.MapPut("/tables/{id}", async (string id, UpsertTableRequest r, ITenantContext t, TableService svc, CancellationToken ct) =>
            (await svc.UpdateTableAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpsertTableRequest>>().RequireAuthorization(Manage);

        g.MapDelete("/tables/{id}", async (string id, ITenantContext t, TableService svc, CancellationToken ct) =>
            (await svc.DeleteTableAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);
    }

    private static void MapOrders(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/ordering/orders").WithTags("Ordering.Orders");

        g.MapGet("/", async (string? status, ITenantContext t, OrderService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ListAsync(t.RequireTenantId(), t.BranchId, status, ct)))
            .RequireAuthorization(Read);

        g.MapGet("/{id}", async (string id, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.GetAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok))
            .RequireAuthorization(Read);

        g.MapPost("/", async (OpenOrderRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.OpenAsync(t.RequireTenantId(), t.BranchId, t.UserId!, r, ct)).Match(o => Results.Created($"/api/ordering/orders/{o.Id}", o)))
            .AddEndpointFilter<ValidationFilter<OpenOrderRequest>>().RequireAuthorization(Create);

        g.MapPost("/{id}/items", async (string id, AddItemRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.AddItemAsync(t.RequireTenantId(), id, t.UserId!, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<AddItemRequest>>().RequireAuthorization(Create);

        g.MapPut("/items/{itemId}/quantity", async (string itemId, SetQuantityRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.SetQuantityAsync(t.RequireTenantId(), itemId, r.Quantity, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Create);

        g.MapPost("/items/{itemId}/void", async (string itemId, VoidItemRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.VoidItemAsync(t.RequireTenantId(), itemId, r.Reason, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);

        g.MapPost("/items/{itemId}/comp", async (string itemId, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.CompItemAsync(t.RequireTenantId(), itemId, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);

        g.MapPut("/items/{itemId}/status", async (string itemId, ItemStatusRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.UpdateItemStatusAsync(t.RequireTenantId(), itemId, r.Status, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Create);

        g.MapPost("/{id}/discounts", async (string id, ApplyDiscountRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.ApplyDiscountAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<ApplyDiscountRequest>>().RequireAuthorization(Manage);

        g.MapPost("/{id}/transfer", async (string id, TransferRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.TransferAsync(t.RequireTenantId(), id, r.ToTableId, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);

        g.MapPost("/{id}/merge", async (string id, MergeRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.MergeAsync(t.RequireTenantId(), id, r.SourceOrderId, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);

        g.MapPost("/{id}/split", async (string id, SplitRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.SplitAsync(t.RequireTenantId(), t.BranchId, id, r.ItemIds, r.ToTableId, t.UserId!, ct)).Match(o => Results.Created($"/api/ordering/orders/{o.Id}", o)))
            .RequireAuthorization(Manage);

        g.MapPost("/{id}/close", async (string id, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.CloseAsync(t.RequireTenantId(), id, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);

        g.MapPost("/{id}/cancel", async (string id, CancelOrderRequest r, ITenantContext t, OrderService svc, CancellationToken ct) =>
            (await svc.CancelAsync(t.RequireTenantId(), id, r.Reason, t.UserId!, ct)).Match(Results.Ok))
            .RequireAuthorization(Manage);
    }
}
