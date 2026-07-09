using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordevo.BuildingBlocks.Http;
using Ordevo.BuildingBlocks.Multitenancy;
using Ordevo.Modules.Inventory.Application;

namespace Ordevo.Modules.Inventory.Api;

public static class InventoryEndpoints
{
    private const string Read = "inventory.read";
    private const string Manage = "inventory.manage";

    private static IResult NoBranch() => Results.Problem(title: "branch.required", detail: "İşlem için şube bağlamı gerekli.", statusCode: 400);

    public static void Map(IEndpointRouteBuilder root)
    {
        var g = root.MapGroup("/api/inventory").WithTags("Inventory");

        g.MapGet("/units", async (ITenantContext t, InventoryService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListUnitsAsync(t.RequireTenantId(), ct))).RequireAuthorization(Read);
        g.MapPost("/units", async (CreateUnitRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            Results.Ok(await svc.CreateUnitAsync(t.RequireTenantId(), r, ct)))
            .AddEndpointFilter<ValidationFilter<CreateUnitRequest>>().RequireAuthorization(Manage);
        g.MapPut("/units/{id}", async (string id, UpdateUnitRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.UpdateUnitAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpdateUnitRequest>>().RequireAuthorization(Manage);
        g.MapDelete("/units/{id}", async (string id, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.DeleteUnitAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapGet("/stock-items", async (ITenantContext t, InventoryService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.ListStockAsync(t.RequireTenantId(), t.BranchId, ct))).RequireAuthorization(Read);
        g.MapGet("/stock-items/{id}", async (string id, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.GetStockAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok)).RequireAuthorization(Read);
        g.MapPost("/stock-items", async (UpsertStockItemRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch() : Results.Ok(await svc.CreateStockAsync(t.RequireTenantId(), t.BranchId, r, ct)))
            .AddEndpointFilter<ValidationFilter<UpsertStockItemRequest>>().RequireAuthorization(Manage);
        g.MapPut("/stock-items/{id}", async (string id, UpsertStockItemRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.UpdateStockAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpsertStockItemRequest>>().RequireAuthorization(Manage);
        g.MapDelete("/stock-items/{id}", async (string id, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.DeleteStockAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);
        g.MapPost("/stock-items/{id}/adjust", async (string id, AdjustStockRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.AdjustAsync(t.RequireTenantId(), id, r, t.UserId!, ct)).Match(Results.Ok)).RequireAuthorization(Manage);
        g.MapGet("/stock-items/{id}/movements", async (string id, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListMovementsAsync(t.RequireTenantId(), id, ct))).RequireAuthorization(Read);

        g.MapGet("/menu-items/{menuItemId}/recipe", async (string menuItemId, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.GetRecipeAsync(t.RequireTenantId(), menuItemId, ct)).Match(Results.Ok)).RequireAuthorization(Read);
        g.MapPut("/menu-items/{menuItemId}/recipe", async (string menuItemId, SetRecipeRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            Results.Ok(await svc.SetRecipeAsync(t.RequireTenantId(), menuItemId, r, ct)))
            .AddEndpointFilter<ValidationFilter<SetRecipeRequest>>().RequireAuthorization(Manage);

        g.MapGet("/suppliers", async (ITenantContext t, InventoryService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListSuppliersAsync(t.RequireTenantId(), ct))).RequireAuthorization(Read);
        g.MapPost("/suppliers", async (CreateSupplierRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            Results.Ok(await svc.CreateSupplierAsync(t.RequireTenantId(), r, ct)))
            .AddEndpointFilter<ValidationFilter<CreateSupplierRequest>>().RequireAuthorization(Manage);
        g.MapPut("/suppliers/{id}", async (string id, UpdateSupplierRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.UpdateSupplierAsync(t.RequireTenantId(), id, r, ct)).Match(Results.Ok))
            .AddEndpointFilter<ValidationFilter<UpdateSupplierRequest>>().RequireAuthorization(Manage);
        g.MapDelete("/suppliers/{id}", async (string id, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.DeleteSupplierAsync(t.RequireTenantId(), id, ct)).Match(Results.NoContent))
            .RequireAuthorization(Manage);

        g.MapPost("/purchases", async (CreatePurchaseRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            t.BranchId is null ? NoBranch()
                : (await svc.CreatePurchaseAsync(t.RequireTenantId(), t.BranchId, t.UserId!, r, ct)).Match(p => Results.Created($"/api/inventory/purchases/{p.Id}", p)))
            .AddEndpointFilter<ValidationFilter<CreatePurchaseRequest>>().RequireAuthorization(Manage);
        g.MapGet("/purchases/{id}", async (string id, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.GetPurchaseAsync(t.RequireTenantId(), id, ct)).Match(Results.Ok)).RequireAuthorization(Read);
        g.MapPost("/purchases/{id}/receive", async (string id, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.ReceivePurchaseAsync(t.RequireTenantId(), id, t.UserId!, ct)).Match(Results.Ok)).RequireAuthorization(Manage);

        g.MapPost("/wastage", async (RecordWastageRequest r, ITenantContext t, InventoryService svc, CancellationToken ct) =>
            (await svc.RecordWastageAsync(t.RequireTenantId(), r, t.UserId!, ct)).Match(id => Results.Ok(new { wastageId = id })))
            .AddEndpointFilter<ValidationFilter<RecordWastageRequest>>().RequireAuthorization(Manage);
    }
}
